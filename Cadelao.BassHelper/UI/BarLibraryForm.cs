using Cadelao.BassHelper.Library;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Cadelao.BassHelper.UI;

/// <summary>
/// Independent, strictly read-only WinForms catalog browser. The filtered
/// 4/5-star list is in memory; album pages are the only on-demand DB reads.
/// No SQLite operation is performed from a UI event, CellValueNeeded or paint.
/// </summary>
internal partial class BarLibraryForm : Form
{
    private const int PageSize = 128;
    private const int MaxCachedPages = 6;
    private readonly BarLibraryCatalog _catalog = new();
    private readonly System.Windows.Forms.Timer _filterDebounce = new() { Interval = 250 };
    private readonly SemaphoreSlim _pageGate = new(1, 1);
    private BarCatalogSnapshot? _snapshot;
    private IReadOnlyList<BarAlbum> _visibleAlbums = Array.Empty<BarAlbum>();
    private BarAlbum? _activeAlbum;
    private readonly Dictionary<int, List<BarSong>> _cachedPages = new();
    private readonly Queue<int> _cacheOrder = new();
    private readonly HashSet<int> _pendingPages = new();
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _pageCts;
    private int _pageGeneration;
    private int _filterGeneration;
    private bool _isClosing;
    private bool _refreshing;
    private bool _suppressSelection;
    private bool _firstShown;
    private bool _selectionQueued;

    public event EventHandler<string>? SongChosen;

    public BarLibraryForm()
    {
        InitializeComponent();
        AttachHandlers();
        lblStatus.Text = "Base somente leitura: " + _catalog.CatalogPath;
    }

    private void AttachHandlers()
    {
        btnRefresh.Click += async (_, _) => await RefreshCatalogAsync();
        listAlbums.RetrieveVirtualItem += (_, e) =>
        {
            if (e.ItemIndex == 0)
                e.Item = new ListViewItem($"Favoritas 4-5 estrelas ({_snapshot?.Count ?? 0:N0})");
            else if (e.ItemIndex > 0 && e.ItemIndex - 1 < _visibleAlbums.Count)
                e.Item = new ListViewItem(_visibleAlbums[e.ItemIndex - 1].Label);
            else e.Item = new ListViewItem("");
        };
        // The native ListView emits a transient deselection and another
        // selection event on a single click. Coalesce both before resetting
        // the virtual grid and cancelling requests.
        listAlbums.SelectedIndexChanged += (_, _) => QueueAlbumSelection();
        txtFilter.TextChanged += (_, _) =>
        {
            _filterGeneration++;
            _filterDebounce.Stop();
            _filterDebounce.Start();
        };
        _filterDebounce.Tick += (_, _) =>
        {
            _filterDebounce.Stop();
            _ = ApplyAlbumFilterAsync();
        };
        gridSongs.CellValueNeeded += GridSongs_CellValueNeeded;
        gridSongs.CellDoubleClick += GridSongs_CellDoubleClick;
        gridSongs.DataError += (_, e) => e.ThrowException = false;
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_firstShown) return;
        _firstShown = true;
        await RefreshCatalogAsync();
    }

    private async Task RefreshCatalogAsync()
    {
        if (_isClosing || _refreshing) return;
        _refreshing = true;
        btnRefresh.Enabled = false;
        _refreshCts?.Cancel();
        var cts = new CancellationTokenSource();
        _refreshCts = cts;
        lblStatus.Text = "Consultando somente as musicas com 4 ou 5 estrelas...";
        try
        {
            var snapshot = await Task.Run(() => _catalog.ReadSnapshot(cts.Token), cts.Token);
            if (_isClosing || IsDisposed || cts.IsCancellationRequested) return;
            _snapshot = snapshot;
            _filterGeneration++;
            await ApplyAlbumFilterAsync();
            if (_isClosing || IsDisposed) return;
            lblStatus.Text = snapshot.Count == 0
                ? "Nenhuma faixa com 4 ou 5 estrelas encontrada no catalogo do Bar."
                : $"{snapshot.Count:N0} favoritas 4-5 estrelas · {snapshot.Albums.Count:N0} albuns · " +
                  "somente leitura · Atualizar para reler as avaliacoes.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                   InvalidDataException or SqliteException or JsonException)
        {
            if (_isClosing || IsDisposed) return;
            _snapshot = null;
            _visibleAlbums = Array.Empty<BarAlbum>();
            _suppressSelection = true;
            try { listAlbums.VirtualListSize = 0; }
            finally { _suppressSelection = false; }
            ResetPlaylist();
            lblStatus.Text = "Nao foi possivel consultar o catalogo: " + ex.Message;
            MessageBox.Show(this,
                "A biblioteca do CadelaoBar8800 nao pode ser consultada.\n\n" +
                ex.Message + "\n\nArquivo esperado:\n" + _catalog.CatalogPath +
                "\n\nO BREGA BASS nao criara nem alterara essa base.",
                "Biblioteca somente leitura", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            if (ReferenceEquals(_refreshCts, cts)) _refreshCts = null;
            cts.Dispose();
            _refreshing = false;
            if (!_isClosing && !IsDisposed) btnRefresh.Enabled = true;
        }
    }

    private async Task ApplyAlbumFilterAsync()
    {
        if (_isClosing || IsDisposed) return;
        int generation = _filterGeneration;
        string query = txtFilter.Text.Trim();
        var snapshot = _snapshot;
        IReadOnlyList<BarAlbum> filtered;
        if (snapshot is null)
            filtered = Array.Empty<BarAlbum>();
        else if (query.Length == 0)
            filtered = snapshot.Albums; // no large ToList() copy on the UI thread
        else
        {
            // Search can contain tens of thousands of albums. Filter off UI.
            filtered = await Task.Run<IReadOnlyList<BarAlbum>>(() =>
                snapshot.Albums.Where(a => a.Label.Contains(query,
                    StringComparison.OrdinalIgnoreCase)).ToList());
        }
        if (_isClosing || IsDisposed || generation != _filterGeneration ||
            !ReferenceEquals(snapshot, _snapshot)) return;

        _visibleAlbums = filtered;
        _suppressSelection = true;
        try
        {
            // VirtualListSize already schedules the needed repaint. Calling
            // Invalidate() here caused massive native ListView redraws.
            listAlbums.VirtualListSize = filtered.Count + (snapshot is null ? 0 : 1);
        }
        finally { _suppressSelection = false; }
        _activeAlbum = null;
        ResetPlaylist();
    }

    private void QueueAlbumSelection()
    {
        if (_isClosing || _suppressSelection || _snapshot is null || _selectionQueued)
            return;
        _selectionQueued = true;
        BeginInvoke((Action)(() =>
        {
            _selectionQueued = false;
            if (_isClosing || IsDisposed || _suppressSelection || _snapshot is null) return;
            if (listAlbums.SelectedIndices.Count == 0) return;
            int index = listAlbums.SelectedIndices[0];
            BarAlbum? selection = index > 0 && index - 1 < _visibleAlbums.Count
                ? _visibleAlbums[index - 1] : null;
            if (Equals(_activeAlbum, selection)) return;
            _activeAlbum = selection;
            ResetPlaylist();
        }));
    }

    private void ResetPlaylist()
    {
        _pageGeneration++;
        _pageCts?.Cancel();
        _pageCts = new CancellationTokenSource();
        _cachedPages.Clear();
        _cacheOrder.Clear();
        _pendingPages.Clear();

        if (_snapshot is null)
        {
            gridSongs.RowCount = 0;
            return;
        }
        if (_activeAlbum is null)
        {
            // Favourite records are in memory; switching back to All does
            // not issue a single SQL query or start a background page task.
            gridSongs.RowCount = _snapshot.Favourites.Count;
            lblStatus.Text = $"Favoritas 4-5 estrelas · {_snapshot.Count:N0} musicas · somente leitura";
        }
        else
        {
            // One tiny indexed COUNT query off-thread, only when clicked.
            gridSongs.RowCount = 0;
            lblStatus.Text = "Abrindo album em segundo plano: " + _activeAlbum.Label;
            int generation = _pageGeneration;
            _ = LoadAlbumCountAsync(_snapshot.Root, _activeAlbum, generation, _pageCts.Token);
        }
    }

    private async Task LoadAlbumCountAsync(string root, BarAlbum album, int generation,
        CancellationToken token)
    {
        try
        {
            long count = await Task.Run(() => _catalog.ReadAlbumCount(root, album, token), token);
            if (_isClosing || IsDisposed || token.IsCancellationRequested || generation != _pageGeneration)
                return;
            gridSongs.RowCount = (int)Math.Clamp(count, 0, int.MaxValue);
            lblStatus.Text = $"{album.Label} · {count:N0} faixas no album · somente leitura";
            if (count > 0) RequestPage(0);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            if (!_isClosing && !IsDisposed && generation == _pageGeneration)
                lblStatus.Text = "Nao foi possivel abrir album: " + ex.Message;
        }
    }

    private void GridSongs_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (_snapshot is null || e.RowIndex < 0 || e.RowIndex >= gridSongs.RowCount) return;
        if (_activeAlbum is null)
        {
            if (e.RowIndex < _snapshot.Favourites.Count)
                FillCell(_snapshot.Favourites[e.RowIndex], e);
            return;
        }

        int start = (e.RowIndex / PageSize) * PageSize;
        if (!_cachedPages.TryGetValue(start, out var tracks))
        {
            RequestPage(start);
            if (e.ColumnIndex == colTitle.Index) e.Value = "Carregando...";
            return;
        }
        int index = e.RowIndex - start;
        if (index >= 0 && index < tracks.Count) FillCell(tracks[index], e);
    }

    private void FillCell(BarSong song, DataGridViewCellValueEventArgs e)
    {
        if (e.ColumnIndex == colTrack.Index)
            e.Value = song.TrackNumber > 0 ? song.TrackNumber.ToString("00") : "?";
        else if (e.ColumnIndex == colTitle.Index) e.Value = song.Title;
        else if (e.ColumnIndex == colArtist.Index) e.Value = song.Artist;
        else if (e.ColumnIndex == colAlbum.Index) e.Value = song.Album;
        else if (e.ColumnIndex == colDuration.Index) e.Value = song.DisplayDuration;
        else if (e.ColumnIndex == colRating.Index) e.Value = song.DisplayStars;
    }

    private void RequestPage(int start)
    {
        if (_isClosing || _snapshot is null || _activeAlbum is null ||
            _cachedPages.ContainsKey(start) || !_pendingPages.Add(start)) return;
        var token = _pageCts?.Token ?? CancellationToken.None;
        _ = FetchPageAsync(start, _pageGeneration, _snapshot.Root, _activeAlbum, token);
    }

    private bool IsPageVisible(int start)
    {
        if (gridSongs.RowCount == 0) return false;
        int first = gridSongs.FirstDisplayedScrollingRowIndex;
        if (first < 0) return false;
        int visible = gridSongs.DisplayedRowCount(false);
        return visible > 0 && start < first + visible && start + PageSize > first;
    }

    private async Task FetchPageAsync(int start, int generation, string root,
        BarAlbum album, CancellationToken token)
    {
        bool gateHeld = false;
        try
        {
            // One SQLite read at a time, preventing many concurrent ORDER BY
            // calls while the user scrolls quickly. Old pages are discarded.
            await _pageGate.WaitAsync(token);
            gateHeld = true;
            if (_isClosing || IsDisposed || token.IsCancellationRequested ||
                generation != _pageGeneration || !IsPageVisible(start)) return;
            var tracks = await Task.Run(
                () => _catalog.ReadPage(root, album, start, PageSize, token), token);
            if (_isClosing || IsDisposed || token.IsCancellationRequested ||
                generation != _pageGeneration) return;
            _cachedPages[start] = tracks;
            _cacheOrder.Enqueue(start);
            while (_cacheOrder.Count > MaxCachedPages)
                _cachedPages.Remove(_cacheOrder.Dequeue());
            if (IsPageVisible(start))
            {
                // Only repaint rows belonging to the visible portion.
                int first = gridSongs.FirstDisplayedScrollingRowIndex;
                int last = Math.Min(gridSongs.RowCount,
                    first + gridSongs.DisplayedRowCount(false));
                for (int i = Math.Max(first, start); i < Math.Min(last, start + PageSize); i++)
                    gridSongs.InvalidateRow(i);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                   SqliteException or InvalidDataException)
        {
            if (!_isClosing && !IsDisposed && generation == _pageGeneration)
                lblStatus.Text = "Falha temporaria na leitura do album: " + ex.Message;
        }
        finally
        {
            if (gateHeld) _pageGate.Release();
            if (!_isClosing && !IsDisposed && generation == _pageGeneration)
                _pendingPages.Remove(start);
        }
    }

    private async void GridSongs_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_isClosing || _snapshot is null || e.RowIndex < 0 ||
            e.RowIndex >= gridSongs.RowCount) return;
        int index = e.RowIndex;
        int start = (index / PageSize) * PageSize;
        int generation = _pageGeneration;
        var token = _pageCts?.Token ?? CancellationToken.None;
        try
        {
            BarSong? song;
            if (_activeAlbum is null)
            {
                song = index < _snapshot.Favourites.Count ? _snapshot.Favourites[index] : null;
            }
            else
            {
                var album = _activeAlbum;
                var root = _snapshot.Root;
                song = _cachedPages.TryGetValue(start, out var page) && index - start < page.Count
                    ? page[index - start]
                    : (await Task.Run(() => _catalog.ReadPage(root, album, index, 1, token), token))
                        .FirstOrDefault();
            }
            if (song is null || _isClosing || IsDisposed || generation != _pageGeneration) return;
            if (!await Task.Run(() => File.Exists(song.Path), token))
            {
                MessageBox.Show(this,
                    "O arquivo aparece no indice do Bar, mas nao existe neste caminho:\n" + song.Path,
                    "Arquivo indisponivel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SongChosen?.Invoke(this, song.Path); // Original 3.4.5 LoadSong/Chordino.
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            if (!_isClosing && !IsDisposed)
                MessageBox.Show(this, ex.Message, "Abrir musica", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void StopLibraryTasks()
    {
        if (_isClosing) return;
        _isClosing = true;
        _filterDebounce.Stop();
        _filterDebounce.Dispose();
        _refreshCts?.Cancel();
        _pageCts?.Cancel();
        // Never synchronously wait for workers while closing the Form.
    }
}

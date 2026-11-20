using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Cadelao.BassHelper.Library;

/// <summary>
/// Read-only view of the CadelaoBar8800 catalog. Never creates, updates,
/// migrates, checkpoints or VACUUMs the Bar database or touches audio tags.
/// The small set of 4/5-star favourites is loaded once on a background thread;
/// album pages are fetched only on demand with ix_tracks_album.
/// </summary>
internal sealed class BarLibraryCatalog
{
    public static string DefaultCatalogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "CadelaoBar8800", "library.sqlite3");

    public string CatalogPath { get; }

    public BarLibraryCatalog(string? path = null) =>
        CatalogPath = path ?? DefaultCatalogPath;

    private SqliteConnection OpenReadOnly()
    {
        if (!File.Exists(CatalogPath))
            throw new FileNotFoundException(
                "A base permanente do CadelaoBar8800 nao foi encontrada. " +
                "Confira a pasta do catalogo em Documentos.", CatalogPath);

        // Pooling saves native SQLite setup costs on every album page.
        // ReadOnly + query_only provide independent layers of write protection.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = CatalogPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = true,
            DefaultTimeout = 2
        }.ToString());
        connection.Open();
        try
        {
            using var guard = connection.CreateCommand();
            guard.CommandText = "PRAGMA query_only=ON; PRAGMA busy_timeout=750;";
            guard.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static void VerifySchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('tracks','ratings')";
        if (Convert.ToInt32(command.ExecuteScalar()) != 2)
            throw new InvalidDataException(
                "Esta base nao tem as tabelas tracks/ratings esperadas no CadelaoBar8800.");
    }

    private string? ReadConfiguredRoot()
    {
        string settingsPath = Path.Combine(Path.GetDirectoryName(CatalogPath)!, "settings.json");
        try
        {
            if (!File.Exists(settingsPath)) return null;
            using var file = File.OpenRead(settingsPath);
            using var json = JsonDocument.Parse(file);
            if (json.RootElement.TryGetProperty("LibraryRoot", out var root) &&
                root.ValueKind == JsonValueKind.String)
            {
                string? value = root.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // The Bar may be replacing settings.json; never write it here.
        }
        return null;
    }

    public BarCatalogSnapshot ReadSnapshot(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var connection = OpenReadOnly();
        VerifySchema(connection);

        string? root = ReadConfiguredRoot();
        if (string.IsNullOrWhiteSpace(root))
        {
            using var findRoot = connection.CreateCommand();
            findRoot.CommandText = "SELECT root FROM tracks GROUP BY root ORDER BY COUNT(*) DESC LIMIT 1";
            root = findRoot.ExecuteScalar() as string;
        }
        root ??= "";
        if (root.Length == 0)
            return new BarCatalogSnapshot("", 0, Array.Empty<BarAlbum>(), Array.Empty<BarSong>());

        // IMPORTANT: ratings(path) is an indexed PK, and tracks(path) is an
        // indexed PK. CROSS JOIN forces a scan of the comparatively small
        // ratings list followed by indexed track lookups, rather than sorting
        // every track of a potentially 200 GB collection for every page.
        // The Bar defines stars=0..5; "mais de 3" means 4 or 5 exactly.
        var favourites = new List<BarSong>();
        var groups = new List<(string Artist, int Year, string Album, long RatedCount)>();
        using (var query = connection.CreateCommand())
        {
            query.CommandText = """
                SELECT t.path, t.title, t.artist, t.album, t.track_no,
                       t.duration_ms, r.stars, t.codec, t.group_artist, t.year
                FROM ratings AS r CROSS JOIN tracks AS t
                WHERE r.stars >= 4 AND t.path = r.path AND t.root = $root
                ORDER BY t.group_artist COLLATE NOCASE, t.year,
                         t.album COLLATE NOCASE, t.track_no, t.path COLLATE NOCASE
                """;
            query.Parameters.AddWithValue("$root", root);
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                string artistGroup = reader.GetString(8);
                int year = reader.GetInt32(9);
                string album = reader.GetString(3);
                favourites.Add(new BarSong(
                    reader.GetString(0),
                    InternationalMetadata.Display(reader.GetString(1)),
                    InternationalMetadata.Display(reader.GetString(2)),
                    InternationalMetadata.Display(album),
                    reader.GetInt32(4), reader.GetInt64(5),
                    Math.Clamp(reader.GetInt32(6), 0, 5), reader.GetString(7)));

                if (groups.Count != 0 &&
                    groups[^1].Year == year &&
                    string.Equals(groups[^1].Artist, artistGroup, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(groups[^1].Album, album, StringComparison.OrdinalIgnoreCase))
                {
                    var old = groups[^1];
                    groups[^1] = (old.Artist, old.Year, old.Album, old.RatedCount + 1);
                }
                else
                {
                    groups.Add((artistGroup, year, album, 1));
                }
            }
        }

        var albums = new List<BarAlbum>(groups.Count);
        foreach (var group in groups)
        {
            token.ThrowIfCancellationRequested();
            string label = $"{InternationalMetadata.Display(group.Artist)} - " +
                $"[{(group.Year > 0 ? group.Year.ToString() : "?")}] " +
                $"{InternationalMetadata.Display(group.Album)} ({group.RatedCount:N0} com 4-5 estrelas)";
            albums.Add(new BarAlbum(group.Artist, group.Year, group.Album, group.RatedCount, label));
        }
        return new BarCatalogSnapshot(root, favourites.Count, albums, favourites);
    }

    /// <summary>
    /// Count only the selected qualifying album using the Bar's album index.
    /// Called asynchronously when the user clicks an album; not N times while
    /// loading the left-hand list. An album shows ALL its tracks, even if only
    /// one track has 4/5 stars.
    /// </summary>
    public long ReadAlbumCount(string root, BarAlbum album, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var connection = OpenReadOnly();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM tracks INDEXED BY ix_tracks_album
            WHERE root = $root AND group_artist = $artist COLLATE NOCASE
                  AND year = $year AND album = $album COLLATE NOCASE
            """;
        AddAlbumParameters(command, root, album);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    /// <summary>Small album-only page, indexed by root/group_artist/year/album.</summary>
    public List<BarSong> ReadPage(string root, BarAlbum album, int offset, int limit,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var connection = OpenReadOnly();
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT t.path, t.title, t.artist, t.album, t.track_no,
                   t.duration_ms, COALESCE(r.stars, 0), t.codec
            FROM tracks AS t INDEXED BY ix_tracks_album
            LEFT JOIN ratings AS r ON r.path = t.path
            WHERE t.root = $root AND t.group_artist = $artist COLLATE NOCASE
                  AND t.year = $year AND t.album = $album COLLATE NOCASE
            ORDER BY t.track_no, t.path COLLATE NOCASE
            LIMIT $limit OFFSET $offset
            """;
        AddAlbumParameters(query, root, album);
        query.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 256));
        query.Parameters.AddWithValue("$offset", Math.Max(0, offset));
        var songs = new List<BarSong>();
        using var reader = query.ExecuteReader();
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            songs.Add(new BarSong(
                reader.GetString(0),
                InternationalMetadata.Display(reader.GetString(1)),
                InternationalMetadata.Display(reader.GetString(2)),
                InternationalMetadata.Display(reader.GetString(3)),
                reader.GetInt32(4), reader.GetInt64(5),
                Math.Clamp(reader.GetInt32(6), 0, 5), reader.GetString(7)));
        }
        return songs;
    }

    private static void AddAlbumParameters(SqliteCommand command, string root, BarAlbum album)
    {
        command.Parameters.AddWithValue("$root", root);
        command.Parameters.AddWithValue("$artist", album.Artist);
        command.Parameters.AddWithValue("$year", album.Year);
        command.Parameters.AddWithValue("$album", album.Album);
    }
}

// Count on BarAlbum is the NUMBER OF QUALIFYING 4/5-STAR TRACKS, not the
// total tracks in the album. The latter is loaded on selection.
internal sealed record BarAlbum(string Artist, int Year, string Album, long Count, string Label);
internal sealed record BarCatalogSnapshot(string Root, long Count,
    IReadOnlyList<BarAlbum> Albums, IReadOnlyList<BarSong> Favourites);
internal sealed record BarSong(string Path, string Title, string Artist, string Album,
    int TrackNumber, long DurationMs, int Stars, string Codec)
{
    public string DisplayDuration => DurationMs > 0
        ? TimeSpan.FromMilliseconds(DurationMs).ToString(DurationMs >= 3_600_000 ? @"h\:mm\:ss" : @"m\:ss")
        : "--:--";
    public string DisplayStars => new string('\u2605', Stars) + new string('\u2606', 5 - Stars);
}

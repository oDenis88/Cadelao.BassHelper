using Cadelao.BassHelper.UI;
using Cadelao.BassHelper.Analysis;
using Cadelao.BassHelper.Audio;
using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper;

public partial class MainForm : Form
{
    private readonly AudioPlayer _player = new();
    private AnalysisResult? _analysis;
    private CancellationTokenSource? _analysisCts;
    private int _generation;
    private bool _draggingSeek;
    private bool _changingSeek;
    private bool _closing;
    private bool _setupPermission;

    public MainForm()
    {
        InitializeComponent();
        _player.Volume = 0.8f;
        AttachEvents();
        UpdateActions();
        RefreshDisplay();
    }

    private void AttachEvents()
    {
        panelVocabulary.Resize += (_, _) =>
        {
            btnApplyChords.Left = panelVocabulary.Width - btnApplyChords.Width - 14;
            txtKnownChords.Left = btnApplyChords.Left - txtKnownChords.Width - 8;
        };
        panelNow.Resize += (_, _) =>
        {
            lblNext.Left = panelNow.Width - lblNext.Width - 21;
            lblBass.Left = panelNow.Width - lblBass.Width - 21;
            lblEvidence.Left = panelNow.Width - lblEvidence.Width - 21;
        };
        panelTransport.Resize += (_, _) =>
        {
            numSync.Left = panelTransport.Width - numSync.Width - 15;
            lblSync.Left = numSync.Left;
            trackVolume.Left = numSync.Left - trackVolume.Width - 13;
            lblVolume.Left = trackVolume.Left + 5;
            trackSeek.Width = Math.Max(140, trackVolume.Left - trackSeek.Left - 12);
            lblFile.Width = Math.Max(140, panelTransport.Width - 35);
        };
        panelGrid.Resize += (_, _) => gridChords.Height = Math.Max(50, panelGrid.Height - 27);
        footer.Resize += (_, _) => progressAnalysis.Left = footer.Width - progressAnalysis.Width - 8;
        btnOpen.Click += (_, _) => OpenFromDialog();
        btnDemo.Click += (_, _) => OpenDemo();
        btnPlay.Click += (_, _) =>
        {
            timeline.FollowPlayhead = true;
            UpdateFollowButton();
            _player.Play();
            UpdateActions();
        };
        btnPause.Click += (_, _) => { _player.Pause(); UpdateActions(); };
        btnStop.Click += (_, _) => { _player.Stop(); UpdateActions(); RefreshDisplay(); };
        btnAnalyze.Click += (_, _) => StartAnalysis();
        btnTools.Click += async (_, _) => await PrepareChordinoAsync();
        btnExport.Click += (_, _) => ExportChords();
        btnApplyChords.Click += (_, _) => ApplyVocabulary(true);
        txtKnownChords.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            ApplyVocabulary(true);
        };
        trackVolume.Scroll += (_, _) => _player.Volume = trackVolume.Value / 100f;
        numSync.ValueChanged += (_, _) => RefreshDisplay();
        trackSeek.MouseDown += (_, _) => _draggingSeek = true;
        trackSeek.MouseUp += (_, _) => { _draggingSeek = false; SeekBySlider(); };
        trackSeek.Scroll += (_, _) =>
        {
            if (!_changingSeek && !_draggingSeek) SeekBySlider();
        };
        timeline.SeekRequested += (_, args) => Seek(args.Seconds);
        timeline.ViewStartChanged += (_, _) => SyncTimelineScroll();
        timelineHeader.Resize += (_, _) =>
        {
            btnTimelineFollow.Left = timelineHeader.Width - btnTimelineFollow.Width - 5;
            numTimelineZoom.Left = btnTimelineFollow.Left - numTimelineZoom.Width - 10;
            lblTimelineZoom.Left = numTimelineZoom.Left - lblTimelineZoom.Width - 9;
        };
        numTimelineZoom.ValueChanged += (_, _) =>
        {
            timeline.ViewSeconds = (double)numTimelineZoom.Value;
            SyncTimelineScroll();
        };
        scrollTimeline.Scroll += (_, _) =>
        {
            timeline.FollowPlayhead = false;
            timeline.ViewStart = scrollTimeline.Value;
            UpdateFollowButton();
        };
        btnTimelineFollow.Click += (_, _) =>
        {
            timeline.FollowPlayhead = !timeline.FollowPlayhead;
            UpdateFollowButton();
        };
        gridChords.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && gridChords.Rows[e.RowIndex].Tag is ChordSegment chord)
                Seek(chord.Start);
        };
        gridChords.CellEndEdit += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colChord.Index) return;
            DataGridViewRow row = gridChords.Rows[e.RowIndex];
            if (row.Tag is not ChordSegment segment) return;
            string value = Convert.ToString(row.Cells[colChord.Index].Value) ?? "";
            if (!ChordVocabulary.TryParse(value, out var parsed))
            {
                row.Cells[colChord.Index].Value = segment.EffectiveName;
                ColorChordCell(row, colChord.Index);
                SetStatus("Cifra invalida. Exemplos: Dm, Am, E, F# ou B7 (vira B).");
                return;
            }
            segment.OverrideChord = parsed!.Name == segment.Name ? null : parsed.Name;
            row.Cells[colChord.Index].Value = segment.EffectiveName;
            ColorChordCell(row, colChord.Index);
            timeline.Invalidate();
            RefreshDisplay();
            if (_player.FilePath is not null && _analysis is not null)
                ChordEditStore.Save(_player.FilePath, _analysis.ChordSegments);
            SetStatus("Correcao salva em seu perfil, sem alterar a transcricao bruta de Chordino.");
        };
        gridChords.DataError += (_, e) => e.ThrowException = false;
        timerPlayhead.Tick += (_, _) => RefreshDisplay();
        _player.PlaybackFinished += (_, _) =>
        {
            if (_closing || IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)(() =>
            {
                if (_closing || IsDisposed) return;
                _player.Seek(0);
                RefreshDisplay();
                UpdateActions();
            }));
        };
    }

    private void OpenFromDialog()
    {
        using var picker = new OpenFileDialog
        {
            Title = "Selecione uma gravacao de brega para estudar no baixo",
            Filter = "Audio MP3/WAV (*.mp3;*.wav)|*.mp3;*.wav|MP3 (*.mp3)|*.mp3|WAV (*.wav)|*.wav",
            CheckFileExists = true,
            Multiselect = false
        };
        if (picker.ShowDialog(this) == DialogResult.OK) LoadSong(picker.FileName);
    }

    private void OpenDemo()
    {
        string demo = Path.Combine(AppContext.BaseDirectory, "Demo", "demo_brega_triad_Dm_Am_E.wav");
        if (!File.Exists(demo))
        {
            MessageBox.Show(this, "O demo WAV nao foi encontrado na pasta Demo.", "Demo", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }
        txtKnownChords.Text = "Dm, Am, E";
        LoadSong(demo);
    }

    private void LoadSong(string fileName)
    {
        try
        {
            CancelAnalysis();
            _player.Open(fileName);
            _analysis = null;
            gridChords.Rows.Clear();
            lblFile.Text = Path.GetFileName(fileName);
            timeline.Analysis = null;
            timeline.AnalyzedTime = 0;
            timeline.Duration = _player.Duration.TotalSeconds;
            timeline.ViewStart = 0;
            timeline.FollowPlayhead = true;
            UpdateFollowButton();
            SyncTimelineScroll();
            progressAnalysis.Value = 0;
            SetStatus("Audio aberto. Analisando as triades em segundo plano...");
            UpdateActions();
            RefreshDisplay();
            StartAnalysis();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Nao foi possivel abrir o audio:\n" + ex.Message,
                "Erro de audio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Erro ao abrir arquivo: " + ex.Message);
            UpdateActions();
        }
    }

    private async void StartAnalysis()
    {
        string? path = _player.FilePath;
        if (path is null) return;
        // Chordino transcribes unrestricted chords. A user's reference list must
        // never suppress a real detected change or force an incorrect chord.
        txtKnownChords.ForeColor = Color.White;
        CancelAnalysis();
        int ownGeneration = ++_generation;
        var cts = new CancellationTokenSource();
        _analysisCts = cts;
        _analysis = null;
        gridChords.Rows.Clear();
        timeline.Analysis = null;
        timeline.AnalyzedTime = 0;
        progressAnalysis.Value = 0;
        UpdateActions();
        var progress = new Progress<AnalysisProgress>(p =>
        {
            if (_closing || IsDisposed || ownGeneration != _generation || _analysis?.IsComplete == true) return;
            // 0-50% represents the exact decoded fraction; native Chordino itself
            // does not provide a progress API, so never pretend we know its percent.
            int percent = Math.Clamp((int)(50 * p.SecondsAnalyzed / Math.Max(0.01, p.TotalSeconds)), 0, 50);
            progressAnalysis.Value = percent;
            timeline.AnalyzedTime = p.SecondsAnalyzed;
            SetStatus(p.FramesProcessed > 0 ? "Analise nativa concluida." :
                $"Preparando WAV temporario para Chordino... {percent * 2}%");
        });
        try
        {
            await EnsureEngineWithConsentAsync(cts.Token);
            if (_closing || IsDisposed || ownGeneration != _generation) return;
            SetStatus("Executando Chordino (NNLS Chroma); sem instalacao externa...");
            AnalysisResult result = await ChordinoEngine.AnalyzeAsync(path, progress, cts.Token);
            if (_closing || IsDisposed || ownGeneration != _generation) return;
            ChordEditStore.Restore(path, result.ChordSegments);
            _analysis = result;
            timeline.Analysis = result;
            timeline.AnalyzedTime = result.Duration;
            progressAnalysis.Value = 100;
            RefreshGrid();
            RefreshDisplay();
            UpdateActions();
            SetStatus($"CHORDINO: {result.ChordSegments.Count} segmentos brutos preservados" +
                (result.Beats.Count > 0 ? $" | {result.Beats.Count} batidas QM" :
                    " | batidas QM indisponiveis (tempo de Chordino preservado)") +
                " | correcao manual salva automaticamente.");
        }
        catch (OperationCanceledException)
        {
            // Trocar arquivo cancela o processamento, nao o audio.
        }
        catch (Exception ex)
        {
            if (_closing || IsDisposed || ownGeneration != _generation) return;
            SetStatus("Motor Chordino: " + ex.Message);
            MessageBox.Show(this, "Nao foi possivel preparar ou executar Chordino:\n" + ex.Message +
                "\n\nClique PREPARAR MOTOR para tentar novamente.\n" +
                "Diagnostico: %LOCALAPPDATA%\\Cadelao.BassHelper\\Native\\win-x64\\bootstrap_status.json",
                "Chordino automatico",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (ReferenceEquals(_analysisCts, cts)) _analysisCts = null;
            cts.Dispose();
        }
    }

    private void CancelAnalysis()
    {
        _generation++;
        var cts = _analysisCts;
        _analysisCts = null;
        try { cts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    private void ApplyVocabulary(bool notify)
    {
        try
        {
            var words = string.IsNullOrWhiteSpace(txtKnownChords.Text)
                ? Array.Empty<ChordDefinition>() : ChordVocabulary.Parse(txtKnownChords.Text).ToArray();
            txtKnownChords.ForeColor = Color.White;
            if (notify) SetStatus(words.Length == 0
                ? "Sem referencias: Chordino reconhece livremente."
                : "Referencias para estudo: " + string.Join(", ", words.Select(w => w.Name)) +
                  ". NENHUM acorde encontrado sera escondido ou substituido.");
            RefreshGrid();
        }
        catch (FormatException ex)
        {
            txtKnownChords.ForeColor = Color.FromArgb(250, 133, 133);
            if (notify) SetStatus(ex.Message);
        }
    }

    private async Task EnsureEngineWithConsentAsync(CancellationToken token)
    {
        if (await PortableChordinoRuntime.IsReadyAsync(token)) return;
        if (!_setupPermission)
        {
            if (_closing || IsDisposed) throw new OperationCanceledException(token);
            var choice = MessageBox.Show(this,
                "O BassHelper pode preparar o motor Chordino automaticamente.\n\n" +
                "Na primeira vez sera necessario acessar a internet para obter:\n" +
                "- Vamp Simple Host, SDK oficial Windows x64 (licenca BSD)\n" +
                "- Chordino/NNLS Chroma Windows x64 (GPL v2 ou posterior)\n\n" +
                "Os arquivos ficam apenas em %LOCALAPPDATA%\\Cadelao.BassHelper, " +
                "sem instalar nada no Windows.\n\nAutoriza baixar esses componentes?",
                "Preparar motor Chordino — uma unica vez", MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (choice != DialogResult.Yes) throw new OperationCanceledException(token);
            _setupPermission = true;
        }
        var info = new Progress<EngineSetupProgress>(p =>
        {
            if (_closing || IsDisposed || token.IsCancellationRequested) return;
            SetStatus($"Preparando Chordino: {p.Stage} — {p.Detail}");
            progressAnalysis.Value = Math.Clamp(p.Percent, 0, 100);
        });
        await PortableChordinoRuntime.EnsureAsync(info, token);
    }

    private async Task PrepareChordinoAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        btnTools.Enabled = false;
        try
        {
            await EnsureEngineWithConsentAsync(cts.Token);
            var host = ChordinoEngine.FindHost();
            if (host is null) throw new InvalidOperationException("Host indisponivel apos preparacao.");
            var check = await ChordinoEngine.VerifyAsync(host, cts.Token);
            if (!check.Chordino) throw new InvalidOperationException(check.Detail);
            MessageBox.Show(this,
                "Motor Chordino verificado!\n\nHost: " + host +
                "\nPlugin privado: " + PortableChordinoRuntime.PluginDirectory +
                "\n\nNenhum instalador externo necessario.",
                "Cadelao.BassHelper", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetStatus("Chordino pronto para reconhecimento de acordes.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Preparacao cancelada. Musica e controles de audio preservados.");
        }
        catch (Exception ex)
        {
            SetStatus("Preparacao nao concluida: " + ex.Message);
            MessageBox.Show(this,
                "Nao foi possivel preparar Chordino:\n" + ex.Message +
                "\n\nDiagnostico: " + PortableChordinoRuntime.LogFile,
                "Preparacao automatica", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { if (!_closing && !IsDisposed) btnTools.Enabled = true; }
    }

    private static void ColorChordCell(DataGridViewRow row, int chordColumn)
    {
        string chordName = Convert.ToString(row.Cells[chordColumn].Value) ?? "--";
        Color accent = ChordColorPalette.Accent(chordName);
        Color readable = ChordColorPalette.ContrastText(accent);
        var style = row.Cells[chordColumn].Style;
        style.BackColor = accent;
        style.ForeColor = readable;
        style.SelectionBackColor = accent;
        style.SelectionForeColor = readable;
    }

    private void RefreshGrid()
    {
        gridChords.SuspendLayout();
        try
        {
            gridChords.Rows.Clear();
            if (_analysis is null) return;
            foreach (ChordSegment c in _analysis.ChordSegments)
            {
                int i = gridChords.Rows.Add(MusicNotes.FormatDetailed(c.Start),
                    MusicNotes.FormatDetailed(c.End), c.EffectiveName,
                    string.IsNullOrEmpty(c.RawName) ? "CHORDINO" : c.RawName);
                gridChords.Rows[i].Tag = c;
                ColorChordCell(gridChords.Rows[i], colChord.Index);
                gridChords.Rows[i].Cells[colChord.Index].ToolTipText =
                    "Chifra bruta: " + c.RawName + " | Edite e confirme para salvar.";
            }
        }
        finally { gridChords.ResumeLayout(); }
    }

    private void RefreshDisplay()
    {
        if (_closing || IsDisposed) return;
        double duration = _player.Duration.TotalSeconds;
        double position = _player.Position.TotalSeconds;
        if (_player.IsPlaying) position += (double)numSync.Value / 1000.0;
        position = Math.Clamp(position, 0, duration);
        lblTime.Text = MusicNotes.FormatTime(position);
        lblDuration.Text = MusicNotes.FormatTime(duration);
        if (!_draggingSeek)
        {
            _changingSeek = true;
            try { trackSeek.Value = duration <= 0 ? 0 : Math.Clamp((int)(10000 * position / duration), 0, 10000); }
            finally { _changingSeek = false; }
        }
        timeline.Duration = duration;
        timeline.CurrentTime = position;
        SyncTimelineScroll();
        ChordSegment? current = _analysis?.AtChord(position);
        NoteSegment? bass = _analysis?.At(position);
        lblCurrent.Text = current?.EffectiveName ?? "--";
        lblCurrent.ForeColor = ChordColorPalette.Accent(current?.EffectiveName);
        lblQuality.ForeColor = ChordColorPalette.Accent(current?.EffectiveName);
        if (current is not null && ChordVocabulary.TryParse(current.EffectiveName, out var triad))
        {
            lblQuality.Text = triad!.Quality;
            lblTriad.Text = "TRIADE     " + triad.Notes;
            lblShape.Text = "CASAS     " + FourStringBass.Suggest(triad).Describe(triad);
        }
        else
        {
            lblQuality.Text = "--";
            lblTriad.Text = "TRIADE  --";
            lblShape.Text = "CASAS  --";
        }
        lblBass.Text = current is not null &&
            ChordVocabulary.TryParse(current.EffectiveName, out var bassTriad)
            ? "FUNDAMENTAL   " + MusicNotes.Names[bassTriad!.Root]
            : "FUNDAMENTAL   --";
        lblEvidence.Text = current is null ? "MOTOR   --" :
            "CHORDINO BRUTO   " + current.RawName;
        var next = _analysis?.ChordSegments.FirstOrDefault(s => s.Start > position + 0.04);
        lblNext.Text = next is null ? "PROXIMA   --" :
            $"PROXIMA   {next.EffectiveName}   {MusicNotes.FormatTime(next.Start)}";
        lblNext.ForeColor = ChordColorPalette.Accent(next?.EffectiveName);
    }

    private void UpdateFollowButton()
    {
        btnTimelineFollow.Text = timeline.FollowPlayhead ? "SEGUIR: ON" : "SEGUIR: OFF";
        btnTimelineFollow.BackColor = timeline.FollowPlayhead
            ? Color.FromArgb(30, 112, 112) : Color.FromArgb(44, 58, 76);
    }

    private void SyncTimelineScroll()
    {
        if (scrollTimeline.IsDisposed) return;
        int total = Math.Max(1, (int)Math.Ceiling(timeline.Duration));
        int view = Math.Clamp((int)Math.Ceiling(timeline.VisibleDuration), 1, total);
        int large = Math.Max(1, view);
        int maximum = Math.Max(large, total) - 1;
        if (scrollTimeline.LargeChange != large) scrollTimeline.LargeChange = large;
        if (scrollTimeline.Maximum != maximum) scrollTimeline.Maximum = maximum;
        scrollTimeline.SmallChange = Math.Max(1, view / 10);
        scrollTimeline.Enabled = total > view;
        int maxStart = Math.Max(0, maximum - large + 1);
        int wanted = Math.Clamp((int)Math.Round(timeline.ViewStart), 0, maxStart);
        if (scrollTimeline.Value != wanted) scrollTimeline.Value = wanted;
    }

    private void SeekBySlider()
    {
        if (_player.IsLoaded) Seek(_player.Duration.TotalSeconds * trackSeek.Value / 10000.0);
    }
    private void Seek(double seconds)
    {
        if (!_player.IsLoaded) return;
        _player.Seek(seconds);
        timeline.FollowPlayhead = true;
        timeline.CenterOn(seconds);
        UpdateFollowButton();
        RefreshDisplay();
    }

    private void ExportChords()
    {
        if (_analysis?.IsComplete != true || _player.FilePath is null) return;
        using var picker = new SaveFileDialog
        {
            Title = "Exportar frases de baixo / triades",
            Filter = "CSV (*.csv)|*.csv|LRC sincronizado (*.lrc)|*.lrc|TXT (*.txt)|*.txt",
            FilterIndex = 1,
            FileName = Path.GetFileNameWithoutExtension(_player.FilePath) + "_tríades.csv",
            OverwritePrompt = true
        };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ChordExporter.Save(picker.FileName, _analysis);
            SetStatus("Cifras salvas: " + picker.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Nao foi possivel exportar:\n" + ex.Message, "Exportacao",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateActions()
    {
        bool loaded = _player.IsLoaded;
        btnPlay.Enabled = loaded;
        btnPause.Enabled = loaded;
        btnStop.Enabled = loaded;
        btnAnalyze.Enabled = loaded;
        btnExport.Enabled = _analysis?.IsComplete == true;
        trackSeek.Enabled = loaded;
    }

    private void SetStatus(string text) => lblStatus.Text = text;

    private void DisposeResources()
    {
        if (_closing) return;
        _closing = true;
        timerPlayhead.Stop();
        CancelAnalysis();
        _player.Dispose();
    }
}

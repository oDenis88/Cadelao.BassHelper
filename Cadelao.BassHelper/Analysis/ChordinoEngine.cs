using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Cadelao.BassHelper.Models;
using NAudio.Wave;

namespace Cadelao.BassHelper.Analysis;

/// <summary>
/// This invokes the real open-source Chordino Vamp plugin using the Vamp SDK
/// command-line host. This is NOT a replacement algorithm or a simulated analyzer.
/// The optional one-time native bootstrap downloads upstream binaries to a
/// private directory after the user's consent; the host is verified before use.
/// </summary>
public sealed class ChordinoEngine
{
    private static readonly Regex ChordLine = new(
        @"^\s*(\d+(?:[.,]\d+)?)\s*:\s*(\S+)\s*$", RegexOptions.Compiled);
    private static readonly Regex BeatLine = new(
        @"^\s*(\d+(?:[.,]\d+)?)\s*:\s*(.*)$", RegexOptions.Compiled);
    public const string ChordinoId = "nnls-chroma:chordino";
    public const string BeatId = "qm-vamp-plugins:qm-barbeattracker:beats";

    public static string? FindHost()
    {
        string? forced = Environment.GetEnvironmentVariable("BASSHELPER_VAMP_HOST");
        if (File.Exists(forced)) return forced;
        // Portable native engines are authoritative even when an older 3.0 user
        // configuration still points at a missing/broken manually selected host.
        string localHost = Path.Combine(AppContext.BaseDirectory, "Tools", "Host", "vamp-simple-host.exe");
        if (File.Exists(localHost)) return localHost;
        if (File.Exists(PortableChordinoRuntime.PreferredHost))
            return PortableChordinoRuntime.PreferredHost;
        var names = new[] { "VampSimpleHost.exe", "vamp-simple-host.exe", "vamp-simple-host" };
        var dirs = new List<string>
        {
            PortableChordinoRuntime.HostDirectory,
            Path.Combine(AppContext.BaseDirectory, "Tools", "Host"),
            Path.Combine(AppContext.BaseDirectory, "Tools"),
            AppContext.BaseDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vamp Plugins"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vamp Plugin SDK"),
        };
        var setting = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cadelao.BassHelper", "host_path.txt");
        if (File.Exists(setting))
        {
            string p = File.ReadAllText(setting).Trim();
            if (File.Exists(p)) return p;
        }
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(path)) dirs.AddRange(path.Split(Path.PathSeparator));
        foreach (string dir in dirs.Distinct(StringComparer.OrdinalIgnoreCase))
            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                var p = Path.Combine(dir, name);
                if (File.Exists(p)) return p;
            }
        return null;
    }

    public static void SaveHost(string fullPath)
    {
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Host Vamp nao encontrado", fullPath);
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cadelao.BassHelper");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "host_path.txt"), Path.GetFullPath(fullPath), Encoding.UTF8);
    }

    private static void SetPluginPath(ProcessStartInfo info)
    {
        string? setting = Environment.GetEnvironmentVariable("BASSHELPER_VAMP_PATH");
        var dirs = new List<string>();
        if (!string.IsNullOrWhiteSpace(setting)) dirs.Add(setting);
        dirs.Add(Path.Combine(AppContext.BaseDirectory, "Tools", "Plugins"));
        dirs.Add(PortableChordinoRuntime.PluginDirectory);
        dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vamp Plugins"));
        dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vamp Plugins"));
        if (info.Environment.TryGetValue("VAMP_PATH", out var previous) && !string.IsNullOrWhiteSpace(previous))
            dirs.Add(previous);
        var valid = dirs.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
        info.Environment["VAMP_PATH"] = string.Join(Path.PathSeparator, valid);
    }

    private static ProcessStartInfo CreateProcess(string host, params string[] args)
    {
        var p = new ProcessStartInfo(host)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string arg in args) p.ArgumentList.Add(arg); // reliable paths with spaces/unicode
        SetPluginPath(p);
        return p;
    }

    private static async Task<(int Code, string Out, string Err)> RunAsync(
        ProcessStartInfo start, CancellationToken token)
    {
        using var p = new Process { StartInfo = start };
        if (!p.Start()) throw new InvalidOperationException("Nao foi possivel iniciar o host Vamp.");
        Task<string> stdout = p.StandardOutput.ReadToEndAsync(token);
        Task<string> stderr = p.StandardError.ReadToEndAsync(token);
        try
        {
            await p.WaitForExitAsync(token).ConfigureAwait(false);
            return (p.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* already exited */ }
            try { await p.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
            throw;
        }
    }

    public static async Task<(bool Chordino, bool Beat, string Detail)> VerifyAsync(string host, CancellationToken token)
    {
        var r = await RunAsync(CreateProcess(host, "--list-ids"), token).ConfigureAwait(false);
        string combined = r.Out + "\n" + r.Err;
        bool chord = combined.Contains("nnls-chroma:chordino", StringComparison.OrdinalIgnoreCase);
        bool beat = combined.Contains("qm-vamp-plugins:qm-barbeattracker", StringComparison.OrdinalIgnoreCase);
        return (chord, beat, combined.Length <= 1600 ? combined : combined[..1600]);
    }

    public static async Task<AnalysisResult> AnalyzeAsync(string fileName,
        IProgress<AnalysisProgress>? progress, CancellationToken token, bool useCache = true)
    {
        if (!File.Exists(fileName)) throw new FileNotFoundException("Arquivo de audio nao encontrado", fileName);
        string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cadelao.BassHelper", "AnalysisCache");
        Directory.CreateDirectory(cacheDir);
        FileInfo info = new(fileName);
        string id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            Path.GetFullPath(fileName).ToUpperInvariant() + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks + "|CHORDINO_V3")));
        string cacheFile = Path.Combine(cacheDir, id + ".json");
        ChordinoCache? stored = null;
        if (useCache && File.Exists(cacheFile))
        {
            try { stored = JsonSerializer.Deserialize<ChordinoCache>(await File.ReadAllTextAsync(cacheFile, token)); }
            catch (JsonException) { }
        }
        if (stored is not null && stored.Events is { Count: > 0 } && stored.Duration > 0)
        {
            progress?.Report(new AnalysisProgress(stored.Duration, stored.Duration, stored.Events.Count));
            return BuildResult(stored);
        }

        string? host = FindHost();
        if (host is null)
            throw new InvalidOperationException(
                "Host Vamp x64 nao encontrado. Clique PREPARAR MOTOR (download integrado na primeira vez).");
        var available = await VerifyAsync(host, token).ConfigureAwait(false);
        if (!available.Chordino)
            throw new InvalidOperationException(
                "O host Vamp nao encontrou nnls-chroma:chordino (x64). " +
                "Clique PREPARAR MOTOR para configurar Chordino x64 no perfil local.\n\nPlugins encontrados:\n" +
                available.Detail);

        string work = Path.Combine(Path.GetTempPath(), "CadelaoBassHelper_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        string wav = Path.Combine(work, "input.wav");
        string chordOutput = Path.Combine(work, "chordino.txt");
        string beatOutput = Path.Combine(work, "beats.txt");
        try
        {
            double duration = await Task.Run(() => DecodeWav16(fileName, wav, progress, token), token)
                .ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            // Work in a separate temp folder: never overwrite the music or its metadata.
            var r = await RunAsync(CreateProcess(host, ChordinoId, wav, "-o", chordOutput), token)
                .ConfigureAwait(false);
            if (r.Code != 0 || !File.Exists(chordOutput))
                throw new InvalidOperationException("Chordino nao concluiu. Exit=" + r.Code + "\n" +
                    (r.Err + "\n" + r.Out).Trim()[..Math.Min(1200, (r.Err + "\n" + r.Out).Trim().Length)]);
            List<ChordinoEvent> events = ParseChordino(await File.ReadAllTextAsync(chordOutput, token));
            if (events.Count == 0) throw new InvalidOperationException("Chordino retornou zero acordes. Saida: " + r.Out + " " + r.Err);
            List<double> beats = new();
            if (available.Beat)
            {
                // The bar/beat tracker is OPTIONAL. Never fail chord analysis if it fails.
                try
                {
                    var b = await RunAsync(CreateProcess(host, BeatId, wav, "-o", beatOutput), token)
                        .ConfigureAwait(false);
                    if (b.Code == 0 && File.Exists(beatOutput))
                        beats = ParseBeats(await File.ReadAllTextAsync(beatOutput, token));
                }
                catch (OperationCanceledException) { throw; }
                catch { /* no beat plugin for this host/output combination */ }
            }
            var data = new ChordinoCache("chordino:nnls-chroma:vamp-simple-host", duration, events, beats);
            // Atomic cache write. No guessed progress while native DSP is running.
            string pending = cacheFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(pending, JsonSerializer.Serialize(data, new JsonSerializerOptions
                { WriteIndented = true }), token);
            File.Move(pending, cacheFile, overwrite: true);
            progress?.Report(new AnalysisProgress(duration, duration, events.Count));
            return BuildResult(data);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Stream decoded audio to PCM 16-bit WAV. Preserve the audio sampling rate and
    /// origin at t=0, with no trimming or time-stretching that could shift chords.
    /// </summary>
    private static double DecodeWav16(string path, string output,
        IProgress<AnalysisProgress>? progress, CancellationToken token)
    {
        using var reader = new AudioFileReader(path);
        using var writer = new WaveFileWriter(output, new WaveFormat(reader.WaveFormat.SampleRate, 16, 1));
        int channels = reader.WaveFormat.Channels;
        if (channels < 1) throw new InvalidOperationException("Audio sem canais.");
        float[] samples = new float[16384 * channels];
        byte[] pcm = new byte[16384 * 2];
        long frames = 0;
        int got;
        while ((got = reader.Read(samples.AsSpan())) > 0)
        {
            token.ThrowIfCancellationRequested();
            int n = got / channels;
            for (int i = 0; i < n; i++)
            {
                float sample = 0;
                for (int ch = 0; ch < channels; ch++) sample += samples[i * channels + ch];
                sample = Math.Clamp(sample / channels, -1f, 1f);
                short pcm16 = (short)Math.Round(sample * 32767f);
                pcm[2 * i] = (byte)(pcm16 & 255);
                pcm[2 * i + 1] = (byte)((pcm16 >> 8) & 255);
            }
            writer.Write(pcm, 0, n * 2);
            frames += n;
            if (frames % (reader.WaveFormat.SampleRate * 3L) < n)
                progress?.Report(new AnalysisProgress(Math.Min(reader.TotalTime.TotalSeconds,
                    (double)frames / reader.WaveFormat.SampleRate), reader.TotalTime.TotalSeconds, 0));
        }
        return (double)frames / reader.WaveFormat.SampleRate;
    }

    public static List<ChordinoEvent> ParseChordino(string output)
    {
        var list = new List<ChordinoEvent>();
        foreach (string line in output.Split('\n'))
        {
            var m = ChordLine.Match(line.Trim());
            if (!m.Success) continue;
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double t)) continue;
            if (!double.IsFinite(t) || t < 0) continue;
            string chord = m.Groups[2].Value.Trim();
            if (chord.Length < 1 || chord.Length > 30) continue;
            list.Add(new ChordinoEvent(t, chord));
        }
        return list.OrderBy(x => x.Time).DistinctBy(x => (x.Time, x.Label)).ToList();
    }

    public static List<double> ParseBeats(string output)
    {
        var list = new List<double>();
        foreach (var line in output.Split('\n'))
        {
            var m = BeatLine.Match(line.Trim());
            if (!m.Success) continue;
            if (double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double t) && t >= 0 && double.IsFinite(t))
                list.Add(t);
        }
        return list.Order().Distinct().ToList();
    }

    private static AnalysisResult BuildResult(ChordinoCache cache)
    {
        var result = new AnalysisResult(Array.Empty<NoteFrame>(), cache.Duration, true);
        result.SetExternalChords(ChordinoTimeline.Build(cache.Events, cache.Duration, cache.Beats));
        result.Beats = cache.Beats;
        result.Source = cache.Engine;
        return result;
    }
}


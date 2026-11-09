using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cadelao.BassHelper.Analysis;

public sealed record EngineSetupProgress(string Stage, int Percent, string Detail);

/// <summary>
/// Fetches the official Vamp x64 host and upstream Windows-x64 Chordino release
/// into a private application folder. No MSI, administrator privilege, system
/// VAMP_PATH edit, Python, or separate software installation is required.
/// This is a ONE-TIME internet bootstrap, not an embedded offline binary.
/// </summary>
public static class PortableChordinoRuntime
{
    private const string HostZipUrl =
        "https://github.com/vamp-plugins/vamp-plugin-sdk/releases/download/vamp-plugin-sdk-v2.10/vamp-plugin-sdk-2.10.0-binaries-win64-msvc2017.zip";
    private const string PluginReleaseUrl =
        "https://api.github.com/repos/jotapemj/nnls-chroma-win64/releases/tags/v1.1-win64";
    private const long MaxDownload = 120L * 1024 * 1024;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Cadelao.BassHelper", "Native", "win-x64");
    public static string HostDirectory => Path.Combine(Root, "Host");
    public static string PluginDirectory => Path.Combine(Root, "Plugins");
    public static string PreferredHost => Path.Combine(HostDirectory, "vamp-simple-host.exe");
    public static string PluginDll => Path.Combine(PluginDirectory, "nnls-chroma.dll");
    public static string LogFile => Path.Combine(Root, "bootstrap_status.json");

    public static async Task<bool> IsReadyAsync(CancellationToken token)
    {
        string? host = ChordinoEngine.FindHost();
        if (host is null) return false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(18));
            var checkedHost = await ChordinoEngine.VerifyAsync(host, deadline.Token).ConfigureAwait(false);
            return checkedHost.Chordino;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return false; }
        catch (Exception) { return false; }
    }

    public static async Task<string> EnsureAsync(IProgress<EngineSetupProgress>? progress,
        CancellationToken token)
    {
        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Root);
            if (await IsReadyAsync(token).ConfigureAwait(false))
                return "Chordino disponivel. Nenhuma preparacao necessaria.";

            if (!OperatingSystem.IsWindows() || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture
                != System.Runtime.InteropServices.Architecture.X64)
                throw new PlatformNotSupportedException("Este motor portatil exige Windows x64.");

            string stage = Path.Combine(Root, ".download-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            try
            {
                using var http = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.All
                }) { Timeout = TimeSpan.FromMinutes(5) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Cadelao.BassHelper/3.1.0");
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                progress?.Report(new("Host Vamp", 8, "Baixando host oficial Windows x64..."));
                if (!File.Exists(PreferredHost) || !IsPeX64(PreferredHost))
                {
                    string zip = Path.Combine(stage, "vamp-sdk.zip");
                    await DownloadAsync(http, HostZipUrl, zip, token).ConfigureAwait(false);
                    InstallHostZip(zip, HostDirectory);
                    CheckX64(PreferredHost, "vamp-simple-host.exe");
                    RecordDownload("host", HostZipUrl, zip);
                }

                progress?.Report(new("Chordino", 46, "Consultando release publica do plugin x64..."));
                if (!File.Exists(PluginDll) || !IsPeX64(PluginDll))
                {
                    var assets = await LoadReleaseAssetsAsync(http, token).ConfigureAwait(false);
                    Directory.CreateDirectory(PluginDirectory);
                    var archive = assets.Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(a => a.Name.Contains("win64", StringComparison.OrdinalIgnoreCase) ||
                                                a.Name.Contains("x64", StringComparison.OrdinalIgnoreCase))
                        .ThenByDescending(a => a.Name.Contains("nnls", StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                    if (archive is not null)
                    {
                        string zip = Path.Combine(stage, "nnls-chroma-x64.zip");
                        await DownloadAsync(http, archive.Url, zip, token).ConfigureAwait(false);
                        VerifyDigestIfPresent(zip, archive.Digest);
                        InstallPluginZip(zip, PluginDirectory);
                        RecordDownload("chordino", archive.Url, zip);
                    }
                    else
                    {
                        var dll = assets.FirstOrDefault(a =>
                            a.Name.Equals("nnls-chroma.dll", StringComparison.OrdinalIgnoreCase));
                        if (dll is null)
                            throw new InvalidDataException("A release do Chordino nao fornece ZIP x64 nem nnls-chroma.dll. " +
                                "Confira " + PluginReleaseUrl);
                        string pending = Path.Combine(stage, "nnls-chroma.dll");
                        await DownloadAsync(http, dll.Url, pending, token).ConfigureAwait(false);
                        VerifyDigestIfPresent(pending, dll.Digest);
                        CheckX64(pending, "nnls-chroma.dll");
                        File.Copy(pending, PluginDll, true);
                        RecordDownload("chordino", dll.Url, pending);
                        foreach (string extra in new[] { "nnls-chroma.cat", "nnls-chroma.n3" })
                        {
                            var entry = assets.FirstOrDefault(a =>
                                a.Name.Equals(extra, StringComparison.OrdinalIgnoreCase));
                            if (entry is null) continue;
                            try { await DownloadAsync(http, entry.Url,
                                Path.Combine(PluginDirectory, extra), token).ConfigureAwait(false); }
                            catch (HttpRequestException) { /* metadata is optional */ }
                        }
                    }
                    CheckX64(PluginDll, "nnls-chroma.dll");
                }

                progress?.Report(new("Verificacao", 88, "Testando Vamp + Chordino de verdade..."));
                // This checks the actual Windows plugin loader, not just file existence.
                using var verifyTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                verifyTimeout.CancelAfter(TimeSpan.FromSeconds(25));
                var verify = await ChordinoEngine.VerifyAsync(PreferredHost, verifyTimeout.Token)
                    .ConfigureAwait(false);
                if (!verify.Chordino)
                    throw new InvalidOperationException("Os arquivos foram preparados, mas o host nao listou " +
                        "nnls-chroma:chordino. Veja log e dependencias do Windows. Saida: " + verify.Detail);

                await SaveStatusAsync("READY", null, token).ConfigureAwait(false);
                progress?.Report(new("Pronto", 100, "Chordino x64 verificado. Pronto para transcrever."));
                return "Chordino preparado e verificado em " + Root;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                await SaveStatusAsync("TIMEOUT", "Tempo esgotado ao preparar o motor.",
                    CancellationToken.None).ConfigureAwait(false);
                throw new TimeoutException("Tempo esgotado na preparacao do motor Chordino.");
            }
            catch (Exception ex)
            {
                await SaveStatusAsync("ERROR", ex.Message, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            finally
            {
                try { Directory.Delete(stage, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        finally { Gate.Release(); }
    }

    private static async Task<List<ReleaseAsset>> LoadReleaseAssetsAsync(HttpClient http, CancellationToken token)
    {
        using var response = await http.GetAsync(PluginReleaseUrl, HttpCompletionOption.ResponseHeadersRead,
            token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: token).ConfigureAwait(false);
        var result = new List<ReleaseAsset>();
        if (!json.RootElement.TryGetProperty("assets", out var assets) ||
            assets.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Release do Chordino sem assets.");
        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var name) ||
                !asset.TryGetProperty("browser_download_url", out var url)) continue;
            string? n = name.GetString(), u = url.GetString();
            if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(u)) continue;
            if (Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
                uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                string? digest = asset.TryGetProperty("digest", out var d) &&
                    d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                result.Add(new ReleaseAsset(n, u, digest));
            }
        }
        return result;
    }

    private static async Task DownloadAsync(HttpClient http, string uri, string destination,
        CancellationToken token)
    {
        var url = new Uri(uri);
        if (url.Scheme != Uri.UriSchemeHttps ||
            !(url.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase) ||
              url.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Origem de download nao autorizada: " + url.Host);
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead,
            token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long declared && declared > MaxDownload)
            throw new InvalidDataException("Download maior que o limite de seguranca.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + ".part";
        try
        {
            await using (var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
            await using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write,
                FileShare.None, 81920, useAsync: true))
            {
                byte[] buffer = new byte[81920];
                long total = 0;
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                    if (read == 0) break;
                    total += read;
                    if (total > MaxDownload) throw new InvalidDataException("Download excedeu 120 MiB.");
                    await file.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                }
                await file.FlushAsync(token).ConfigureAwait(false);
            }
            // Windows cannot replace an open .part file; close it before File.Move.
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void InstallHostZip(string zipPath, string directory)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var host = zip.Entries.FirstOrDefault(e =>
            e.Name.Equals("vamp-simple-host.exe", StringComparison.OrdinalIgnoreCase) ||
            e.Name.Equals("VampSimpleHost.exe", StringComparison.OrdinalIgnoreCase));
        if (host is null) throw new InvalidDataException("SDK Vamp x64 nao contem vamp-simple-host.exe.");
        Directory.CreateDirectory(directory);
        CopyEntry(host, PreferredHost);
        // The SDK Windows host may depend on companion DLLs from its binary folder.
        // Extract native DLLs only (not sources or arbitrary scripts).
        string dir = host.FullName.Replace('\\', '/');
        int slash = dir.LastIndexOf('/');
        string prefix = slash >= 0 ? dir[..(slash + 1)] : "";
        foreach (var entry in zip.Entries)
        {
            if (!entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
            if (!entry.FullName.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            CopyEntry(entry, Path.Combine(directory, entry.Name));
        }
        // Fallback: some SDK distributions put support DLLs in a sibling lib/bin folder.
        foreach (var entry in zip.Entries)
        {
            if (!entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
            string dest = Path.Combine(directory, entry.Name);
            if (!File.Exists(dest)) CopyEntry(entry, dest);
        }
    }

    private static void InstallPluginZip(string zipPath, string directory)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var dll = zip.Entries.FirstOrDefault(e =>
            e.Name.Equals("nnls-chroma.dll", StringComparison.OrdinalIgnoreCase));
        if (dll is null) throw new InvalidDataException("Pacote Chordino nao contem nnls-chroma.dll.");
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, "nnls-chroma.temporary.dll");
        CopyEntry(dll, temporary);
        try { CheckX64(temporary, "nnls-chroma.dll"); File.Move(temporary, PluginDll, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        foreach (var optional in new[] { "nnls-chroma.cat", "nnls-chroma.n3" })
        {
            var data = zip.Entries.FirstOrDefault(e => e.Name.Equals(optional,
                StringComparison.OrdinalIgnoreCase));
            if (data is not null) CopyEntry(data, Path.Combine(directory, optional));
        }
    }

    private static void CopyEntry(ZipArchiveEntry entry, string destination)
    {
        if (entry.Length > MaxDownload) throw new InvalidDataException("Entrada ZIP muito grande.");
        using var source = entry.Open();
        using var file = new FileStream(destination, FileMode.Create, FileAccess.Write);
        source.CopyTo(file);
    }

    public static bool IsPeX64(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length < 0x100) return false;
            Span<byte> dos = stackalloc byte[64];
            stream.ReadExactly(dos);
            if (dos[0] != 'M' || dos[1] != 'Z') return false;
            int peOffset = BinaryPrimitives.ReadInt32LittleEndian(dos[60..64]);
            if (peOffset < 64 || peOffset > stream.Length - 24) return false;
            stream.Position = peOffset;
            Span<byte> pe = stackalloc byte[6];
            stream.ReadExactly(pe);
            return pe[0] == 'P' && pe[1] == 'E' && pe[2] == 0 && pe[3] == 0 &&
                BinaryPrimitives.ReadUInt16LittleEndian(pe[4..6]) == 0x8664;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static void CheckX64(string path, string name)
    {
        if (!IsPeX64(path)) throw new InvalidDataException(
            "Binario invalido ou nao x64: " + name + ". O motor nao sera executado.");
    }

    private static void RecordDownload(string component, string url, string source)
    {
        string file = Path.Combine(Root, "download_provenance.jsonl");
        var entry = new { Timestamp = DateTimeOffset.UtcNow, Component = component, Url = url,
            SHA256 = GetSha256(source) };
        File.AppendAllText(file, JsonSerializer.Serialize(entry) + Environment.NewLine, Encoding.UTF8);
    }

    private static void VerifyDigestIfPresent(string path, string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest)) return;
        const string prefix = "sha256:";
        if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
        string expected = digest[prefix.Length..].Trim();
        if (expected.Length == 64 && !string.Equals(expected, GetSha256(path),
            StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SHA-256 do arquivo Chordino diverge da release GitHub.");
    }

    private static string GetSha256(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static Task SaveStatusAsync(string status, string? error, CancellationToken token)
    {
        string json = JsonSerializer.Serialize(new
        {
            Schema = "CADELAO_PORTABLE_CHORDINO_V1", TimeUtc = DateTimeOffset.UtcNow,
            Status = status, Error = error, Host = PreferredHost, Plugin = PluginDll,
            HostExists = File.Exists(PreferredHost), PluginExists = File.Exists(PluginDll),
            PluginIsX64 = IsPeX64(PluginDll)
        }, new JsonSerializerOptions { WriteIndented = true });
        return File.WriteAllTextAsync(LogFile, json, token);
    }

    private sealed record ReleaseAsset(string Name, string Url, string? Digest);
}

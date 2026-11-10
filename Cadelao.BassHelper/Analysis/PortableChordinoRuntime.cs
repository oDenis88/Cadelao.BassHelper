namespace Cadelao.BassHelper.Analysis;

/// <summary>
/// Fully offline runtime: use only the exact native binaries shipped with the app.
/// Does not have an HTTP client, bootstrap, or first-run installation step.
/// </summary>
public static class PortableChordinoRuntime
{
    public static string Root => Path.Combine(AppContext.BaseDirectory, "Tools");
    public static string HostDirectory => Path.Combine(Root, "Host");
    public static string PluginDirectory => Path.Combine(Root, "Plugins");
    public static string PreferredHost => Path.Combine(HostDirectory, "vamp-simple-host.exe");
    public static string PluginDll => Path.Combine(PluginDirectory, "nnls-chroma.dll");

    public static async Task<bool> IsReadyAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows() ||
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture !=
                System.Runtime.InteropServices.Architecture.X64 ||
            !File.Exists(PreferredHost) || !File.Exists(PluginDll)) return false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var check = await ChordinoEngine.VerifyAsync(PreferredHost, deadline.Token)
                .ConfigureAwait(false);
            return check.Chordino;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return false; }
        catch (Exception) { return false; }
    }
}

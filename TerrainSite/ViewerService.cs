using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace TerrainSite;

// opens the unity exe in 3D view mode for one run (a window with a free camera)
// only one view window at a time: opening another one closes the old one
public class ViewerService(IOptions<GeneratorOptions> options, ILogger<ViewerService> logger)
{
    readonly GeneratorOptions opt = options.Value;
    readonly object gate = new();
    Process? current;

    public string? Open(Run run)
    {
        // the config the run really used is saved next to its images
        string runDir = Path.Combine(opt.RunsDir, run.Folder);
        string configPath = Path.Combine(runDir, "config.json");
        if (!File.Exists(configPath)) return "this run has no config.json (it didn't finish)";

        lock (gate)
        {
            if (current is { HasExited: false })
            {
                try { current.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }   // it closed by itself in the meantime
            }

            var psi = new ProcessStartInfo(opt.ExePath) { UseShellExecute = false };
            foreach (string arg in new[]
                     {
                         "-config", configPath,
                         "-view",
                         "-logFile", Path.Combine(runDir, "view.log"),
                         "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720"
                     })
                psi.ArgumentList.Add(arg);

            current = Process.Start(psi);
            logger.LogInformation("3D view opened for {Folder}", run.Folder);
        }
        return null;   // no error
    }
}
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TerrainSite;

// runs in the background for as long as the site is on
// takes runs from the queue, starts the unity exe for each one and saves the results
public class GeneratorWorker(IServiceScopeFactory scopes, RunQueue queue,
    IOptions<GeneratorOptions> options, ILogger<GeneratorWorker> log) : BackgroundService
{
    private readonly GeneratorOptions _opt = options.Value;

    // camelCase so "VoronoiPeaks" becomes "voronoiPeaks", exactly like the unity fields
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_opt.ConfigsDir);
        Directory.CreateDirectory(_opt.RunsDir);
        await RecoverAsync();

        // waits here until something is in the queue, one run at a time
        await foreach (int id in queue.ReadAllAsync(stoppingToken))
        {
            try { await ProcessAsync(id, stoppingToken); }
            catch (Exception e) { log.LogError(e, "run {Id} crashed the worker", id); }
        }
    }

    // after a restart of the site: queued runs go back in the queue,
    // running ones were cut in the middle so they count as failed
    private async Task RecoverAsync()
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        foreach (var r in await db.Runs.Where(r => r.State == RunState.Running).ToListAsync())
        {
            r.State = RunState.Failed;
            r.Message = "site restarted during the run";
        }
        await db.SaveChangesAsync();

        var queued = await db.Runs.Where(r => r.State == RunState.Queued)
                                  .OrderBy(r => r.Id).Select(r => r.Id).ToListAsync();
        foreach (int id in queued) queue.Enqueue(id);
    }

    private async Task ProcessAsync(int id, CancellationToken ct)
    {
        // the worker lives forever but the db context is short lived, so a new scope per run
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var run = await db.Runs.FindAsync([id], ct);
        if (run == null || run.State != RunState.Queued) return;

        run.State = RunState.Running;
        await db.SaveChangesAsync(ct);

        try
        {
            await GenerateAsync(run, ct);
        }
        catch (Exception e)
        {
            // exe missing, folder without rights etc -> the run fails but the site keeps going
            run.State = RunState.Failed;
            run.Message = e.Message;
        }

        run.FinishedAt = DateTime.Now;
        // None so it still saves even if the site is shutting down
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task GenerateAsync(Run run, CancellationToken ct)
    {
        // 1. writing the config file that unity reads
        var config = new TerrainConfig { Seed = run.Seed, Preset = run.Preset };
        string configPath = Path.Combine(_opt.ConfigsDir, run.Folder + ".json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, Json), ct);

        string outDir = Path.Combine(_opt.RunsDir, run.Folder);
        Directory.CreateDirectory(outDir);

        // 2. starting the exe exactly like i did from powershell
        var psi = new ProcessStartInfo(_opt.ExePath) { UseShellExecute = false };
        string[] args =
        [
            "-config", configPath,
            "-out", outDir,
            "-logFile", Path.Combine(outDir, "player.log"), // unity's own log, per run
            "-screen-fullscreen", "0", "-screen-width", "640", "-screen-height", "360"
        ];
        foreach (string a in args) psi.ArgumentList.Add(a); // ArgumentList handles the quotes by itself

        int? exitCode = null;
        using (var p = Process.Start(psi) ?? throw new Exception("could not start the exe"))
        {
            // if unity hangs i dont wait forever
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));
            try
            {
                await p.WaitForExitAsync(timeout.Token);
                exitCode = p.ExitCode;
            }
            catch (OperationCanceledException)
            {
                p.Kill(entireProcessTree: true);
            }
        }

        // 3. reading what unity left in the run folder
        var status = await ReadJsonAsync<RunStatus>(Path.Combine(outDir, "status.json"));

        if (exitCode == null)
        {
            run.State = RunState.Failed;
            run.Message = $"timeout after {_opt.TimeoutSeconds}s";
        }
        else if (status == null || status.State != "done")
        {
            run.State = RunState.Failed;
            run.Message = $"exit code {exitCode}, status.json missing or not finished";
        }
        else if (status.Code == 0)
        {
            string metricsPath = Path.Combine(outDir, "metrics.json");
            var m = await ReadJsonAsync<RunMetrics>(metricsPath);
            run.State = RunState.Done;
            if (m != null)
            {
                run.HeightHash = m.HeightHash;
                run.DurationMs = m.DurationMs;
                run.MeanHeight = m.MeanHeight;
                run.HeightStdDev = m.HeightStdDev;
                run.MeanSlopeDeg = m.MeanSlopeDeg;
                run.MetricsJson = await File.ReadAllTextAsync(metricsPath, ct);
            }
        }
        else
        {
            // 2 = rejected by the quality check, anything else = error
            run.State = status.Code == 2 ? RunState.Rejected : RunState.Failed;
            run.Message = status.Message;
        }
    }

    private static async Task<T?> ReadJsonAsync<T>(string path)
    {
        if (!File.Exists(path)) return default;
        try { return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), Json); }
        catch (JsonException) { return default; }
    }
}
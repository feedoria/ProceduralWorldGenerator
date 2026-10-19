using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TerrainSite;

// runs in the background for as long as the site is up
// takes runs from the queue, samples the preset, starts the unity exe, reads what it left behind
public class GeneratorWorker(
    IServiceScopeFactory scopes,
    RunQueue queue,
    PresetLibrary presets,
    IOptions<GeneratorOptions> options,
    ILogger<GeneratorWorker> logger) : BackgroundService
{
    readonly GeneratorOptions opt = options.Value;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    record Attempt(RunState State, string? Message, RunMetrics? Metrics, string? MetricsJson);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(opt.ConfigsDir);
        Directory.CreateDirectory(opt.RunsDir);
        await RecoverAsync(ct);

        await foreach (int id in queue.ReadAllAsync(ct))
        {
            try { await ProcessAsync(id, ct); }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "run {Id} crashed", id);
            }
        }
    }

    // runs left over from the last time the site stopped
    async Task RecoverAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        foreach (var run in await db.Runs.Where(r => r.State == RunState.Running).ToListAsync(ct))
        {
            run.State = RunState.Failed;
            run.Message = "the site stopped during the run";
            run.FinishedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);

        var queued = await db.Runs.Where(r => r.State == RunState.Queued).OrderBy(r => r.Id).Select(r => r.Id).ToListAsync(ct);
        foreach (int id in queued) queue.Enqueue(id);
    }

    async Task ProcessAsync(int id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();   // DbContext is scoped, the worker is a singleton
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var run = await db.Runs.FindAsync([id], ct);
        if (run == null) return;

        run.State = RunState.Running;
        await db.SaveChangesAsync(ct);

        Attempt result = new(RunState.Failed, "no attempt was made", null, null);
        for (int attempt = 1; attempt <= opt.MaxAttempts; attempt++)
        {
            int seed = run.RequestedSeed + attempt - 1;
            var config = presets.Sample(run.Preset, seed);

            run.Attempts = attempt;
            run.Seed = seed;
            run.ConfigJson = JsonSerializer.Serialize(config, Json);
            await db.SaveChangesAsync(ct);   // so the page can show "attempt 2"

            result = await GenerateAsync(run, config, ct);
            if (result.State != RunState.Rejected) break;
            logger.LogInformation("run {Id}: seed {Seed} rejected ({Msg})", run.Id, seed, result.Message);
        }

        run.State = result.State;
        run.Message = result.Message;
        run.FinishedAt = DateTime.UtcNow;
        if (result.Metrics is { } m)
        {
            run.HeightHash = m.HeightHash;
            run.DurationMs = m.DurationMs;
            run.MeanHeight = m.MeanHeight;
            run.HeightStdDev = m.HeightStdDev;
            run.MeanSlopeDeg = m.MeanSlopeDeg;
            run.MetricsJson = result.MetricsJson;
        }
        await db.SaveChangesAsync(CancellationToken.None);   // save the result even if the site is stopping
    }

    async Task<Attempt> GenerateAsync(Run run, TerrainConfig config, CancellationToken ct)
    {
        string configPath = Path.Combine(opt.ConfigsDir, run.Folder + ".json");
        string outDir = Path.Combine(opt.RunsDir, run.Folder);

        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, Json), ct);
        if (Directory.Exists(outDir))
        {
            try { Directory.Delete(outDir, true); }   // clean folder for every attempt
            catch (IOException e) { logger.LogWarning("could not clean {Dir}: {Msg}", outDir, e.Message); }
        }
        Directory.CreateDirectory(outDir);

        var psi = new ProcessStartInfo(opt.ExePath) { UseShellExecute = false };
        foreach (string arg in new[]
                 {
                     "-config", configPath,
                     "-out", outDir,
                     "-logFile", Path.Combine(outDir, "player.log"),
                     "-screen-fullscreen", "0", "-screen-width", "640", "-screen-height", "360"
                 })
            psi.ArgumentList.Add(arg);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start the generator");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(opt.TimeoutSeconds));

        try
        {
            await proc.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            proc.Kill(entireProcessTree: true);
            if (ct.IsCancellationRequested) throw;   // the site is stopping, not a timeout
            return new Attempt(RunState.Failed, $"timeout after {opt.TimeoutSeconds} s", null, null);
        }

        var status = await ReadJsonAsync<RunStatus>(Path.Combine(outDir, "status.json"));
        string metricsPath = Path.Combine(outDir, "metrics.json");
        string? metricsJson = File.Exists(metricsPath) ? await File.ReadAllTextAsync(metricsPath, ct) : null;
        var metrics = metricsJson == null ? null : JsonSerializer.Deserialize<RunMetrics>(metricsJson, Json);

        // status.json is the contract, the exit code is only for when it's missing
        var state = status?.State switch
        {
            "done" => RunState.Done,
            "rejected" => RunState.Rejected,
            _ => RunState.Failed
        };
        string? message = status?.Message;
        if (status == null) message = $"no status.json (exit code {proc.ExitCode})";

        return new Attempt(state, message, metrics, metricsJson);
    }

    static async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), Json); }
        catch (JsonException) { return null; }
    }
}

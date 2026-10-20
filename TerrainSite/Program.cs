using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using TerrainSite;

var builder = WebApplication.CreateBuilder(args);

var genSection = builder.Configuration.GetSection("Generator");
builder.Services.Configure<GeneratorOptions>(genSection);
var gen = genSection.Get<GeneratorOptions>() ?? throw new InvalidOperationException("no Generator section in appsettings.json");
Directory.CreateDirectory(gen.DataDir);
Directory.CreateDirectory(gen.RunsDir);   // has to exist before the file provider below

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite($"Data Source={Path.Combine(gen.DataDir, "generator.db")}"));
builder.Services.AddSingleton<RunQueue>();
builder.Services.AddSingleton<PresetLibrary>();
builder.Services.AddHostedService<GeneratorWorker>();
builder.Services.AddSingleton<ViewerService>();

var app = builder.Build();

// creates the db if it's missing; it does NOT update an old one, so delete generator.db when Run changes
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

app.UseDefaultFiles();   // "/" -> index.html
app.UseStaticFiles();    // wwwroot
app.UseStaticFiles(new StaticFileOptions   // images of the runs, straight from data/runs
{
    FileProvider = new PhysicalFileProvider(gen.RunsDir),
    RequestPath = "/runs"
});

app.MapGet("/api/presets", (PresetLibrary presets) =>
    presets.All.Select(p => new { p.Name, p.Label, p.Description }));

app.MapPost("/api/runs", async (CreateRunRequest req, AppDb db, RunQueue queue, PresetLibrary presets) =>
{
    string name = string.IsNullOrWhiteSpace(req.Preset) ? "deal" : req.Preset.Trim();
    var preset = presets.Find(name);
    if (preset == null) return Results.BadRequest(new { error = $"unknown preset '{name}'" });

    int seed = req.Seed ?? Random.Shared.Next(1, 1_000_000);
    var run = new Run
    {
        Preset = preset.Name,
        RequestedSeed = seed,
        Seed = seed,
        State = RunState.Queued,
        CreatedAt = DateTime.UtcNow
    };
    db.Runs.Add(run);
    await db.SaveChangesAsync();
    queue.Enqueue(run.Id);
    return Results.Ok(ToDto(run));
});

app.MapGet("/api/runs", async (AppDb db) =>
    (await db.Runs.OrderByDescending(r => r.Id).Take(20).ToListAsync()).Select(ToDto));

app.MapGet("/api/runs/{id:int}", async (int id, AppDb db) =>
    await db.Runs.FindAsync(id) is { } run ? Results.Ok(ToDto(run)) : Results.NotFound());

// opens the 3D window on this computer (the site and unity run on the same machine)
app.MapPost("/api/runs/{id:int}/view", async (int id, AppDb db, ViewerService viewer) =>
{
    var run = await db.Runs.FindAsync(id);
    if (run == null) return Results.NotFound();
    if (run.State is not (RunState.Done or RunState.Rejected))
        return Results.BadRequest(new { error = "the run isn't finished" });

    string? error = viewer.Open(run);
    return error == null ? Results.Ok() : Results.BadRequest(new { error });
});

app.Run();

// what the page gets, not the db row itself
static object ToDto(Run r)
{
    bool hasImages = r.State is RunState.Done or RunState.Rejected;
    string v = (r.FinishedAt?.Ticks ?? 0).ToString();   // new url after a retry, so the browser doesn't show the old image
    return new
    {
        r.Id,
        r.Preset,
        r.RequestedSeed,
        r.Seed,
        r.Attempts,
        State = r.State.ToString(),
        r.Message,
        r.CreatedAt,
        r.FinishedAt,
        r.HeightHash,
        r.DurationMs,
        Metrics = ParseJson(r.MetricsJson),
        Config = ParseJson(r.ConfigJson),
        Images = hasImages
            ? new
            {
                Top = $"/runs/{r.Folder}/top.png?v={v}",
                Oblique = $"/runs/{r.Folder}/oblique.png?v={v}",
                Preview = $"/runs/{r.Folder}/preview.png?v={v}"
            }
            : null
    };
}

static JsonElement? ParseJson(string? json) =>
    string.IsNullOrEmpty(json) ? null : JsonDocument.Parse(json).RootElement.Clone();

record CreateRunRequest(int? Seed, string? Preset);
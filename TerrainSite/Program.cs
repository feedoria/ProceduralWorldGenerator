using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using TerrainSite;

var builder = WebApplication.CreateBuilder(args);

// the "Generator" section from appsettings.json becomes a GeneratorOptions object
builder.Services.Configure<GeneratorOptions>(builder.Configuration.GetSection("Generator"));
var opt = builder.Configuration.GetSection("Generator").Get<GeneratorOptions>()!;

// the sqlite file sits in data/ next to the runs
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite($"Data Source={Path.Combine(opt.DataDir, "generator.db")}"));
builder.Services.AddSingleton<RunQueue>();
builder.Services.AddHostedService<GeneratorWorker>();

var app = builder.Build();

// creates the db and the tables the first time
Directory.CreateDirectory(opt.RunsDir);
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

// wwwroot/index.html is the page
app.UseDefaultFiles();
app.UseStaticFiles();
// data/runs is served at /runs so the page can show the pngs directly
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(opt.RunsDir),
    RequestPath = "/runs"
});

// the button: creates the run, puts it in the queue and answers right away with the id
app.MapPost("/api/runs", async (CreateRunRequest req, AppDb db, RunQueue queue) =>
{
    var run = new Run { Seed = req.Seed ?? Random.Shared.Next(0, int.MaxValue) };
    db.Runs.Add(run);
    await db.SaveChangesAsync();
    queue.Enqueue(run.Id);
    return Results.Ok(new { run.Id });
});

// the last 20 runs, for the history list
app.MapGet("/api/runs", async (AppDb db) =>
{
    var runs = await db.Runs.OrderByDescending(r => r.Id).Take(20).ToListAsync();
    return runs.Select(ToDto);
});

// one run, the page asks this every half second until it's done
app.MapGet("/api/runs/{id:int}", async (int id, AppDb db) =>
    await db.Runs.FindAsync(id) is { } r ? Results.Ok(ToDto(r)) : Results.NotFound());

app.Run();

// what the page gets: the run + the image links if it's done
static object ToDto(Run r) => new
{
    r.Id,
    r.Seed,
    r.Preset,
    State = r.State.ToString(),
    r.Message,
    r.CreatedAt,
    r.FinishedAt,
    r.HeightHash,
    r.DurationMs,
    Metrics = r.MetricsJson == null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(r.MetricsJson),
    Images = r.State == RunState.Done
        ? new
        {
            Top = $"/runs/{r.Folder}/top.png",
            Oblique = $"/runs/{r.Folder}/oblique.png",
            Preview = $"/runs/{r.Folder}/preview.png"
        }
        : null
};

record CreateRunRequest(int? Seed);
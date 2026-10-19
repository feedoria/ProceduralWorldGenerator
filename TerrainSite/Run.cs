using System.ComponentModel.DataAnnotations.Schema;

namespace TerrainSite;

public enum RunState { Queued, Running, Done, Failed, Rejected }

// one row in sqlite = one generation
public class Run
{
    public int Id { get; set; }
    public int Seed { get; set; }
    public string Preset { get; set; } = "hills";
    public RunState State { get; set; } = RunState.Queued;
    public string? Message { get; set; } // what went wrong, if something did
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? FinishedAt { get; set; }

    // the main metrics get their own columns so i can sort and plot them later
    public string? HeightHash { get; set; }
    public double? DurationMs { get; set; }
    public float? MeanHeight { get; set; }
    public float? HeightStdDev { get; set; }
    public float? MeanSlopeDeg { get; set; }
    // the whole metrics.json as text, so a new metric in unity doesnt need a new column right away
    public string? MetricsJson { get; set; }

    // folder name in data/runs, not saved in the db because it comes from the id
    [NotMapped] public string Folder => $"run_{Id:D6}";
}
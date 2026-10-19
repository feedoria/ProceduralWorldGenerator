using System.ComponentModel.DataAnnotations.Schema;

namespace TerrainSite;

public enum RunState { Queued, Running, Done, Failed, Rejected }

// one row in the database = one click on "Generează"
public class Run
{
    public int Id { get; set; }
    public string Preset { get; set; } = "deal";
    public int RequestedSeed { get; set; }   // the seed the user asked for
    public int Seed { get; set; }            // the seed actually used (+1 for every rejected try)
    public int Attempts { get; set; }
    public RunState State { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    // copied from metrics.json so they can be queried and compared later
    public string? HeightHash { get; set; }
    public long? DurationMs { get; set; }
    public double? MeanHeight { get; set; }
    public double? HeightStdDev { get; set; }
    public double? MeanSlopeDeg { get; set; }

    public string? MetricsJson { get; set; }
    public string? ConfigJson { get; set; }  // the exact config sent to unity

    [NotMapped] public string Folder => $"run_{Id:D6}";
}
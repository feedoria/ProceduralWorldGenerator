namespace TerrainSite;

// the "Generator" section from appsettings.json
public class GeneratorOptions
{
    public string ExePath { get; set; } = "";
    public string DataDir { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxAttempts { get; set; } = 3;   // a rejected terrain is retried with seed + 1

    public string ConfigsDir => Path.Combine(DataDir, "configs");
    public string RunsDir => Path.Combine(DataDir, "runs");
}
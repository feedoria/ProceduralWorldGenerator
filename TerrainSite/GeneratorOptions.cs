namespace TerrainSite;

// the paths come from appsettings.json so nothing is hardcoded in the code
public class GeneratorOptions
{
    public string ExePath { get; set; } = "";
    public string DataDir { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 120;

    public string ConfigsDir => Path.Combine(DataDir, "configs");
    public string RunsDir => Path.Combine(DataDir, "runs");
}
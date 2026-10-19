namespace TerrainSite;

// copies of the classes from unity, same field names so the json matches on both sides
// if i add a field in unity i add it here too, that's the contract between the two

public class PerlinLayer
{
    public float XScale { get; set; } = 0.004f;
    public float YScale { get; set; } = 0.004f;
    public int Octaves { get; set; } = 4;
    public float Persistance { get; set; } = 0.5f;
    public float HeightScale { get; set; } = 0.12f;
}

// what goes into config.json (unity reads it)
public class TerrainConfig
{
    public int SchemaVersion { get; set; } = 1;
    public int Seed { get; set; } = 42;
    public string Preset { get; set; } = "hills";

    public int VoronoiPeaks { get; set; } = 5;
    public float VoronoiFallOff { get; set; } = 0.2f;
    public float VoronoiDropOff { get; set; } = 0.6f;
    public float VoronoiMinHeight { get; set; } = 0.1f;
    public float VoronoiMaxHeight { get; set; } = 0.3f;
    public string VoronoiType { get; set; } = "Combined";

    public List<PerlinLayer> PerlinLayers { get; set; } = new()
    {
        new PerlinLayer(),
        new PerlinLayer { XScale = 0.03f, YScale = 0.03f, Octaves = 3, HeightScale = 0.02f }
    };

    public int SmoothIterations { get; set; } = 1;
    public float MinHeightRange { get; set; } = 0.01f;
}

// what unity writes in metrics.json
public class RunMetrics
{
    public int Seed { get; set; }
    public string Preset { get; set; } = "";
    public int Resolution { get; set; }
    public float MinHeight { get; set; }
    public float MaxHeight { get; set; }
    public float MeanHeight { get; set; }
    public float HeightStdDev { get; set; }
    public float HeightP10 { get; set; }
    public float HeightP50 { get; set; }
    public float HeightP90 { get; set; }
    public float MeanSlopeDeg { get; set; }
    public float[] SlopeBands { get; set; } = [];
    public string HeightHash { get; set; } = "";
    public double DurationMs { get; set; }
}

// what unity writes in status.json
public class RunStatus
{
    public string State { get; set; } = "";
    public int Code { get; set; }
    public string Message { get; set; } = "";
}
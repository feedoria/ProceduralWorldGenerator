namespace TerrainSite;

// mirror of the unity classes (TerrainConfig.cs, RunOutput.cs, Gen.cs)
// written as camelCase json, so the names match the unity fields exactly

public class PerlinLayer
{
    public float XScale { get; set; } = 0.006f;
    public float YScale { get; set; } = 0.006f;
    public int Octaves { get; set; } = 4;
    public float Persistance { get; set; } = 0.5f;
    public float HeightScale { get; set; } = 0.5f;
}

public class RidgeBlock
{
    public bool Enabled { get; set; }
    public float Weight { get; set; } = 0.3f;
    public List<PerlinLayer> Layers { get; set; } = [];
}

public class VoronoiBlock
{
    public bool Enabled { get; set; }
    public int Peaks { get; set; } = 2;
    public string Type { get; set; } = "Power";
    public float FallOff { get; set; } = 1f;
    public float DropOff { get; set; } = 1f;
    public float MinHeight { get; set; } = 0.85f;
    public float MaxHeight { get; set; } = 1f;
}

public class PerlinBlock
{
    public bool Enabled { get; set; } = true;
    public List<PerlinLayer> Layers { get; set; } = [];
}

public class ThermalBlock
{
    public bool Enabled { get; set; }
    public float TalusDeg { get; set; } = 35f;
    public int Passes { get; set; } = 3;
    public float Amount { get; set; } = 0.003f;
}

public class RiverBlock
{
    public bool Enabled { get; set; }
    public int Droplets { get; set; } = 50;
    public int Springs { get; set; } = 5;
    public float DepthM { get; set; } = 2f;
    public float LengthM { get; set; } = 200f;
}

public class SmoothBlock
{
    public bool Enabled { get; set; }
    public int Iterations { get; set; } = 1;
}

public class WaterBlock
{
    public bool Enabled { get; set; }
    public float Percentile { get; set; } = 0.04f;
}

public class TextureBlock
{
    public bool Enabled { get; set; } = true;
    public float SandBand { get; set; } = 0.02f;
    public float RockSlopeDeg { get; set; } = 90f;
    public bool SnowEnabled { get; set; }
    public float SnowPercentile { get; set; } = 0.93f;
}

public class ShorelineBlock
{
    public bool Enabled { get; set; }
}

public class TargetsBlock
{
    public bool Enabled { get; set; }
    public float MeanSlopeMin { get; set; }
    public float MeanSlopeMax { get; set; } = 90f;
    public float Under5Min { get; set; }
    public float Under5Max { get; set; } = 1f;
    public float Over30Min { get; set; }
    public float Over30Max { get; set; } = 1f;
}

public class TerrainConfig
{
    public int SchemaVersion { get; set; } = 2;
    public int Seed { get; set; }
    public string Preset { get; set; } = "deal";
    public float TerrainSizeM { get; set; } = 1000f;
    public float ReliefM { get; set; } = 100f;
    public float Exponent { get; set; } = 1f;
    public float MinHeightRange { get; set; } = 0.01f;

    public RidgeBlock Ridge { get; set; } = new();
    public VoronoiBlock Voronoi { get; set; } = new();
    public PerlinBlock Perlin { get; set; } = new();
    public ThermalBlock Thermal { get; set; } = new();
    public RiverBlock River { get; set; } = new();
    public SmoothBlock Smooth { get; set; } = new();
    public WaterBlock Water { get; set; } = new();
    public TextureBlock Textures { get; set; } = new();
    public ShorelineBlock Shoreline { get; set; } = new();
    public TargetsBlock Targets { get; set; } = new();
}

public class RunMetrics
{
    public int SchemaVersion { get; set; }
    public int Seed { get; set; }
    public string? Preset { get; set; }
    public int Resolution { get; set; }
    public float TerrainSizeM { get; set; }
    public float ReliefM { get; set; }
    public float MinHeight { get; set; }
    public float MaxHeight { get; set; }
    public float MeanHeight { get; set; }
    public float HeightStdDev { get; set; }
    public float HeightP10 { get; set; }
    public float HeightP50 { get; set; }
    public float HeightP90 { get; set; }
    public float RawHeightRange { get; set; }
    public float WaterHeight { get; set; }
    public float WaterLevelM { get; set; }
    public float MeanSlopeDeg { get; set; }
    public float[] SlopeBands { get; set; } = [];
    public string? HeightHash { get; set; }
    public long DurationMs { get; set; }
    public bool TargetsChecked { get; set; }
    public List<string> TargetFails { get; set; } = [];
}

public class RunStatus
{
    public string? State { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
    public string? StartedAt { get; set; }
    public string? FinishedAt { get; set; }
}
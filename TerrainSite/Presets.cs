using System.Text.Json;

namespace TerrainSite;

// a preset holds ranges, not values: [min, max] picks a value in between, [x] is fixed
// the seed decides the values, so the same preset + seed always gives the same config

public class LayerRange
{
    public double[] Scale { get; set; } = [0.006];
    public int[] Octaves { get; set; } = [4];
    public double[] Persistance { get; set; } = [0.5];
    public double[] HeightScale { get; set; } = [0.5];
}

public class RidgeRange
{
    public bool Enabled { get; set; }
    public double[] Weight { get; set; } = [0.3];
    public List<LayerRange> Layers { get; set; } = [];
}

public class VoronoiRange
{
    public bool Enabled { get; set; }
    public int[] Peaks { get; set; } = [2];
    public string Type { get; set; } = "Power";
    public double[] FallOff { get; set; } = [1];
    public double[] DropOff { get; set; } = [1];
    public double[] MinHeight { get; set; } = [0.85];
    public double[] MaxHeight { get; set; } = [1];
}

public class PerlinRange
{
    public bool Enabled { get; set; } = true;
    public List<LayerRange> Layers { get; set; } = [];
}

public class ThermalRange
{
    public bool Enabled { get; set; }
    public double[] TalusDeg { get; set; } = [35];
    public int[] Passes { get; set; } = [3];
    public double[] Amount { get; set; } = [0.003];
}

public class RiverRange
{
    public bool Enabled { get; set; }
    public int[] Droplets { get; set; } = [50];
    public int[] Springs { get; set; } = [5];
    public double[] DepthM { get; set; } = [2];
    public double[] LengthM { get; set; } = [200];
}

public class SmoothRange
{
    public bool Enabled { get; set; }
    public int[] Iterations { get; set; } = [1];
}

public class WaterRange
{
    public bool Enabled { get; set; }
    public double[] Percentile { get; set; } = [0.04];
}

public class TexturesRange
{
    public bool Enabled { get; set; } = true;
    public double[] SandBand { get; set; } = [0.02];
    public double[] RockSlopeDeg { get; set; } = [90];
    public bool SnowEnabled { get; set; }
    public double[] SnowPercentile { get; set; } = [0.93];
}

public class ShorelineRange
{
    public bool Enabled { get; set; }
}

// targets are not sampled, they are the accepted interval
public class TargetsRange
{
    public bool Enabled { get; set; }
    public double[] MeanSlopeDeg { get; set; } = [0, 90];
    public double[] Under5 { get; set; } = [0, 1];
    public double[] Over30 { get; set; } = [0, 1];
}

public class PresetDef
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public double TerrainSizeM { get; set; } = 1000;
    public double[] ReliefM { get; set; } = [100];
    public double[] Exponent { get; set; } = [1];   // 1 = no change, so deal and campie keep the same values
    public double MinHeightRange { get; set; } = 0.01;

    public RidgeRange Ridge { get; set; } = new();
    public VoronoiRange Voronoi { get; set; } = new();
    public PerlinRange Perlin { get; set; } = new();
    public ThermalRange Thermal { get; set; } = new();
    public RiverRange River { get; set; } = new();
    public SmoothRange Smooth { get; set; } = new();
    public WaterRange Water { get; set; } = new();
    public TexturesRange Textures { get; set; } = new();
    public ShorelineRange Shoreline { get; set; } = new();
    public TargetsRange Targets { get; set; } = new();
}

// reads TerrainSite/Presets/*.json once at startup and turns a preset + seed into a full config
public class PresetLibrary
{
    readonly Dictionary<string, PresetDef> presets = new(StringComparer.OrdinalIgnoreCase);

    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public PresetLibrary(IWebHostEnvironment env, ILogger<PresetLibrary> logger)
    {
        string dir = Path.Combine(env.ContentRootPath, "Presets");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"no presets folder: {dir}");

        foreach (string file in Directory.GetFiles(dir, "*.json").OrderBy(f => f))
        {
            var p = JsonSerializer.Deserialize<PresetDef>(File.ReadAllText(file), Json)
                    ?? throw new InvalidDataException($"empty preset: {file}");
            if (string.IsNullOrWhiteSpace(p.Name)) p.Name = Path.GetFileNameWithoutExtension(file);
            presets[p.Name] = p;
        }
        logger.LogInformation("presets loaded: {Names}", string.Join(", ", presets.Keys));
    }

    public IEnumerable<PresetDef> All => presets.Values;

    public PresetDef? Find(string name) => presets.GetValueOrDefault(name);

    public TerrainConfig Sample(string name, int seed)
    {
        var p = presets[name];
        var s = new Sampler(seed);   // the order of the calls below is part of the determinism, don't shuffle it

        return new TerrainConfig
        {
            SchemaVersion = 2,
            Seed = seed,
            Preset = p.Name,
            TerrainSizeM = (float)p.TerrainSizeM,
            ReliefM = s.F(p.ReliefM, 1),
            Exponent = s.F(p.Exponent, 2),
            MinHeightRange = (float)p.MinHeightRange,

            Ridge = new RidgeBlock
            {
                Enabled = p.Ridge.Enabled,
                Weight = s.F(p.Ridge.Weight, 3),
                Layers = p.Ridge.Layers.Select(s.Layer).ToList()
            },
            Voronoi = new VoronoiBlock
            {
                Enabled = p.Voronoi.Enabled,
                Peaks = s.I(p.Voronoi.Peaks),
                Type = p.Voronoi.Type,
                FallOff = s.F(p.Voronoi.FallOff),
                DropOff = s.F(p.Voronoi.DropOff),
                MinHeight = s.F(p.Voronoi.MinHeight),
                MaxHeight = s.F(p.Voronoi.MaxHeight)
            },
            Perlin = new PerlinBlock { Enabled = p.Perlin.Enabled, Layers = p.Perlin.Layers.Select(s.Layer).ToList() },
            Thermal = new ThermalBlock
            {
                Enabled = p.Thermal.Enabled,
                TalusDeg = s.F(p.Thermal.TalusDeg, 1),
                Passes = s.I(p.Thermal.Passes),
                Amount = s.F(p.Thermal.Amount)
            },
            River = new RiverBlock
            {
                Enabled = p.River.Enabled,
                Droplets = s.I(p.River.Droplets),
                Springs = s.I(p.River.Springs),
                DepthM = s.F(p.River.DepthM, 2),
                LengthM = s.F(p.River.LengthM, 0)
            },
            Smooth = new SmoothBlock { Enabled = p.Smooth.Enabled, Iterations = s.I(p.Smooth.Iterations) },
            Water = new WaterBlock { Enabled = p.Water.Enabled, Percentile = s.F(p.Water.Percentile, 4) },
            Textures = new TextureBlock
            {
                Enabled = p.Textures.Enabled,
                SandBand = s.F(p.Textures.SandBand, 4),
                RockSlopeDeg = s.F(p.Textures.RockSlopeDeg, 1),
                SnowEnabled = p.Textures.SnowEnabled,
                SnowPercentile = s.F(p.Textures.SnowPercentile, 4)
            },
            Shoreline = new ShorelineBlock { Enabled = p.Shoreline.Enabled },
            Targets = new TargetsBlock
            {
                Enabled = p.Targets.Enabled,
                MeanSlopeMin = (float)p.Targets.MeanSlopeDeg[0],
                MeanSlopeMax = (float)p.Targets.MeanSlopeDeg[^1],
                Under5Min = (float)p.Targets.Under5[0],
                Under5Max = (float)p.Targets.Under5[^1],
                Over30Min = (float)p.Targets.Over30[0],
                Over30Max = (float)p.Targets.Over30[^1]
            }
        };
    }

    // picks values from ranges, System.Random with a seed always gives the same sequence
    class Sampler(int seed)
    {
        readonly Random rng = new(seed);

        public float F(double[] r, int decimals = 5)
        {
            double v = r.Length == 1 ? r[0] : r[0] + rng.NextDouble() * (r[1] - r[0]);
            return (float)Math.Round(v, decimals);
        }

        public int I(int[] r) => r.Length == 1 ? r[0] : rng.Next(r[0], r[1] + 1);

        public PerlinLayer Layer(LayerRange l)
        {
            float scale = F(l.Scale);
            return new PerlinLayer
            {
                XScale = scale,
                YScale = scale,
                Octaves = I(l.Octaves),
                Persistance = F(l.Persistance, 3),
                HeightScale = F(l.HeightScale, 3)
            };
        }
    }
}
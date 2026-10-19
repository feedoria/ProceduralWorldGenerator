using System;
using System.Collections.Generic;
using UnityEngine;

// config v2: one block per pipeline step, every step can be turned off with "enabled"
// the site fills this from a preset (ranges + seed), unity only runs what it gets
// field names are the json names, the site writes the same names in camelCase

[Serializable]
public class PerlinLayer
{
    public float xScale = 0.006f;     // frequency, smaller = wider shapes
    public float yScale = 0.006f;
    public int octaves = 4;
    public float persistance = 0.5f;  // how much each octave keeps from the previous one
    public float heightScale = 0.5f;  // weight of the layer (Normalize rescales the total anyway)
}

[Serializable]
public class RidgeBlock
{
    public bool enabled = false;
    // the ridges end up between 0 and weight, so the layers added after them still fit under 1
    public float weight = 0.3f;
    // heightScale is ignored here, the step forces 1 so the noise crosses 0.5
    public List<PerlinLayer> layers = new List<PerlinLayer>
    {
        new PerlinLayer { xScale = 0.005f, yScale = 0.005f, octaves = 5, persistance = 0.5f, heightScale = 1f }
    };
}

[Serializable]
public class VoronoiBlock
{
    public bool enabled = false;
    public int peaks = 2;
    public string type = "Power";     // Linear, Power, SinPow, Combined, Sin, Perlin
    public float fallOff = 1f;
    public float dropOff = 1f;
    public float minHeight = 0.85f;   // peak heights, 0..1
    public float maxHeight = 1f;
}

[Serializable]
public class PerlinBlock
{
    public bool enabled = true;
    public List<PerlinLayer> layers = new List<PerlinLayer>
    {
        new PerlinLayer { xScale = 0.006f, yScale = 0.006f, octaves = 4, persistance = 0.5f, heightScale = 0.5f },  // big shapes
        new PerlinLayer { xScale = 0.02f, yScale = 0.02f, octaves = 3, persistance = 0.5f, heightScale = 0.08f }    // detail
    };
}

[Serializable]
public class ThermalBlock
{
    public bool enabled = true;
    public float talusDeg = 32f;      // slopes steeper than this angle slide down
    public int passes = 3;
    public float amount = 0.003f;     // how much material moves in one pass
}

[Serializable]
public class RiverBlock
{
    public bool enabled = true;
    public int droplets = 50;         // number of river sources
    public int springs = 5;           // rivers started from each source
    public float depthM = 2f;         // depth at the source, in meters
    public float lengthM = 200f;      // roughly how far a river goes before it fades, in meters
}

[Serializable]
public class SmoothBlock
{
    public bool enabled = true;
    public int iterations = 1;
}

[Serializable]
public class WaterBlock
{
    public bool enabled = true;
    public float percentile = 0.04f;  // 0.04 = the lowest 4% of the terrain is under water
}

[Serializable]
public class TextureBlock
{
    public bool enabled = true;
    // splat table order on GenTerrain: 0 sand, 1 grass, 2 rock, 3 snow
    public float sandBand = 0.02f;        // sand covers the next 2% of the terrain above the water
    public float rockSlopeDeg = 32f;      // rock above this slope (90 = no rock)
    public bool snowEnabled = false;
    public float snowPercentile = 0.93f;  // snow on the highest 7%
}

[Serializable]
public class ShorelineBlock
{
    public bool enabled = false;      // slow on big water areas, off by default
}

[Serializable]
public class TargetsBlock
{
    // what the finished terrain has to look like, otherwise the run is rejected (exit code 2)
    public bool enabled = true;
    public float meanSlopeMin = 4f, meanSlopeMax = 12f;   // degrees
    public float under5Min = 0.2f, under5Max = 0.6f;      // share of terrain flatter than 5 degrees
    public float over30Min = 0f, over30Max = 0.05f;       // share of terrain steeper than 30 degrees
}

[Serializable]
public class TerrainConfig
{
    public const int CurrentSchema = 2;

    public int schemaVersion = CurrentSchema;
    public int seed = 42;
    public string preset = "deal";

    // 1 unity unit = 1 meter
    public float terrainSizeM = 1000f;  // width and length
    public float reliefM = 100f;        // lowest to highest point after Normalize
    public float minHeightRange = 0.01f; // below this (before Normalize) the terrain counts as flat

    public RidgeBlock ridge = new RidgeBlock();
    public VoronoiBlock voronoi = new VoronoiBlock();
    public PerlinBlock perlin = new PerlinBlock();
    public ThermalBlock thermal = new ThermalBlock();
    public RiverBlock river = new RiverBlock();
    public SmoothBlock smooth = new SmoothBlock();
    public WaterBlock water = new WaterBlock();
    public TextureBlock textures = new TextureBlock();
    public ShorelineBlock shoreline = new ShorelineBlock();
    public TargetsBlock targets = new TargetsBlock();

    public static TerrainConfig FromJson(string json) => JsonUtility.FromJson<TerrainConfig>(json);
    public string ToJson() => JsonUtility.ToJson(this, true);
}
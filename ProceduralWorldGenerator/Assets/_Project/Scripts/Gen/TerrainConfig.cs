using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PerlinLayer
{
    public float xScale = 0.004f;
    public float yScale = 0.004f;
    public int octaves = 4;
    public float persistance = 0.5f;
    public float heightScale = 0.12f;
}

[Serializable]
public class TerrainConfig
{
    public int schemaVersion = 1;
    public int seed = 42;
    public string preset = "hills";

    // voronoi
    public int voronoiPeaks = 5;
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.3f;
    public string voronoiType = "Combined";

    // multiple perlin, offsets come from the seed
    public List<PerlinLayer> perlinLayers = new List<PerlinLayer>
    {
        new PerlinLayer(),
        new PerlinLayer { xScale = 0.03f, yScale = 0.03f, octaves = 3, heightScale = 0.02f }
    };

    public int smoothIterations = 1;

    // reject if terrain is basically flat
    public float minHeightRange = 0.01f;

    public static TerrainConfig FromJson(string json) => JsonUtility.FromJson<TerrainConfig>(json);
    public string ToJson() => JsonUtility.ToJson(this, true);
}
using System.Collections.Generic;

public static class Pipeline
{
    public static List<IStep> Build(TerrainConfig config)
    {
        return new List<IStep>
        {
            new ResetStep(),
            new VoronoiStep(),
            new MultiplePerlinStep(),
            new SmoothStep()
        };
    }
}
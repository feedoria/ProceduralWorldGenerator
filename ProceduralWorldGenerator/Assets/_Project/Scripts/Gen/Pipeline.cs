// the fixed order of the steps
// shape (Ridge, Voronoi, Perlin) -> Normalize -> erosion (Thermal, River) -> Smooth -> surface (Water, Textures, Shoreline)
// Normalize sits before erosion so erosion can use real units (degrees, meters)
public static class Pipeline
{
    public static IStep[] Steps() => new IStep[]
    {
        new ResetStep(),
        new RidgeStep(),          // resets by itself, must be the first shape step
        new VoronoiStep(),
        new MultiplePerlinStep(),
        new NormalizeStep(),
        new ThermalStep(),
        new RiverStep(),
        new SmoothStep(),
        new WaterStep(),
        new TextureStep(),
        new ShorelineStep()
    };
}
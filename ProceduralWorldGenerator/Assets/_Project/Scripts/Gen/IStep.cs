// one step of the pipeline
// IsEnabled reads the config so Gen can skip the step and still write it in the log
public interface IStep
{
    string Name { get; }
    bool IsEnabled(TerrainConfig config);
    void Apply(RunContext ctx);
}
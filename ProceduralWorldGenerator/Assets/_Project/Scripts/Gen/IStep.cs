public interface IStep
{
    string Name { get; }
    void Apply(RunContext ctx);
}
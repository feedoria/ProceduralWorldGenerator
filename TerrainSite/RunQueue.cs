using System.Threading.Channels;

namespace TerrainSite;

// the endpoint puts run ids in, the worker takes them out one by one
public class RunQueue
{
    readonly Channel<int> channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int runId) => channel.Writer.TryWrite(runId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
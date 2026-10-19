using System.Threading.Channels;

namespace TerrainSite;

// in-memory queue of run ids, the button puts ids in, the worker takes them out one by one
public class RunQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int runId) => _channel.Writer.TryWrite(runId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
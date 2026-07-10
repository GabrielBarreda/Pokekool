using System.Threading.Channels;
using Pokekool.Application.Storage;

namespace Pokekool.Infrastructure.Storage;

/// <summary>
/// File d'attente en mémoire des jobs de compilation Blender (Channel non borné).
/// Singleton : partagée entre les producteurs (handler d'upload) et le consommateur
/// (<see cref="BlenderCompileBackgroundService"/>). Non persistée : les jobs en file
/// sont perdus au redémarrage (les jobs restés en base en 'queued' peuvent être relancés).
/// </summary>
internal sealed class InMemoryCompileQueue : IStorageModelCompileQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public async Task EnqueueAsync(Guid jobId, CancellationToken ct)
        => await _channel.Writer.WriteAsync(jobId, ct);

    /// <summary>Flux consommé par le background service.</summary>
    public ChannelReader<Guid> Reader => _channel.Reader;
}

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Pokekool.Application.Abstractions;

namespace Pokekool.Infrastructure.Services;

/// <summary>
/// Singleton qui reçoit les événements de progression du sync
/// et les diffuse à tous les consommateurs SSE connectés.
/// </summary>
public sealed class SyncProgressService : ISyncProgressReporter
{
    private readonly List<Channel<SyncProgressEvent>> _listeners = [];
    private readonly object _lock = new();

    /// <summary>Émet un événement vers tous les listeners SSE actifs.</summary>
    public void Report(SyncProgressEvent evt)
    {
        lock (_lock)
        {
            foreach (var ch in _listeners)
                ch.Writer.TryWrite(evt);
        }
    }

    /// <summary>
    /// Retourne un flux async d'événements pour un consommateur SSE.
    /// Se désinscrit automatiquement à la déconnexion (cancellation).
    /// </summary>
    public IAsyncEnumerable<SyncProgressEvent> SubscribeAsync(CancellationToken ct)
    {
        var ch = Channel.CreateBounded<SyncProgressEvent>(
            new BoundedChannelOptions(200) { FullMode = BoundedChannelFullMode.DropOldest });

        lock (_lock) _listeners.Add(ch);

        return ReadAsync(ch, ct);
    }

    private async IAsyncEnumerable<SyncProgressEvent> ReadAsync(
        Channel<SyncProgressEvent> ch,
        [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            await foreach (var evt in ch.Reader.ReadAllAsync(ct))
                yield return evt;
        }
        finally
        {
            lock (_lock) _listeners.Remove(ch);
        }
    }
}

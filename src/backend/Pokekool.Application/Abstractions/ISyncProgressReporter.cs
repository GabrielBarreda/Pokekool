namespace Pokekool.Application.Abstractions;

public interface ISyncProgressReporter
{
    void Report(SyncProgressEvent evt);
}

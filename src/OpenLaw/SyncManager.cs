using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Clarius.OpenLaw;

public record struct SyncResult(string Id, ContentAction Action);

/// <summary>
/// Simple synchronization manager that syncs content from a source to a target repository in 
/// a single-threaded way.
/// </summary>
public class SyncManager(IContentRepository source, IContentRepository target)
{
    /// <summary>
    /// Synchronizes content from the source to the target repository.
    /// </summary>
    /// <param name="cancellation">Token to cancel the enumeration.</param>
    /// <returns>An asynchonous enumeration of the results of each content sync operation.</returns>
    public async IAsyncEnumerable<SyncResult> Sync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await foreach (var info in source.EnumerateAsync(cancellation))
        {
            var timestamp = await target.GetTimestampAsync(info.Id);
            if (timestamp != info.Timestamp)
            {
                using var content = await source.GetContentAsync(info.Id);
                Debug.Assert(content is not null);
                yield return new SyncResult(info.Id, await target.SetContentAsync(info.Id, info.Timestamp, content));
            }
            else
            {
                yield return new SyncResult(info.Id, ContentAction.Skipped);
            }
        }
    }
}
namespace Clarius.OpenLaw;

public interface IContentRepository
{
    IAsyncEnumerable<IContentInfo> EnumerateAsync(CancellationToken cancellation);
    ValueTask<Stream?> GetContentAsync(string id);
    ValueTask<long?> GetTimestampAsync(string id);
    ValueTask<ContentAction> SetContentAsync(string id, long timestamp, Stream content);
}
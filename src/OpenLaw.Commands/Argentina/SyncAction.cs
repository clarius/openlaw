using System.Diagnostics;
using System.Text;
using Polly;

namespace Clarius.OpenLaw.Argentina;

class SyncAction(SearchResult item, long? targetTimestamp, bool forceUpdate)
{
    Document? original;
    Document? document;

    public SearchResult Item => item;

    public long? TargetTimestamp => targetTimestamp;

    public bool ForceUpdate => forceUpdate;

    public int Attempts { get; private set; }

    public Exception? Exception { get; private set; }

    public async Task<SyncActionResult?> ExecuteAsync(SaijClient client, FileDocumentRepository targetRepository, bool noDebugger = false)
    {
        Exception = null;

        // Try loading the doc(s) only once.
        try
        {
            if (document == null)
            {
                document = await client.LoadAsync(item);
                // Might return null if it's a new doc.
                original = await targetRepository.GetDocumentAsync(item.Id);
            }
        }
        catch (Exception e) when (e is HttpRequestException || e is ExecutionRejectedException)
        {
            // Don't consider transient exceptions as actual attempts.
            return null;
        }
        catch (Exception e)
        {
            Attempts++;
            Exception = e;
#if DEBUG
            if (noDebugger != true)
                Debugger.Launch();
#endif
            return null;
        }

        if (forceUpdate || targetTimestamp is null || document.Timestamp > targetTimestamp)
        {
            using var markdown = new MemoryStream(Encoding.UTF8.GetBytes(document.ToMarkdown(true)));
            try
            {
                var result = new SyncActionResult(await targetRepository.SetDocumentAsync(document), document, original, targetRepository.GetLocation(document));

                // Diff new and old content, if we only have two changes and they are to timestamp and
                // fecha-umod, change action to Timestamp
                if (result.Action == ContentAction.Updated &&
                    JsonDiff.Diff(original!.Json, document!.Json) is { Length: 2 } diff &&
                    diff[0].Kind == JsonOperation.Replace && diff[0].Path == "/document/metadata/timestamp" &&
                    diff[1].Kind == JsonOperation.Replace && diff[1].Path == "/document/content/fecha-umod")
                {
                    return result with { Action = ContentAction.Timestamps };
                }

                return result;
            }
            catch (Exception e) when (e is HttpRequestException || e is ExecutionRejectedException)
            {
                // Don't consider transient exceptions as actual attempts.
                return null;
            }
            catch (Exception e)
            {
                Attempts++;
                Exception = e;
#if DEBUG
                if (noDebugger != true)
                    Debugger.Launch();
#endif
                return null;
            }
        }

        return new SyncActionResult(ContentAction.Skipped, document, original, targetRepository.GetLocation(document));
    }
}

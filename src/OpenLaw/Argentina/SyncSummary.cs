using System.Diagnostics;

namespace Clarius.OpenLaw.Argentina;

public class SyncSummary(string operation)
{
    readonly HashSet<string> errors = [];
    readonly Stopwatch stopwatch = Stopwatch.StartNew();
    int created = 0;
    int updated = 0;
    int skipped = 0;
    int failed = 0;

    public string Operation { get; } = operation;
    public int Created => created;
    public int Updated => updated;
    public int Skipped => skipped;
    public int Failed => failed;

    public TimeSpan Elapsed => stopwatch.Elapsed;

    public static SyncSummary Start(string operation) => new SyncSummary(operation);

    public void Add(ContentAction action)
    {
        switch (action)
        {
            case ContentAction.Created:
                Interlocked.Increment(ref created);
                break;
            case ContentAction.Updated:
                Interlocked.Increment(ref updated);
                break;
            case ContentAction.Skipped:
                Interlocked.Increment(ref skipped);
                break;
        }
    }

    public void Add(Exception? exception)
    {
        if (exception != null)
            errors.Add(exception.Message);

        Interlocked.Increment(ref failed);
    }

    public void Stop() => stopwatch.Stop();

    public string ToMarkdown()
    {
        var report = $"{Operation}: ";

        if (Created > 0) report += $":heavy_plus_sign: {Created} ";
        if (Updated > 0) report += $":pencil2: {Updated} ";
        if (Skipped > 0) report += $":white_check_mark: {Skipped} ";
        if (Failed > 0) report += $":x: {Failed} ";

        return report + $":hourglass: {Elapsed.ToMinimalString()}";
    }
}

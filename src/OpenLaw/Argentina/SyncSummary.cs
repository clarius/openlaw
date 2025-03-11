using System.Diagnostics;

namespace Clarius.OpenLaw.Argentina;

public class SyncSummary(string operation)
{
    readonly HashSet<string> errors = [];
    readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public string Operation { get; } = operation;
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Skipped { get; private set; }
    public int Failed { get; private set; }

    public TimeSpan Elapsed => stopwatch.Elapsed;

    public static SyncSummary Start(string operation) => new SyncSummary(operation);

    public void Add(ContentAction action)
    {
        switch (action)
        {
            case ContentAction.Created:
                Created++;
                break;
            case ContentAction.Updated:
                Updated++;
                break;
            case ContentAction.Skipped:
                Skipped++;
                break;
        }
    }

    public void Add(Exception? exception)
    {
        if (exception != null)
            errors.Add(exception.Message);

        Failed++;
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

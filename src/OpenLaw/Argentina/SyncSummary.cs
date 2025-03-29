using System.Diagnostics;
using System.Text;
using DiffPlex.DiffBuilder;

namespace Clarius.OpenLaw.Argentina;

public class SyncSummary(string operation)
{
    readonly List<SyncActionResult> results = [];
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

    public static SyncSummary Start(string operation) => new(operation);

    public void Add(SyncActionResult result)
    {
        switch (result.Action)
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

        results.Add(result);
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
        if (Updated > 0) report += $":pencil: {Updated} ";
        if (Skipped > 0) report += $":white_check_mark: {Skipped} ";
        if (Failed > 0) report += $":x: {Failed} ";

        report += $":hourglass: {Elapsed.ToMinimalString()}";

        var details = new StringBuilder();
        var nop = 0;

        // TODO: add/count diff.
        foreach (var result in results)
        {
            // quickly skip from report the dummy updates
            if (result.Action == ContentAction.Skipped)
                continue;
            if (result.Action == ContentAction.Updated &&
                result.OldDocument != null)
            {
                var diff = InlineDiffBuilder.Diff(result.OldDocument!.Json, result.NewDocument.Json)
                    .Lines.Where(x => x.Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged)
                    .ToList();

                if (diff.Count == 4 &&
                    diff[0].Type == DiffPlex.DiffBuilder.Model.ChangeType.Deleted && diff[0].Text.Trim().StartsWith("\"timestamp\":") &&
                    diff[1].Type == DiffPlex.DiffBuilder.Model.ChangeType.Inserted && diff[1].Text.Trim().StartsWith("\"timestamp\":") &&
                    diff[2].Type == DiffPlex.DiffBuilder.Model.ChangeType.Deleted && diff[2].Text.Trim().StartsWith("\"fecha-umod\":") &&
                    diff[3].Type == DiffPlex.DiffBuilder.Model.ChangeType.Inserted && diff[3].Text.Trim().StartsWith("\"fecha-umod\":"))
                {
                    // discard changes that are only timestamp/fecha-umod
                    // see: https://github.com/clarius/normas/pull/32/files#diff-2f592ca38476012d2be4d6b3f17789b83b8bd3c3fa1df6eda2e54b8ccc7e1cbc
                    nop++;
                    continue;
                }
            }

            details.AppendLine($"|{(result.Action == ContentAction.Created ? ":heavy_plus_sign:" : ":pencil:")}|{result.NewDocument.Name}|{result.NewDocument.Title}|");
        }

        if (details.Length > 0)
        {
            report +=
                """


                <details>

                <summary>:information_source: Detalles</summary>

                | | Nombre | Título|
                |--------|---------------|-------------|

                """;
            report += details.ToString();
            report += Environment.NewLine;
            if (nop > 0)
            {
                report +=
                    $"""

                    > Actualizaciones sin cambios de contenido: {nop}
                    """;
            }

            report +=
                """

                </details>
                """;
        }

        return report;
    }
}

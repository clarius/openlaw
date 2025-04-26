using System.Diagnostics;
using System.Text;

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
            case ContentAction.Timestamps:
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

    public void Save(string filePath, bool append = false)
    {
        var content = ToMarkdown();
        AddOrUpdate(filePath, append, content);

        // Persist the timestamp-only changes to a separate txt file so it can be 
        // committed separately without a PR, for example, since there's no need 
        // for a review.
        filePath = Path.ChangeExtension(filePath, ".txt");

        AddOrUpdate(filePath, append, results
            .Where(r => r.Action == ContentAction.Timestamps)
            .Select(r => r.Location.Data)
            .Concat(results
            .Where(r => r.Action == ContentAction.Timestamps)
            .Select(r => r.Location.Text)));
    }

    static void AddOrUpdate(string path, bool append, string content) => AddOrUpdate(path, append, [content]);

    static void AddOrUpdate(string path, bool append, IEnumerable<string> lines)
    {
        if (File.Exists(path) && append)
        {
            File.AppendAllLines(path, [Environment.NewLine]);
            File.AppendAllLines(path, lines);
        }
        else
        {
            if (Path.GetDirectoryName(path) is { } dir)
                Directory.CreateDirectory(dir);
            File.WriteAllLines(path, lines);
        }
    }

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

            if (result.Action == ContentAction.Timestamps)
            {
                // discard changes that are only timestamp/fecha-umod
                // see: https://github.com/clarius/normas/pull/32/files#diff-2f592ca38476012d2be4d6b3f17789b83b8bd3c3fa1df6eda2e54b8ccc7e1cbc
                nop++;
                continue;
            }

            details.AppendLine($"|{(result.Action == ContentAction.Created ? ":heavy_plus_sign:" : ":pencil:")}|[{result.NewDocument.Name ?? result.NewDocument.Alias}]({result.NewDocument.WebUrl})|{result.NewDocument.Title}|");
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

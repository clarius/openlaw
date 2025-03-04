using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

public class SyncCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<SyncCommand.SyncSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SyncSettings settings)
    {
        var watch = Stopwatch.StartNew();
        var targetDir = Path.GetFullPath(settings.Directory);
        var query = $"{settings.Tipo} ({settings.Jurisdiccion}{(settings.Provincia == null ? "" : ", " + settings.Provincia)})";
        var target = new FileContentRepository(targetDir);

        await console.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Sincronizando  {query} -> {settings.Directory}");
                var createdTask = ctx.AddTask(":check_mark_button: Created: 0");
                var updatedTask = ctx.AddTask(":pencil: Updated: 0");
                var skippedTask = ctx.AddTask(":right_arrow: Skipped: 0");
                createdTask.IsIndeterminate = updatedTask.IsIndeterminate = skippedTask.IsIndeterminate = true;

                var created = 0;
                var updated = 0;
                var skipped = 0;

                var lastProgress = new ProgressMessage("", 0);
                IProgress<ProgressMessage> progress = new Progress<ProgressMessage>(x =>
                {
                    lastProgress = x;
                    task.Description = x.Message;
                    task.Value(x.Percentage);
                });

                var client = new SaijClient(http, progress);
                var options = new ParallelOptions();
                if (Debugger.IsAttached)
                    options.MaxDegreeOfParallelism = 1;

                await Parallel.ForEachAsync(client.SearchAsync(settings.Tipo, settings.Jurisdiccion, settings.Provincia), options, async (doc, cancellation) =>
                {
                    var action = ContentAction.Skipped;
                    var timestamp = await target.GetTimestampAsync(doc.Id);
                    if (timestamp == null || doc.Timestamp == null || doc.Timestamp != timestamp)
                    {
                        var content = await client.LoadAsync(doc);
                        Debug.Assert(content is not null);
                        // Compare once more since we may have gotten a content timestamp we previously 
                        // didn't have (some search docs don't have timestamps)
                        if (content.Timestamp != timestamp)
                        {
                            using var markdown = new MemoryStream(Encoding.UTF8.GetBytes(doc.ToMarkdown(true)));
                            action = await target.SetContentAsync(doc.Id, content.Timestamp, markdown);
                        }
                    }

                    switch (action)
                    {
                        case ContentAction.Created:
                            //createdTask.Increment(1);
                            createdTask.Description = $":check_mark_button: Created: {created++}";
                            break;
                        case ContentAction.Updated:
                            //updatedTask.Increment(1);
                            updatedTask.Description = $":pencil: Updated: {updated++}";
                            break;
                        case ContentAction.Skipped:
                            //skippedTask.Increment(1);
                            skippedTask.Description = $":right_arrow: Skipped: {skipped++}";
                            break;
                    }
                });
            });

        console.MarkupLine($"[bold green]Enumeración completada en {watch.Elapsed.Humanize()}[/]");

        return 0;
    }

    public class SyncSettings : ClientSettings
    {
        [Description("Ubicación opcional archivos. Por defecto el directorio actual.")]
        [CommandOption("--dir")]
        public string Directory { get; set; } = ".";
    }
}

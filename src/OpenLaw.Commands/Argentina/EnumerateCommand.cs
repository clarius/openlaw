using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Enumerar todos los documentos.")]
public class EnumerateCommand(IAnsiConsole console, IHttpClientFactory http, CancellationTokenSource cts) : AsyncCommand<EnumerateCommand.EnumerateSettings>
{
    // For batched retrieval from search results.
    const int PageSize = 100;

    public override async Task<int> ExecuteAsync(CommandContext context, EnumerateSettings settings, CancellationToken cancellation = default)
    {
        var watch = Stopwatch.StartNew();

        console.MarkupLine($"[dim]Enumerating {settings.Tipo} {settings.Jurisdiccion} {settings.Provincia}[/]");

        var results = new ConcurrentBag<SearchResult>();

        await console.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new PrefixProgressColumn(new RemainingTimeColumn(), Emoji.Known.HourglassNotDone),
                new PrefixProgressColumn(new ElapsedTimeColumn(), Emoji.Known.HourglassDone),
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Enumerando...");
                var client = new SaijClient(http, new Progress<ProgressMessage>(x =>
                {
                    if (x.Total != task.MaxValue)
                        task.MaxValue = x.Total;
                }));

                var options = new ParallelOptions
                {
                    CancellationToken = cts.Token,
                    MaxDegreeOfParallelism = Debugger.IsAttached ? 1 : Environment.ProcessorCount
                };

                // Fetch batches in parallel
                await Parallel.ForEachAsync(
                    Enumerable.Range(0, options.MaxDegreeOfParallelism),
                    options,
                    async (index, cancellationToken) =>
                    {
                        // Starting point for this task
                        var skip = index * PageSize;
                        while (true)
                        {
                            // Fetch a batch
                            var search = client.SearchAsync(settings.Tipo, settings.Jurisdiccion, settings.Provincia, settings.Filters, skip, PageSize, cancellationToken);
                            var count = 0;
                            await foreach (var item in search)
                            {
                                if (item.ContentType != settings.ContentType)
                                    continue;

                                count++;
                                results.Add(item);
                                task.Description = $"Enumerated {results.Count} of {task.MaxValue}";
                                task.Value = results.Count;
                                if (count == PageSize)
                                    break;
                            }

                            if (count == 0) // No more items, stop this task
                                break;

                            // Move to the next batch
                            skip += options.MaxDegreeOfParallelism * PageSize;
                        }
                    });
            });

        console.MarkupLine($"[bold green]Enumeracion de {results.Count} items completada en {watch.Elapsed.Humanize()}[/]");

        if (settings.Save.IsSet)
        {
            var file = settings.Save.Value ?? $"{settings.Tipo}.csv";
            await console.Status().StartAsync($"Guardando {file}...", async ctx =>
            {
                var content = new StringBuilder();

                // Only add the header if file doesn't exist already
                if (!File.Exists(file))
                    content.AppendLine($"date,id");
                else
                    content.AppendLine();

                foreach (var item in results)
                    content.AppendLine($"{item.Date:yyyyMMdd},{item.Id}");

                if (File.Exists(file))
                    await File.AppendAllTextAsync(file, content.ToString());
                else
                    await File.WriteAllTextAsync(file, content.ToString());
            });
        }

        return 0;
    }

    public class EnumerateSettings : ClientSettings
    {
        [Description("Mostrar resultados con links.")]
        [CommandOption("--show-links", IsHidden = true)]
        public bool ShowLinks { get; set; }

        [Description("Guardar los resultados en el archivo CSV especificado.")]
        [CommandOption("--save [PATH]")]
        public FlagValue<string> Save { get; set; } = new();
    }
}

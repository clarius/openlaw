using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Enumerar todos los documentos.")]
public class EnumerateCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<EnumerateCommand.EnumerateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, EnumerateSettings settings)
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
                new RemainingTimeColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Enumerando...");
                var client = new SaijClient(http, new Progress<ProgressMessage>(x =>
                {
                    if (x.Total != task.MaxValue)
                        task.MaxValue = x.Total;
                }));

                var options = new ParallelOptions();
                var take = 100;
                if (Debugger.IsAttached)
                    options.MaxDegreeOfParallelism = 1;
                else
                    options.MaxDegreeOfParallelism = Environment.ProcessorCount;

                // Fetch batches in parallel
                await Parallel.ForEachAsync(
                    Enumerable.Range(0, options.MaxDegreeOfParallelism),
                    options,
                    async (index, cancellationToken) =>
                    {
                        // Starting point for this task
                        var skip = index * take; 
                        while (true)
                        {
                            // Fetch a batch
                            var search = client.SearchAsync(settings.Tipo, settings.Jurisdiccion, settings.Provincia, skip, take, cancellationToken);
                            var count = 0;
                            await foreach (var item in search)
                            {
                                count++;
                                results.Add(item);
                                task.Description = $"Enumerated {results.Count} of {task.MaxValue}";
                                task.Value = results.Count;
                                if (count == take)
                                    break;
                            }

                            if (count == 0) // No more items, stop this task
                                break;

                            // Move to the next batch
                            skip += options.MaxDegreeOfParallelism * take;
                        }
                    });
            });

        console.MarkupLine($"[bold green]Enumeracion de {results.Count} items completada en {watch.Elapsed.Humanize()}[/]");
        return 0;
    }

    public class EnumerateSettings : ClientSettings
    {
        [Description("Mostrar resultados con links.")]
        [CommandOption("--show-links", IsHidden = true)]
        public bool ShowLinks { get; set; }
    }
}

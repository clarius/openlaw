using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Polly;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

public class SyncCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<SyncCommand.SyncSettings>
{
    // For batched retrieval from search results.
    const int PageSize = 100;

    public override async Task<int> ExecuteAsync(CommandContext context, SyncSettings settings)
    {
        var watch = Stopwatch.StartNew();
        var targetDir = Path.GetFullPath(settings.Directory);
        var query = $"{settings.Tipo} ({settings.Jurisdiccion}{(settings.Provincia == null ? "" : ", " + settings.Provincia)})";
        var target = new FileContentRepository(targetDir);

        long? total = null;
        var client = new SaijClient(http, new Progress<ProgressMessage>(x => total = x.Total));
        var poison = new List<SyncAction>();
        var summary = new SyncSummary(query);

        await console.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new ElapsedTimeColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var results = new ConcurrentQueue<SyncAction>();
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Debugger.IsAttached ? 1 : Environment.ProcessorCount
                };

                var loadTask = ctx.AddTask($"Cargando [lime]{query}[/]");

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
                            var search = client.SearchAsync(settings.Tipo, settings.Jurisdiccion, settings.Provincia, skip, PageSize, cancellationToken);
                            var count = 0;
                            await foreach (var item in search)
                            {
                                count++;
                                if (settings.Top != null && results.Count >= settings.Top)
                                    return;

                                results.Enqueue(new SyncAction(client, item, target, await target.GetTimestampAsync(item.Id)));

                                if (settings.Top != null && results.Count >= settings.Top)
                                    return;

                                loadTask.Description = $"Cargando [lime]{query}[/]: {results.Count} de {total}";
                                loadTask.Value = results.Count;
                                if (total.HasValue)
                                    loadTask.MaxValue(total.Value);

                                if (count == PageSize)
                                    break;
                            }

                            if (count == 0) // No more items, stop this task
                                break;

                            // Move to the next batch
                            skip += options.MaxDegreeOfParallelism * PageSize;
                        }
                    });

                loadTask.StopTask();

                total = results.Count;
                var processed = 0;
                var syncTask = ctx.AddTask($"Sincronizando [lime]{query}[/]: {processed} de {total} ({summary.Created}:plus:, {summary.Updated}:writing_hand:, {summary.Skipped}:check_mark_button:)", maxValue: total.Value);

                void UpdateSync(ContentAction action)
                {
                    Interlocked.Increment(ref processed);
                    syncTask.Value = processed;
                    summary.Add(action);
                    syncTask.Description = $"Sincronizando [lime]{query}[/]: {processed} de {total} ({summary.Created}:plus:, {summary.Updated}:writing_hand:, {summary.Skipped}:check_mark_button:)";
                }

                async IAsyncEnumerable<SyncAction> GetResults()
                {
                    while (processed != total)
                    {
                        if (results.TryDequeue(out var result))
                            yield return result;
                        else
                            await Task.Delay(100);
                    }
                }

                await Parallel.ForEachAsync(GetResults(), options, async (item, cancellation) =>
                {
                    // If item attempts > 2, add an await Task.Delay that is exponential to the attempts. 
                    if (item.Attempts >= 2)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, item.Attempts));
                        await Task.Delay(delay, cancellation);
                    }

                    var action = await item.ExecuteAsync();
                    if (action != null)
                    {
                        UpdateSync(action.Value);
                    }
                    else if (item.Attempts < 5)
                    {
                        // Re-enqueue for execution later on again. Note we won't 
                        // update the stats.
                        results.Enqueue(item);
                    }
                    else
                    {
                        poison.Add(item);
                        summary.Add(item.Exception);
                        Interlocked.Increment(ref processed);
                    }
                });

                syncTask.StopTask();
            });

        console.MarkupLine($"[bold green]Sincronización completada en {watch.Elapsed.ToMinimalString()}[/]");
        summary.Stop();

        if (poison.Count > 0)
        {
            var errorsDir = Directory.CreateDirectory(".openlaw/errors").FullName;
            console.MarkupLine($":cross_mark: [red]{poison.Count}[/] fallas de sincronización en [link={errorsDir}].openlaw/errors[/].");
            foreach (var error in poison)
            {
                await File.WriteAllTextAsync(Path.Combine(errorsDir, $"{error.Item.Id}.yml"),
                    new { error.Attempts, Exception = error.Exception?.ToString(), error.Item }.ToYaml());
            }
        }

        if (settings.ChangeLog is not null)
        {
            var changelog = summary.ToMarkdown();
            if (File.Exists(settings.ChangeLog) && settings.AppendLog)
            {
                await File.AppendAllLinesAsync(settings.ChangeLog, [Environment.NewLine]);
                await File.AppendAllTextAsync(settings.ChangeLog, changelog);
            }
            else
            {
                if (Path.GetDirectoryName(settings.ChangeLog) is { } dir)
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(settings.ChangeLog, changelog);
            }
        }

        return 0;
    }

    public class SyncSettings : ClientSettings
    {
        [Description("Ubicación opcional archivos. Por defecto el directorio actual.")]
        [CommandOption("--dir")]
        public string Directory { get; set; } = ".";

        [Description("Escribir un resumen de las operaciones efectuadas en el archivo especificado.")]
        [CommandOption("--changelog")]
        public string? ChangeLog { get; set; }

        [Description("Agregar al log de cambios si ya existe.")]
        [CommandOption("--appendlog")]
        public bool AppendLog { get; set; }

        [DefaultValue(null)]
        [CommandOption("--top", IsHidden = true)]
        public int? Top { get; set; } = null;
    }

    class SyncAction(SaijClient client, SearchResult item, IContentRepository targetRepository, long? targetTimestamp)
    {
        Document? document;

        public SearchResult Item => item;

        public int Attempts { get; private set; }

        public Exception? Exception { get; private set; }

        public async Task<ContentAction?> ExecuteAsync()
        {
            Exception = null;

            // Try loading the doc only once.
            try
            {
                document ??= await client.LoadAsync(item);
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
                Debugger.Launch();
#endif
                return null;
            }

            if (targetTimestamp is null || document.Timestamp > targetTimestamp)
            {
                using var markdown = new MemoryStream(Encoding.UTF8.GetBytes(document.ToMarkdown(true)));
                try
                {
                    return await targetRepository.SetContentAsync(item.Id, document.Timestamp, markdown);
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
                    Debugger.Launch();
#endif
                    return null;
                }
            }

            return ContentAction.Skipped;
        }
    }
}

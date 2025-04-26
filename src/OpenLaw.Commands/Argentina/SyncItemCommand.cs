using System.ComponentModel;
using System.Runtime.ExceptionServices;
using Polly;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Sincroniza un documento especifico de SAIJ")]
public class SyncItemCommand(IAnsiConsole console, IHttpClientFactory http, CancellationTokenSource cts) : AsyncCommand<SyncItemCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var targetDir = Path.GetFullPath(settings.Directory);
        var target = new FileDocumentRepository(targetDir);
        var client = new SaijClient(http, new Progress<ProgressMessage>(x => console.WriteLine(x.Message)));

        return await console.Status().StartAsync($"Sync {settings.Id} > {settings.Directory}", async _ =>
        {
            try
            {
                var summary = new SyncSummary($"Sync {settings.Id}");
                var result = await client.SearchIdAsync(settings.Id, cts.Token);
                // Fallback to loading by document id, but as a search result so we can levarge the rest.
                if (result is null)
                {
                    var doc = await Policy
                        // ArgumentException is what loading a doc throws if no data is found, which in retries, is a transient error.
                        .Handle<ArgumentException>()
                        .WaitAndRetryAsync(
                            retryCount: 5,
                            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                            onRetry: (exception, timeSpan, retryCount, context) =>
                            {
                                console.MarkupLine($"[yellow]Intento {retryCount}. Reintentando en {timeSpan.TotalSeconds}s...[/]");
                            })
                        .ExecuteAsync(async () => await client.LoadAsync(settings.Id));

                    result = new SearchResult(settings.Id, doc.ContentType, doc.DocumentType, doc.Status, doc.Date, doc.Timestamp);
                }

                var item = new SyncAction(result, null, true);
                SyncActionResult? action = null;

                while (action == null && item.Attempts < 5)
                {
                    if (item.Attempts >= 2)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, item.Attempts));
                        await Task.Delay(delay, cts.Token);
                    }

                    action = await item.ExecuteAsync(client, target, settings.NoDebugger);
                }

                if (item.Exception is not null)
                    ExceptionDispatchInfo.Capture(item.Exception).Throw();

                if (action is null)
                    throw new InvalidOperationException($"Unable to sync document {item.Item.Id} after {item.Attempts} attempts.");

                var ghemoji = action.Action switch
                {
                    ContentAction.Created => ":heavy_plus_sign:",
                    ContentAction.Updated => ":pencil:",
                    _ => ":white_check_mark:",
                };
                var cliemoji = action.Action switch
                {
                    ContentAction.Created => ":plus:",
                    ContentAction.Updated => ":pencil:",
                    _ => ":check_mark_button:",
                };

                console.MarkupLine($"{cliemoji}  {action.NewDocument.WebUrl}");

                if (settings.ChangeLog is not null)
                {
                    summary.Add(action);
                    summary.Stop();
                    summary.Save(settings.ChangeLog, settings.AppendLog);
                }

                return 0;
            }
            catch (Exception ex)
            {
                console.MarkupLine($":cross_mark: {ex.Message}");
                return 1;
            }
        });
    }

    public class Settings : ClientSettings
    {
        [Description("Ubicación opcional archivos. Por defecto el directorio actual")]
        [CommandOption("--dir")]
        public string Directory { get; set; } = ".";

        [Description("ID del item a sincronizar")]
        [CommandArgument(0, "<ID>")]
        public required string Id { get; set; }

        [Description("Escribir un resumen de las operaciones efectuadas en el archivo especificado")]
        [CommandOption("--changelog")]
        public string? ChangeLog { get; set; }

        [Description("Agregar al log de cambios si ya existe")]
        [CommandOption("--appendlog")]
        public bool AppendLog { get; set; }

        [Description("Don't attemp to attach debugger in debug builds.")]
        [CommandOption("--no-debugger", IsHidden = true)]
        public bool NoDebugger { get; set; }
    }
}

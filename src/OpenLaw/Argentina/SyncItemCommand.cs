﻿using System.ComponentModel;
using System.Text;
using Polly;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Sincroniza un documento especifico de SAIJ")]
public class SyncItemCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<SyncItemCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var targetDir = Path.GetFullPath(settings.Directory);
        var target = new FileDocumentRepository(targetDir);
        var client = new SaijClient(http, new Progress<ProgressMessage>(x => console.WriteLine(x.Message)));

        // ArgumentException is what loading a doc throws if no data is found, which in retries, is a transient error.
        var retryPolicy = Policy.Handle<ArgumentException>().WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                console.MarkupLine($"[yellow]Intento {retryCount}. Reintentando en {timeSpan.TotalSeconds}s...[/]");
            });

        return await console.Status().StartAsync($"Sync {settings.Item} > {settings.Directory}", async _ =>
        {
            try
            {
                var document = await retryPolicy.ExecuteAsync(() => client.LoadAsync(settings.Item));

                using var markdown = new MemoryStream(Encoding.UTF8.GetBytes(document.ToMarkdown(true)));
                var action = await target.SetDocumentAsync(document);
                var ghemoji = action switch
                {
                    ContentAction.Created => ":heavy_plus_sign:",
                    ContentAction.Updated => ":pencil:",
                    _ => ":white_check_mark:",
                };
                var cliemoji = action switch
                {
                    ContentAction.Created => ":plus:",
                    ContentAction.Updated => ":pencil:",
                    _ => ":check_mark_button:",
                };

                console.MarkupLine($"{cliemoji}  {document.WebUrl}");

                if (settings.ChangeLog is not null)
                {
                    // TODO: reuse with SyncCommand?
                    if (File.Exists(settings.ChangeLog) && settings.AppendLog)
                    {
                        await File.AppendAllLinesAsync(settings.ChangeLog, [Environment.NewLine]);
                        await File.AppendAllTextAsync(settings.ChangeLog, $"{ghemoji}  [{document.Alias}]({document.WebUrl})");
                    }
                    else
                    {
                        if (Path.GetDirectoryName(settings.ChangeLog) is { } dir)
                            Directory.CreateDirectory(dir);
                        await File.WriteAllTextAsync(settings.ChangeLog, $"{ghemoji}  [{document.Alias}]({document.WebUrl})");
                    }
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
        public required string Item { get; set; }

        [Description("Escribir un resumen de las operaciones efectuadas en el archivo especificado")]
        [CommandOption("--changelog")]
        public string? ChangeLog { get; set; }

        [Description("Agregar al log de cambios si ya existe")]
        [CommandOption("--appendlog")]
        public bool AppendLog { get; set; }
    }
}

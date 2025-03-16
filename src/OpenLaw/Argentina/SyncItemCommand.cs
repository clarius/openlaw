using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Sincroniza un documento especifico de SAIJ")]
public class SyncItemCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<SyncItemCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var watch = Stopwatch.StartNew();
        var targetDir = Path.GetFullPath(settings.Directory);
        var target = new FileDocumentRepository(targetDir);
        var client = new SaijClient(http, new Progress<ProgressMessage>(x => console.WriteLine(x.Message)));

        try
        {
            var document = await client.LoadAsync(settings.Item);
            using var markdown = new MemoryStream(Encoding.UTF8.GetBytes(document.ToMarkdown(true)));
            var action = await target.SetDocumentAsync(document);
            var emoji = action switch
            {
                ContentAction.Created => ":heavy_plus_sign:",
                ContentAction.Updated => ":pencil:",
                _ => ":white_check_mark:",
            };
            var summary = $"{emoji}  [link={document.WebUrl}]{document.Alias}[/]";
            console.MarkupLine(summary);

            if (settings.ChangeLog is not null)
            {
                // TODO: reuse with SyncCommand?
                if (File.Exists(settings.ChangeLog) && settings.AppendLog)
                {
                    await File.AppendAllLinesAsync(settings.ChangeLog, [Environment.NewLine]);
                    await File.AppendAllTextAsync(settings.ChangeLog, summary);
                }
                else
                {
                    if (Path.GetDirectoryName(settings.ChangeLog) is { } dir)
                        Directory.CreateDirectory(dir);
                    await File.WriteAllTextAsync(settings.ChangeLog, summary);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            console.MarkupLine($":cross_mark: {ex.Message}");
            return 1;
        }
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

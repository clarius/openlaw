using System.ComponentModel;
using System.Diagnostics;
using Devlooped;
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

        await console.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Enumerando...");
                var client = new SaijClient(http, new Progress<ProgressMessage>(x =>
                {
                    task.Description = x.Message;
                    task.Value(x.Percentage);
                }));

                var options = new ParallelOptions();
                if (Debugger.IsAttached)
                    options.MaxDegreeOfParallelism = 1;

                await Parallel.ForEachAsync(client.SearchAsync(settings.Tipo, settings.Jurisdiccion, settings.Provincia), options, async (doc, cancellation) =>
                {
                    if (settings.ShowLinks)
                    {
                        try
                        {
                            var full = await client.LoadAsync(doc);
                            var json = await JQ.ExecuteAsync(full.Json, ".document.content.d_link // empty");
                            if (string.IsNullOrEmpty(json))
                                return;

                            AnsiConsole.MarkupInterpolated($":link: [blue][link={doc.DataUrl()}]{doc.Id}[/][/] \r\n[dim]{json}[/]");
                            await File.AppendAllTextAsync("links.txt", $"[InlineData(\"{doc.Id}\")]\r\n", cancellation);
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.WriteException(e);
                        }
                    }
                });
            });

        console.MarkupLine($"[bold green]Sincronización completada en {watch.Elapsed.Humanize()}[/]");
        return 0;
    }

    public class EnumerateSettings : ClientSettings
    {
        [Description("Mostrar resultados con links.")]
        [CommandOption("--show-links", IsHidden = true)]
        public bool ShowLinks { get; set; }
    }
}

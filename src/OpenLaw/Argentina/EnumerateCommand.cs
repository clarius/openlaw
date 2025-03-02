using System.ComponentModel;
using System.Diagnostics;
using Devlooped;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace Clarius.OpenLaw.Argentina;

[Description("Enumerar todos los documentos.")]
public class EnumerateCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<EnumerateCommand.EnumerateSettings>
{
    public class EnumerateSettings : CommandSettings
    {
        [Description("Tipo de norma a enumerar.")]
        [CommandOption("-t|--tipo <Ley|Decreto|Resolution|Disposicion|Acordada>")]
        [DefaultValue(TipoNorma.Ley)]
        public TipoNorma? Tipo { get; set; } = TipoNorma.Ley;

        [Description("Jurisdicción a enumerar.")]
        [CommandOption("-j|--jurisdiccion <Internacional|Nacional|Provincial>")]
        [DefaultValue(Argentina.Jurisdiccion.Nacional)]
        public Jurisdiccion? Jurisdiccion { get; set; } = Argentina.Jurisdiccion.Nacional;

        [Description("Provincia a enumerar.")]
        [CommandOption("-p|--provincia")]
        [DefaultValue(null)]
        public Provincia? Provincia { get; set; }

        [Description("Enumerar todo, sin filtros.")]
        [CommandOption("--all", IsHidden = true)]
        public bool All { get; set; }

        [Description("Mostrar resultados con links.")]
        [CommandOption("--show-links", IsHidden = true)]
        public bool ShowLinks { get; set; }

        public override ValidationResult Validate()
        {
            if (All)
            {
                Tipo = null;
                Jurisdiccion = null;
                Provincia = null;
            }

            if (Jurisdiccion == Argentina.Jurisdiccion.Provincial && Provincia == null)
                return ValidationResult.Error("Debe especificar una provincia para la jurisdicción provincial.");

            if (Jurisdiccion != Argentina.Jurisdiccion.Provincial && Provincia != null)
                return ValidationResult.Error("No se puede especificar una provincia para la jurisdicción no provincial.");

            return base.Validate();
        }
    }

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
                            var full = await client.FetchAsync(doc.Id);
                            var json = await JQ.ExecuteAsync(full.Json, ".document.content.d_link // empty");
                            if (string.IsNullOrEmpty(json))
                                return;

                            AnsiConsole.MarkupInterpolated($":link: [blue][link={doc.DataUrl}]{doc.Id}[/][/] \r\n[dim]{json}[/]");
                            await File.AppendAllTextAsync("links.txt", $"[InlineData(\"{doc.Id}\")]\r\n", cancellation);
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.WriteException(e);
                        }
                    }
                    // Do nothing, just enumerate
                });
            });

        console.MarkupLine($"[bold green]Enumeración completada en {watch.Elapsed}[/]");
        return 0;
    }
}

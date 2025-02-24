using System.ComponentModel;
using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Enumerar todos los documentos.")]
public class EnumerateCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<EnumerateCommand.EnumerateSettings>
{
    public class EnumerateSettings : CommandSettings
    {
        [Description("Enumerar todos los documentos, no solamente Leyes de alcance Nacional.")]
        [DefaultValue(true)]
        [CommandOption("--all")]
        public bool All { get; set; } = false;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, EnumerateSettings settings)
    {
        var watch = Stopwatch.StartNew();

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

                var tipo = settings.All ? null : "Ley";
                var jurisdiccion = settings.All ? null : "Nacional";
                var options = new ParallelOptions();
                if (Debugger.IsAttached)
                    options.MaxDegreeOfParallelism = 1;

                await Parallel.ForEachAsync(client.EnumerateAsync(), options, (doc, cancellation) =>
                {
                    Debugger.Log(0, "", doc.Modified.ToString());
                    return ValueTask.CompletedTask;
                    // Do nothing, just enumerate
                });
            });

        console.MarkupLine($"[bold green]Enumeración completada en {watch.Elapsed}[/]");
        return 0;
    }
}

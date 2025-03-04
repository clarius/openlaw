using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Devlooped;
using NuGet.Packaging.Signing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

[Description("Descargar normas argentinas del sistema SAIJ.")]
public class DownloadCommand(IAnsiConsole console, IHttpClientFactory http) : AsyncCommand<DownloadSettings>
{
    static readonly JsonSerializerOptions readOptions = new()
    {
        Converters = { new JsonDictionaryConverter() },
    };

    static readonly JsonSerializerOptions writeOptions = new()
    {
        WriteIndented = true,
    };

    public override async Task<int> ExecuteAsync(CommandContext context, DownloadSettings settings)
    {
        Directory.CreateDirectory(settings.Directory);

        await console.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Descargando...");
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

                await Parallel.ForEachAsync(client.SearchAsync(), options, async (doc, cancellation) =>
                {
                    var file = Path.Combine(settings.Directory, doc.Id + ".json");
                    // Skip if file exists and has the same timestamp
                    if (File.Exists(file) && await GetJsonTimestampAsync(file) is var timestamp &&
                        timestamp == doc.Timestamp)
                    {
                        // Source json file hasn't changed, so only convert if requested
                        if (settings.Convert)
                            // Don't force conversion if file already exists.
                            Convert(file, overwrite: false);

                        return;
                    }

                    // Converting to dictionary performs string multiline formatting and markup removal
                    var full = await client.LoadAsync(doc);
                    File.WriteAllText(file, full.Json);
                    if (settings.Convert)
                        Convert(file, overwrite: true);
                });
            });

        return 0;
    }

    static void Convert(string file, bool overwrite)
        => DictionaryConverter.Convert(file, true, true, true, overwrite);

    static async Task<long> GetJsonTimestampAsync(string file)
    {
        var jq = await Cli.Wrap(JQ.Path)
            .WithArguments([".document.metadata.timestamp", file, "-r"])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8);

        var value = jq.StandardOutput.Trim();
        return long.TryParse(value, out var timestamp) ? timestamp : 0;
    }
}
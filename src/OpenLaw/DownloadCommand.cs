using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Devlooped;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw;

[Description("Descargar documentos del sistema SAIJ.")]
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

                await Parallel.ForEachAsync(client.EnumerateAsync(), options, async (doc, cancellation) =>
                {
                    if (doc["document"]?["metadata"]?["uuid"]?.GetValue<string>() is not string id ||
                        doc["document"]?["metadata"]?["timestamp"]?.GetValue<long>() is not long timestamp)
                        return;

                    var file = Path.Combine(settings.Directory, id + ".json");
                    // Skip if file exists and has the same timestamp
                    if (File.Exists(file) && await GetJsonTimestampAsync(file) == timestamp)
                    {
                        // Ensure we set the last write time to the timestamp for easier checks at conversion time.
                        // when converting files.
                        File.SetLastWriteTimeUtc(file, DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime);

                        // Source json file hasn't changed, so only convert if requested
                        if (settings.Convert)
                            // Don't force conversion if file already exists.
                            Convert(file, overwrite: false);

                        return;
                    }

                    // Converting to dictionary performs string multiline formatting and markup removal
                    var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(doc, readOptions);
                    File.WriteAllText(file, JsonSerializer.Serialize(dictionary, writeOptions));
                    if (settings.Convert)
                        Convert(file, overwrite: true);

                    // Ensure we set the last write time to the timestamp for easier check 
                    // when converting files.
                    File.SetLastWriteTimeUtc(file, DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime);
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
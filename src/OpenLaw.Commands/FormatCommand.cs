using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw;

[Description("Normaliza el formato de archivos JSON.")]
public class FormatCommand(IAnsiConsole console) : Command<FormatCommand.FormatSettings>
{
    static readonly JsonSerializerOptions readOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonDictionaryConverter() },
    };

    static readonly JsonSerializerOptions writeOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public class FormatSettings : CommandSettings
    {
        [Description("Ubicación de archivos a formatear. Por defecto '%AppData%\\clarius\\openlaw'")]
        [CommandOption("--dir")]
        public string Directory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\clarius\\openlaw");
    }

    public override int Execute(CommandContext context, FormatSettings settings)
    {
        if (Directory.Exists(settings.Directory))
        {
            console.Progress()
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                ])
                .Start(ctx =>
                {
                    var options = new ParallelOptions();
                    if (Debugger.IsAttached)
                        options.MaxDegreeOfParallelism = 1;

                    Parallel.ForEach(Directory.EnumerateFiles(settings.Directory, "*.json", SearchOption.AllDirectories),
                        options, file =>
                        {
                            var task = ctx.AddTask($"Formateando {file}");
                            task.IsIndeterminate = true;
                            FormatFile(file);
                            task.Value(100);
                        });
                });
        }

        return 0;
    }

    static void FormatFile(string file)
    {
        var json = File.ReadAllText(file);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, readOptions);
        if (dictionary is null)
            return;

        File.WriteAllText(file, JsonSerializer.Serialize(dictionary, writeOptions));

        if (dictionary.TryGetValue("document", out var document) &&
            document is Dictionary<string, object?> doc &&
            doc.TryGetValue("metadata", out var metadata) &&
            metadata is Dictionary<string, object?> meta &&
            meta.TryGetValue("timestamp", out var timestamp) &&
            (timestamp is double || timestamp is long))
        {
            var ts = timestamp is double ? Convert.ToInt64(timestamp) : (long)timestamp;

            File.SetLastWriteTimeUtc(file, DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime);
        }
    }
}

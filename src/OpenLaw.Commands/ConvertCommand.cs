using System.ComponentModel;
using System.Diagnostics;
using Clarius.OpenLaw.Argentina;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw;

[Description("Convierte archivos JSON a YAML, Markdown y PDF.")]
public class ConvertCommand(IAnsiConsole console) : Command<ConvertCommand.ConvertSettings>
{
    public override int Execute(CommandContext context, ConvertSettings settings)
    {
        if (settings.File is not null)
        {
            Convert(settings.File, settings);
            return 0;
        }

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
                            var task = ctx.AddTask($"Convirtiendo {file}");
                            task.IsIndeterminate = true;
                            Convert(file, settings);
                            task.Value(100);
                        });
                });
        }

        return 0;
    }

    static void Convert(string file, ConvertSettings settings)
        => JsonDataConverter.Convert(file, settings.Yaml, settings.Pdf, settings.Markdown, settings.Overwrite);

    public class ConvertSettings : CommandSettings
    {
        public override ValidationResult Validate()
        {
            if (!string.IsNullOrWhiteSpace(File) && !System.IO.File.Exists(File))
                return ValidationResult.Error("El archivo especificado '{File}' no existe.");

            return base.Validate();
        }

        [Description("Archivo a convertir. Opcional.")]
        [CommandArgument(0, "[file]")]
        public string? File { get; set; }

        [Description("Ubicación de archivos a convertir. Por defecto '%AppData%\\clarius\\openlaw'")]
        [CommandOption("--dir")]
        public string Directory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\clarius\\openlaw");

        [Description("Sobreescribir archivos existentes. Por defecto 'false'.")]
        [DefaultValue(false)]
        [CommandOption("--overwrite")]
        public bool Overwrite { get; set; } = false;

        [Description("Generar archivos YAML. Por defecto 'true'.")]
        [DefaultValue(true)]
        [CommandOption("--yaml")]
        public bool Yaml { get; set; } = true;

        [Description("Generar archivos PDF. Por defecto 'true'.")]
        [DefaultValue(true)]
        [CommandOption("--pdf")]
        public bool Pdf { get; set; } = true;

        [Description("Generar archivos Markdown. Por defecto 'true'.")]
        [DefaultValue(true)]
        [CommandOption("--md")]
        public bool Markdown { get; set; } = true;
    }
}

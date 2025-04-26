using System.ComponentModel;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw;

public class DownloadSettings : CommandSettings
{
    [Description("Descargar todos los documentos, no solamente Leyes de alcance Nacional.")]
    [DefaultValue(true)]
    [CommandOption("--all")]
    public bool All { get; set; } = false;

    [Description("Forzar formato en documentos existentes.")]
    [DefaultValue(false)]
    [CommandOption("--ff", IsHidden = true)]
    public bool ForceFormat { get; set; } = false;

    [Description("Convertir automaticamente documentos nuevos descargados a Markdown, PDF y YAML.")]
    [DefaultValue(true)]
    [CommandOption("--convert")]
    public bool Convert { get; set; } = false;

    [Description("Ubicación opcional para descarga de archivos. Por defecto '%AppData%\\clarius\\openlaw'")]
    [CommandOption("--dir")]
    public string Directory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\clarius\\openlaw");
}
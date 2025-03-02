using System.ComponentModel;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

public class SyncCommand : AsyncCommand<SyncCommand.SyncSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, SyncSettings settings)
    {

        return Task.FromResult(0);
    }

    public class SyncSettings : CommandSettings
    {
        [Description("Ubicación opcional archivos. Por defecto el directorio actual.")]
        [CommandOption("--dir")]
        public string Directory { get; set; } = System.IO.Directory.GetCurrentDirectory();

        [Description("Tipo de norma a sincronizar.")]
        [CommandOption("-t|--tipo")]
        public TipoNorma Tipo { get; set; } = TipoNorma.Ley;

        [Description("Jurisdicción a sincronizar.")]
        [CommandOption("-j|--jurisdiccion")]
        public Jurisdiccion Jurisdiccion { get; set; } = Jurisdiccion.Nacional;

        [Description("Provincia a sincronizar.")]
        [CommandOption("-p|--provincia")]
        public Provincia? Provincia { get; set; }

        public override ValidationResult Validate()
        {
            if (Jurisdiccion == Jurisdiccion.Provincial && Provincia == null)
                return ValidationResult.Error("Debe especificar una provincia para la jurisdicción provincial.");

            if (Jurisdiccion != Jurisdiccion.Provincial && Provincia != null)
                return ValidationResult.Error("No se puede especificar una provincia para la jurisdicción no provincial.");

            return base.Validate();
        }
    }
}

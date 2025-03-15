using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

public class ClientSettings : CommandSettings
{
    [Description("Tipo de norma a sincronizar")]
    [CommandOption("-t|--tipo")]
    [DefaultValue(TipoNorma.Ley)]
    public TipoNorma? Tipo { get; set; } = TipoNorma.Ley;

    [Description("Jurisdicción a sincronizar")]
    [CommandOption("-j|--jurisdiccion")]
    [DefaultValue(Argentina.Jurisdiccion.Nacional)]
    public Jurisdiccion? Jurisdiccion { get; set; } = Argentina.Jurisdiccion.Nacional;

    [Description("Provincia a sincronizar")]
    [CommandOption("-p|--provincia")]
    [DefaultValue(null)]
    public Provincia? Provincia { get; set; }

    [Description("Filtros avanzados a aplicar (KEY=VALUE)")]
    [CommandOption("-f|--filtro")]
    public Dictionary<string, string> Filters { get; set; } = [];

    [Description("Mostrar solo leyes/decretos vigentes")]
    [CommandOption("--vigentes")]
    public bool Vigentes { get; set; }

    [Description("Enumerar todo, sin filtros")]
    [CommandOption("--all", IsHidden = true)]
    public bool All { get; set; }

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

        if (Vigentes)
            Filters.AddFilter(KnownFilters.EstadoDeVigencia.VigenteDeAlcanceGeneral);

        return base.Validate();
    }
}

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

public class ClientSettings : CommandSettings
{
    [EnumDescription<TipoNorma>("Tipo de norma a sincronizar")]
    [CommandOption("-t|--tipo")]
    [DefaultValue(TipoNorma.Ley)]
    public TipoNorma? Tipo { get; set; } = TipoNorma.Ley;

    [DisplayValueDescription<Jurisdiccion>("Jurisdicción a sincronizar", lowerCase: true)]
    [CommandOption("-j|--jurisdiccion")]
    [DefaultValue(Argentina.Jurisdiccion.Nacional)]
    public Jurisdiccion? Jurisdiccion { get; set; } = Argentina.Jurisdiccion.Nacional;

    [EnumDescription<Provincia>("Provincia a sincronizar", lowerCase: true)]
    [CommandOption("-p|--provincia")]
    [DefaultValue(null)]
    public Provincia? Provincia { get; set; }

    [EnumDescription<ContentType>("Tipo de contenido a sincronizar", lowerCase: true)]
    [DefaultValue(ContentType.Legislacion)]
    [CommandOption("-c|--content-type")]
    public ContentType ContentType { get; set; } = ContentType.Legislacion;

    [Description("Filtros avanzados a aplicar (KEY=VALUE)")]
    [CommandOption("-f|--filtro")]
    public Dictionary<string, string> Filters { get; set; } = [];

    [Description("Mostrar solo leyes/decretos vigentes")]
    [CommandOption("--vigente")]
    public bool Vigente { get; set; }

    [Description("Mostrar solo leyes/decretos vigentes")]
    [CommandOption("--vigentes", IsHidden = true)]
    public bool Vigentes
    {
        get => Vigente;
        set => Vigente = value;
    }

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

        if (Vigente)
            Filters.AddFilter(KnownFilters.EstadoDeVigencia.VigenteDeAlcanceGeneral);

        return base.Validate();
    }
}

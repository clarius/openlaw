namespace Clarius.OpenLaw.Argentina;

/// <summary>
/// The parameters used to perform a search.
/// </summary>
public record Search(TipoNorma? Tipo, Jurisdiccion? Jurisdiccion, Provincia? Provincia)
{
    public static Search Empty { get; } = new(null, null, null);
}
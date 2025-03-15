namespace Clarius.OpenLaw.Argentina;

public static class KnownFilters
{
    public static IDictionary<string, string> AddFilter(this IDictionary<string, string> filters, string filter)
    {
        var parts = filter.Split('/');
        filters[parts[0]] = parts[1];
        return filters;
    }

    public static class EstadoDeVigencia
    {
        public const string VigenteDeAlcanceGeneral = "Estado de Vigencia/Vigente, de alcance general";
    }
}

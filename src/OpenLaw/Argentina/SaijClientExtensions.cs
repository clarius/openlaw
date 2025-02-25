using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Clarius.OpenLaw.Argentina;

public static class SaijClientExtensions
{
    public static async IAsyncEnumerable<JsonObject> EnumerateJsonAsync(this SaijClient client,
        TipoNorma? tipo = TipoNorma.Ley,
        Jurisdiccion? jurisdiccion = Jurisdiccion.Nacional,
        Provincia? provincia = default,
        int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await foreach (var doc in client.EnumerateAsync(tipo, jurisdiccion, provincia, skip, take, cancellation))
        {
            if (await client.FetchJsonAsync(doc.Id) is not { } json)
                continue;

            yield return json;
        }
    }
}

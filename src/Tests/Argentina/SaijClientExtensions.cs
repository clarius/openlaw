using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Devlooped;

namespace Clarius.OpenLaw.Argentina;

public static class SaijClientExtensions
{
    public static async IAsyncEnumerable<JsonObject> EnumerateJsonAsync(this SaijClient client,
        TipoNorma? tipo = TipoNorma.Ley,
        Jurisdiccion? jurisdiccion = Jurisdiccion.Nacional,
        Provincia? provincia = default,
        int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await foreach (var doc in client.SearchAsync(tipo, jurisdiccion, provincia, skip, take, cancellation))
        {
            if (await client.FetchJsonAsync(doc.Id) is not { } json)
                continue;

            yield return json;
        }
    }

    public static async Task<Legislation?> FetchDocumentAsync(this SaijClient client, string id)
    {
        if (await client.FetchAsync(id) is not { } data ||
            await data.GetAbstractAsync() is not Legislation doc)
        {
            Debugger.Launch();
            return null;
        }

        return doc with { Summary = StringMarkup.Cleanup(doc.Summary) };
    }

    public static async Task<JsonObject?> FetchJsonAsync(this SaijClient client, string id)
    {
        if (await client.FetchAsync(id) is not { } data ||
            JsonNode.Parse(data.Json) is not JsonObject json)
            return null;

        return json;
    }
}

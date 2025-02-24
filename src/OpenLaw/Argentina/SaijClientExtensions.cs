using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Clarius.OpenLaw.Argentina;

public static class SaijClientExtensions
{
    public static async IAsyncEnumerable<JsonObject> EnumerateJsonAsync(this SaijClient client, string? tipo = "Ley", string? jurisdiccion = "Nacional", int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await foreach (var doc in client.EnumerateAsync(tipo, jurisdiccion, skip, take, cancellation))
        {
            if (await client.FetchJsonAsync(doc.Id) is not { } json)
                continue;

            yield return json;
        }
    }
}

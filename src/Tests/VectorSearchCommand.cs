using System.ClientModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Clarius.OpenLaw;

//[Service(ServiceLifetime.Transient)]
public class VectorSearchCommand(IVectorStoreService stores, IOptions<OpenAISettings> settings)
{
    static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = Debugger.IsAttached,
    };

    readonly OpenAIClient oai = new(new ApiKeyCredential(settings.Value.Key));
    readonly ConcurrentDictionary<string, string> memory = new(StringComparer.OrdinalIgnoreCase);

    [Description("Perform a search on all currently active legal norms.")]
    public virtual async Task<string> Execute(string query)
    {
        if (memory.TryGetValue(query, out var cached))
            return cached;

        JsonNode? node = null;
        JsonArray? data = null;

        var content = new Dictionary<string, object>
        {
            ["query"] = query,
            ["rewrite_query"] = true,
            ["ranking_options"] = new { score_threshold = settings.Value.Score }
        };

        foreach (var store in await stores.GetStores())
        {
            var message = oai.Pipeline.CreateMessage();
            message.Request.Method = "POST";
            message.Request.Uri = new Uri($"https://api.openai.com/v1/vector_stores/{store.Id}/search");

            message.Request.Content = BinaryContent.Create(BinaryData.FromString(JsonSerializer.Serialize(content, options)));

            await oai.Pipeline.SendAsync(message);
            if (message.Response is null)
                continue;

            var response = JsonNode.Parse(message.Response.Content.ToString());
            if (response!["data"] is not JsonArray responseData)
                continue;

            // Aggregate results across multiple vector stores as needed.
            if (node is null)
            {
                node = response;
                data = responseData;
            }
            else if (data != null)
            {
                foreach (var item in responseData)
                {
                    if (item is null)
                        continue;

                    data.Add(item.DeepClone());
                }
            }
        }

        return memory.GetOrAdd(query, node?.ToJsonString(options) ?? string.Empty);
    }
}

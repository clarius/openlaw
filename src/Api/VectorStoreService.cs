using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Clarius.OpenLaw;

public record VectorStore(string Id, DateOnly From, DateOnly To)
{
    public VectorStore(string id) : this(id, DateOnly.MinValue, DateOnly.MaxValue) { }
}

[Service]
public class VectorStoreService(
    [FromKeyedServices("oai")] OpenAIClient client,
    IMemoryCache cache,
    ILogger<VectorStoreService> logger,
    IOptions<OpenAISettings> settings) : IVectorStoreService
{
    static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    const string CacheKey = "VectorStorages";
    readonly OpenAISettings settings = settings.Value;

    public async Task<IEnumerable<VectorStore>> GetStores(CancellationToken cancellation = default)
    {
        if (cache.TryGetValue(CacheKey, out List<VectorStore>? cachedStores) && cachedStores != null)
            return cachedStores;

        var stores = await LoadStoresAsync(cancellation);

        // Cache the result
        cache.Set(CacheKey, stores, CacheDuration);

        return stores;
    }

    public async Task<VectorStore?> GetStoreAsync(DateOnly documentDate, CancellationToken cancellation = default)
    {
        var storages = await GetStores(cancellation);
        return storages.FirstOrDefault(vs => documentDate >= vs.From && documentDate <= vs.To);
    }

    async Task<List<VectorStore>> LoadStoresAsync(CancellationToken cancellation)
    {
        logger.LogInformation("Fetching vector storages from OpenAI for agent {Agent}", settings.Agent);
        var result = new List<VectorStore>();

        try
        {
            // Retrieve vector stores with agent metadata matching settings.Agent
            var response = client.GetVectorStoreClient().GetVectorStoresAsync(cancellationToken: cancellation);
            await foreach (var store in response)
            {
                if (store.Metadata.TryGetValue("agent", out var agent) &&
                    agent.ToString() == settings.Agent)
                {
                    // Default values if metadata is missing
                    var from = DateOnly.MinValue;
                    var to = DateOnly.MaxValue;

                    if (store.Metadata.TryGetValue("from", out var fromValue) &&
                        !DateOnly.TryParse(fromValue.ToString(), out from))
                    {
                        from = DateOnly.MinValue;
                        logger.LogWarning("Invalid 'from' value in store {Id}, using MinValue", store.Id);
                    }

                    if (store.Metadata.TryGetValue("to", out var toValue) &&
                        !DateOnly.TryParse(toValue.ToString(), out to))
                    {
                        to = DateOnly.MaxValue;
                        logger.LogWarning("Invalid 'to' value in store {Id}, using MaxValue", store.Id);
                    }

                    result.Add(new VectorStore(store.Id, from, to));
                    logger.LogInformation("Found vector store {Name} ({Id}) for dates {From} to {To}",
                        store.Name, store.Id,
                        from == DateOnly.MinValue ? "MinValue" : from.ToString(),
                        to == DateOnly.MaxValue ? "MaxValue" : to.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving vector stores from OpenAI");
        }

        return result;
    }
}
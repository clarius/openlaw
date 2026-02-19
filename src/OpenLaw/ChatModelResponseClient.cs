using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Clarius.OpenLaw;

public class ChatModelResponseClient(string defaultModel, string apiKey, params ResponseTool[] tools) : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();

    public void Dispose()
    {
        foreach (var client in clients.Values)
        {
            if (client is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var modelId = options?.ModelId ?? defaultModel;
        var client = clients.GetOrAdd(modelId, modelId => new ResponsesClient(modelId, apiKey).AsIChatClient(tools));
        return client.GetResponseAsync(messages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var modelId = options?.ModelId ?? defaultModel;
        var client = clients.GetOrAdd(modelId, modelId => new ResponsesClient(modelId, apiKey).AsIChatClient(tools));
        return client.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => clients
        .GetOrAdd(defaultModel, modelId => new ResponsesClient(modelId, apiKey).AsIChatClient(tools))
        .GetService(serviceType, serviceKey);
}
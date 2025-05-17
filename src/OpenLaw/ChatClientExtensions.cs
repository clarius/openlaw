using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Clarius.OpenLaw;

public static class ChatClientExtensions
{
    extension(ChatOptions options)
    {
        public string? EndUserId
        {
            get => options.AdditionalProperties?[nameof(ResponseCreationOptions.EndUserId)] as string;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(ResponseCreationOptions.EndUserId)] = value;
                }
            }
        }

        public string? Instructions
        {
            get => options.AdditionalProperties?[nameof(ResponseCreationOptions.Instructions)] as string;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(ResponseCreationOptions.Instructions)] = value;
                }
            }
        }

        public IDictionary<string, string>? Metadata
        {
            get => options.AdditionalProperties?[nameof(ResponseCreationOptions.Metadata)] as IDictionary<string, string>;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(ResponseCreationOptions.Metadata)] = value;
                }
            }
        }

        public ResponseReasoningOptions? ReasoningOptions
        {
            get => options.AdditionalProperties?[nameof(ResponseCreationOptions.ReasoningOptions)] as ResponseReasoningOptions;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(ResponseCreationOptions.ReasoningOptions)] = value;
                }
            }
        }

        public bool? StoredOutputEnabled
        {
            get => options.AdditionalProperties?[nameof(ResponseCreationOptions.StoredOutputEnabled)] as bool?;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(ResponseCreationOptions.StoredOutputEnabled)] = value;
                }
            }
        }

        public ResponseTruncationMode? TruncationMode
        {
            get => options.AdditionalProperties?[nameof(ResponseCreationOptions.TruncationMode)] as ResponseTruncationMode?;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(ResponseCreationOptions.TruncationMode)] = value;
                }
            }
        }
    }

    public static IChatClient AsIChatClient(this OpenAIResponseClient client, params ResponseTool[] tools)
        => tools.Length == 0 ? OpenAIClientExtensions.AsIChatClient(client) : new ToolsReponseClient(client, tools).AsIChatClient();

    class ToolsReponseClient(OpenAIResponseClient inner, ResponseTool[] tools) : OpenAIResponseClient
    {
        public override Task<ClientResult<OpenAIResponse>> CreateResponseAsync(IEnumerable<ResponseItem> inputItems, ResponseCreationOptions options = null, CancellationToken cancellationToken = default)
            => inner.CreateResponseAsync(inputItems, AddTools(options), cancellationToken);

        public override AsyncCollectionResult<StreamingResponseUpdate> CreateResponseStreamingAsync(IEnumerable<ResponseItem> inputItems, ResponseCreationOptions options = null, CancellationToken cancellationToken = default)
            => inner.CreateResponseStreamingAsync(inputItems, AddTools(options), cancellationToken);

        ResponseCreationOptions AddTools(ResponseCreationOptions options)
        {
            foreach (var tool in tools)
                options.Tools.Add(tool);

            return options;
        }
    }
}

using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Clarius.OpenLaw;

public static class ChatClientExtensions
{
    extension(ChatOptions options)
    {
        public string? EndUserId
        {
            get => options.AdditionalProperties?[nameof(CreateResponseOptions.EndUserId)] as string;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(CreateResponseOptions.EndUserId)] = value;
                }
            }
        }

        public IDictionary<string, string>? Metadata
        {
            get => options.AdditionalProperties?[nameof(CreateResponseOptions.Metadata)] as IDictionary<string, string>;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(CreateResponseOptions.Metadata)] = value;
                }
            }
        }

        public bool? StoredOutputEnabled
        {
            get => options.AdditionalProperties?[nameof(CreateResponseOptions.StoredOutputEnabled)] as bool?;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(CreateResponseOptions.StoredOutputEnabled)] = value;
                }
            }
        }

        public ResponseTruncationMode? TruncationMode
        {
            get => options.AdditionalProperties?[nameof(CreateResponseOptions.TruncationMode)] as ResponseTruncationMode?;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties[nameof(CreateResponseOptions.TruncationMode)] = value;
                }
            }
        }
    }

    public static IChatClient AsIChatClient(this ResponsesClient client, params ResponseTool[] tools)
    {
        var chatClient = OpenAIClientExtensions.AsIChatClient(client);
        if (tools.Length == 0)
            return chatClient;

        return chatClient
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                options.Tools ??= [];
                foreach (var tool in tools)
                    options.Tools.Add(tool);
            })
            .Build();
    }
}
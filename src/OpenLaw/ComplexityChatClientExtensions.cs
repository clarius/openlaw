using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Clarius.OpenLaw;

public static class ComplexityChatClientExtensions
{
    public static ChatClientBuilder UseComplexityAssessment(this ChatClientBuilder builder, ComplexityAssessment assessment)
        => builder.Use((IChatClient innerClient, IServiceProvider services) =>
            new ComplexityChatClient(innerClient, assessment));

    class ComplexityChatClient(IChatClient inner, ComplexityAssessment assessment) : DelegatingChatClient(inner)
    {
        public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options?.ModelId is not null)
                return await base.GetResponseAsync(messages, options, cancellationToken);

            var message = messages.LastOrDefault(x => x.Role == ChatRole.User);
            if (message is null)
                return await base.GetResponseAsync(messages, options, cancellationToken);

            options ??= new ChatOptions();

            var complexity = await assessment.EvaluateAsync(message.Text, cancellationToken);

            options.ModelId = complexity switch
            {
                ComplexityLevel.Low => "gpt-4.1-nano",
                ComplexityLevel.Medium => "gpt-4.1-mini",
                ComplexityLevel.High => "gpt-4.1",
                _ => options.ModelId,
            };

            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
    }
}
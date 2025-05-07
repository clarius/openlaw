using System.Text.RegularExpressions;
using Devlooped.WhatsApp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clarius.OpenLaw;

[Service]
public partial class WhatsApp(IChatClient chat, IWhatsAppClient wa, ComplexityAssessment complexity, IOptionsSnapshot<AgentSettings> settings, ILogger<WhatsApp> logger) : IWhatsAppHandler
{
    public async Task HandleAsync(Message message)
    {
        if (string.IsNullOrEmpty(message.To.Id))
        {
            logger.LogWarning("Skipping work item without a To number: {From} 👉 {Id}", message.From.Name, message.Id);
            return;
        }

        logger.LogInformation("Processing work item: {From} 👉 {Id}", message.From.Name, message.Id);

        if (message is ErrorMessage error)
        {
            await ProcessErrorAsync(error);
        }
        else if (message is ContentMessage content)
        {
            await ProcessContentAsync(content);
        }
    }

    async Task ProcessErrorAsync(ErrorMessage error)
    {
        // Reengagement error, we need to invite the user.
        if (error.Error.Code == 131047)
        {
            await wa.SendAsync(error.To.Id, new
            {
                messaging_product = "whatsapp",
                to = error.From.Number,
                type = "template",
                template = new
                {
                    name = "reengagement",
                    language = new
                    {
                        code = "es_AR"
                    }
                }
            });
            return;
        }

        logger.LogError("Unknown error message received: {Error}", error.Error.Message);
    }

    async Task ProcessContentAsync(ContentMessage message)
    {
        if (message.Content is not Devlooped.WhatsApp.TextContent { Text: var text })
        {
            await wa.ReplyAsync(message, "🤖 Todavía no aprendí a procesar este tipo de mensajes, pero aprendo rápido!");
            return;
        }

        await wa.ReactAsync(message, "🧠");

        var options = new ChatOptions
        {
            EndUserId = message.From.Number,
            Instructions = ThisAssembly.Resources.Instructions.Text,
            StoredOutputEnabled = true
        };

        if (message.From.Number == settings.Value.Admin)
        {
            if (text.StartsWith('@') && GptModel().Match(text) is var match && match.Success)
            {
                options.ModelId = match.Value.TrimStart('@');
                text = text.Replace(match.Value, string.Empty).Trim();
            }
        }

        var response = await chat.GetResponseAsync(text, options);
        var reply = response.Text;

        if (message.From.Number == settings.Value.Admin)
        {
            reply += Environment.NewLine + "--" + Environment.NewLine + $"🤖 by {options.ModelId}";
        }

        await wa.ReplyAsync(message, reply);
        await wa.ReactAsync(message, "");
    }

    [GeneratedRegex(@"^@[^\s]+")]
    private partial Regex GptModel();
}

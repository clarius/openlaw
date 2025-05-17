using Microsoft.Extensions.AI;

namespace Clarius.OpenLaw;

public static class WhatsAppExtensions
{
    extension(ChatMessage message)
    {
        public string? WhatsAppId => message.AdditionalProperties?["WhatsAppId"] as string;
    }
}

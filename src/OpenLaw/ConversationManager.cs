using Microsoft.Extensions.AI;

namespace Clarius.OpenLaw;

public class ConversationManager(ISystemIdMapper idMapper, AgentSettings settings)
{
    public IList<ChatMessage> AppendAsync(ChatMessage message)
    {
        throw new NotImplementedException();
    }
}

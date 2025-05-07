using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Clarius.OpenLaw;

public class ConversationManagerTests
{
    [Fact]
    public async Task GetConversation()
    {
        var manager = new ConversationManager(new MemorySystemIdMapper(), new AgentSettings());
        //ChatMessage message = new ChatMessage("Hello, world!", "user", DateTime.UtcNow);
    }
}

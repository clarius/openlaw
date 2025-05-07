namespace Clarius.OpenLaw;

public class AgentSettings
{
    public string? Admin { get; set; }
    public string? Instructions { get; set; } = ThisAssembly.Resources.Instructions.Text;
    public int TimeToLive { get; set; } = 900; // 15 minutes
}

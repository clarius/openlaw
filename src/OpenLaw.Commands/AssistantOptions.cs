namespace Clarius.OpenLaw;

public class AssistantOptions
{
    /// <summary>
    /// The assistant id.
    /// </summary>
    public required string Id { get; set; }
    /// <summary>
    /// The assistant access key/token.
    /// </summary>
    public required string Key { get; set; }
    /// <summary>
    /// The assistant endpoint uri.
    /// </summary>
    public required string Uri { get; set; }
    /// <summary>
    /// The assistant vector store identifier.
    /// </summary>
    public required string Store { get; set; }
}

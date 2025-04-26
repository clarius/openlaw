namespace Clarius.OpenLaw.Argentina;

public interface IWebDocument
{
    /// <summary>
    /// Identifier for the web document.
    /// </summary>
    string Id { get; }
    /// <summary>
    /// The source JSON for the web document.
    /// </summary>
    string Json { get; }
    /// <summary>
    /// The projection from the source JSON for the web document that was 
    /// used to deserialize the instance.
    /// </summary>
    string JQ { get; }
    /// <summary>
    /// The dictionary representation of the web document.
    /// </summary>
    Dictionary<string, object?> Data { get; }

    string WebUrl => $"https://www.saij.gob.ar/{Id}";
    string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";
}

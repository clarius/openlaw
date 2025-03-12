using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Clarius.OpenLaw.Argentina;

/// <summary>
/// Tracks references of different kinds from and to other documents.
/// </summary>
public record References(Reference Ammends, Reference Repeals, Reference Remarks);

/// <summary>
/// Links documents via references.
/// </summary>
/// <param name="By">Incoming references from other documents.</param>
/// <param name="To">Outgoing references to other documents.</param>
public record Reference(
    [property: YamlIgnore] HashSet<string> By,
    [property: YamlIgnore] HashSet<string> To)
{
    // NOTE: when deserializing from JSON, we'll get the full By/To list.
    // When serializing to YAML, though, we only care about the count (for now).
    // This keeps the markdown docs small. If we need the full data, we'd need 
    // to fetch the original JSON anyway (i.e. which articles in particular are 
    // referenced and so on).
    [YamlMember(Alias = nameof(By)), JsonIgnore]
    public int ByCount => By.Count;

    [YamlMember(Alias = nameof(To)), JsonIgnore]
    public int ToCount => To.Count;
}
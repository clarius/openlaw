using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

record NormalizedWebDocument(string Id) : IWebDocument
{
    Dictionary<string, object?> data = new();
    string json = "{}";
    string jq = "{}";

    [JsonIgnore]
    public Dictionary<string, object?> Data => data;

    [JsonIgnore]
    public string Json
    {
        get => json;
        init
        {
            data = JsonSerializer.Deserialize<Dictionary<string, object?>>(value, JsonOptions.Default) ?? [];
            json = JsonSerializer.Serialize(data, JsonOptions.Indented);
        }
    }

    [JsonIgnore]
    public string JQ
    {
        get => jq;
        init => jq = value;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clarius.OpenLaw;

/// <summary>
/// JSON Patch operation kind as defined by RFC 6902.
/// </summary>
public enum JsonOperation
{
    Add,
    Remove,
    Replace,
    Move,
    Copy,
    Test
}

/// <summary>
/// Represents a JSON Patch operation with a specified path and operation type.
/// </summary>
/// <param name="Path">Specifies the location in the JSON document where the operation will be applied.</param>
/// <param name="Kind">Indicates the type of operation to perform on the specified path.</param>
[JsonConverter(typeof(JsonPatchConverter))]
public abstract partial record JsonPatch(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("op")] JsonOperation Kind)
{
    class JsonPatchConverter : JsonConverter<JsonPatch>
    {
        public override JsonPatch? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (!root.TryGetProperty("op", out var opElement) || string.IsNullOrEmpty(opElement.GetString()))
                throw new JsonException("Invalid or missing 'op' property for JsonPatch.");

            if (!Enum.TryParse(opElement.GetString(), true, out JsonOperation operation))
                throw new JsonException("Invalid 'op' value for JsonPatch.");

            return operation switch
            {
                JsonOperation.Add => root.Deserialize(JsonPatchContext.Default.JsonPatchAdd)
                    ?? throw new JsonException("Deserialization failed for JsonPatchAdd."),
                JsonOperation.Remove => root.Deserialize(JsonPatchContext.Default.JsonPatchRemove)
                    ?? throw new JsonException("Deserialization failed for JsonPatchRemove."),
                JsonOperation.Replace => root.Deserialize(JsonPatchContext.Default.JsonPatchReplace)
                    ?? throw new JsonException("Deserialization failed for JsonPatchReplace."),
                JsonOperation.Move => root.Deserialize(JsonPatchContext.Default.JsonPatchMove)
                    ?? throw new JsonException("Deserialization failed for JsonPatchMove."),
                JsonOperation.Copy => root.Deserialize(JsonPatchContext.Default.JsonPatchCopy)
                    ?? throw new JsonException("Deserialization failed for JsonPatchCopy."),
                JsonOperation.Test => root.Deserialize(JsonPatchContext.Default.JsonPatchTest)
                    ?? throw new JsonException("Deserialization failed for JsonPatchTest."),
                _ => throw new JsonException($"Unsupported operation: {operation}")
            };
        }

        public override void Write(Utf8JsonWriter writer, JsonPatch value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UseStringEnumConverter = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        WriteIndented = true
        )]
    [JsonSerializable(typeof(JsonPatch))]
    [JsonSerializable(typeof(JsonPatchAdd))]
    [JsonSerializable(typeof(JsonPatchRemove))]
    [JsonSerializable(typeof(JsonPatchReplace))]
    [JsonSerializable(typeof(JsonPatchMove))]
    [JsonSerializable(typeof(JsonPatchCopy))]
    [JsonSerializable(typeof(JsonPatchTest))]
    partial class JsonPatchContext : JsonSerializerContext { }

    internal class ObjectConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(object);

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return ReadElement(document.RootElement);
        }

        object? ReadElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ReadObject(element),
                JsonValueKind.Array => ReadArray(element),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => throw new JsonException($"Unsupported JsonValueKind: {element.ValueKind}"),
            };
        }

        object ReadObject(JsonElement element)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var property in element.EnumerateObject())
                dict[property.Name] = ReadElement(property.Value);
            return dict;
        }

        object ReadArray(JsonElement element)
        {
            var list = new List<object?>();
            foreach (var item in element.EnumerateArray())
                list.Add(ReadElement(item));
            return list;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}

/// <summary>
/// Represents an operation to add a value at a specified location in a JSON document.
/// </summary>
/// <param name="Path">Specifies the location in the JSON document where the new value will be added.</param>
/// <param name="Value">Holds the value that will be added to the JSON document at the specified location.</param>
public record JsonPatchAdd(
    string Path,
    [property: JsonPropertyName("value")]
    [property: JsonConverter(typeof(JsonPatch.ObjectConverter))] object Value) : JsonPatch(Path, JsonOperation.Add);

/// <summary>
/// Represents a JSON Patch operation to remove a specified value from a JSON document.
/// </summary>
/// <param name="Path">Specifies the location in the JSON document where the value should be removed.</param>
public record JsonPatchRemove(string Path) : JsonPatch(Path, JsonOperation.Remove);

/// <summary>
/// Represents a JSON Patch operation that replaces a value at a specified location in a JSON document. 
/// </summary>
/// <param name="Path">Specifies the location in the JSON document where the replacement should occur.</param>
/// <param name="Value">Holds the new value that will replace the existing value at the specified location.</param>
public record JsonPatchReplace(
    string Path,
    [property: JsonPropertyName("value")]
    [property: JsonConverter(typeof(JsonPatch.ObjectConverter))] object Value) : JsonPatch(Path, JsonOperation.Replace);

/// <summary>
/// Represents a move operation in a JSON Patch document, specifying the source and destination paths for the move.
/// </summary>
/// <param name="From">Indicates the location from which the value should be moved.</param>
/// <param name="Path">Specifies the target location where the value should be moved to.</param>
public record JsonPatchMove(string Path, [property: JsonPropertyName("from")] string From) : JsonPatch(Path, JsonOperation.Move);

/// <summary>
/// Represents a JSON Patch operation that copies a value from one location to another within a JSON document.
/// </summary>
/// <param name="From">Specifies the source location from which the value will be copied.</param>
/// <param name="Path">Indicates the destination location where the value will be placed.</param>
public record JsonPatchCopy(string Path, [property: JsonPropertyName("from")] string From) : JsonPatch(Path, JsonOperation.Copy);

/// <summary>
/// Represents a JSON Patch operation that tests whether a specified value matches the current value at a given path.
/// </summary>
/// <param name="Path">Specifies the location in the JSON document to be tested for a match.</param>
/// <param name="Value">Holds the value that is expected to be found at the specified location.</param>
public record JsonPatchTest(
    string Path,
    [property: JsonPropertyName("value")]
    [property: JsonConverter(typeof(JsonPatch.ObjectConverter))] object Value) : JsonPatch(Path, JsonOperation.Test);

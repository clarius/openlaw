﻿using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Clarius.OpenLaw;

public partial class JsonDictionaryConverter : JsonConverter<Dictionary<string, object?>>
{
    public override Dictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var dictionary = new Dictionary<string, object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dictionary;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string propertyName = reader.GetString() ?? throw new JsonException();

            reader.Read();

            dictionary[propertyName] = ReadValue(ref reader, options);
        }

        return dictionary;
    }

    object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                return Read(ref reader, typeof(Dictionary<string, object?>), options);
            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return list;

                    list.Add(ReadValue(ref reader, options));
                }
                throw new JsonException();
            case JsonTokenType.String:
                return ProcessString(reader.GetString());
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue))
                    return intValue;
                return reader.GetDouble();
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null;
            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options) 
        => throw new NotImplementedException();

    string? ProcessString(string? value)
    {
        if (value == null)
            return null;

        var replaced = value.Replace("[[p]]", "\n").Replace("[[/p]]", "\n");
        var multiline = MultilineExpr().Replace(replaced, "\n\n");
        var clean = RemoveMarkup().Replace(multiline, string.Empty);

        return clean.Trim();
    }

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex MultilineExpr();

    [GeneratedRegex(@"\[\[(/?\w+[^\]]*)\]\]")]
    private static partial Regex RemoveMarkup();
}

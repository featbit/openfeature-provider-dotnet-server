using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace FeatBit.OpenFeature.ServerProvider;

internal static partial class JsonConverters
{
    private static readonly OpenFeatureJsonSerializerContext JsonContext = new(
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new OpenFeatureValueJsonConverter(),
                new OpenFeatureStructureJsonConverter()
            }
        }
    );

    internal static bool TryDeserializeValue(string? json, out Value value)
    {
        if (json is null or "null")
        {
            value = new Value();
            return true;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize(json, JsonContext.Value);
            value = deserialized ?? new Value();
            return deserialized is not null;
        }
        catch (Exception)
        {
            value = new Value();
            return false;
        }
    }

    internal static string SerializeValue(Value value)
    {
        return JsonSerializer.Serialize(value, JsonContext.Value);
    }

    private sealed class OpenFeatureValueJsonConverter : JsonConverter<Value>
    {
        public override Value? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.StartObject =>
                    new Value(JsonSerializer.Deserialize<Structure>(ref reader, options)!),
                JsonTokenType.StartArray =>
                    new Value(JsonSerializer.Deserialize<IImmutableList<Value>>(ref reader, options)!),
                JsonTokenType.String => new Value(reader.GetString()!),
                JsonTokenType.Number => new Value(reader.GetDouble()),
                JsonTokenType.True => new Value(true),
                JsonTokenType.False => new Value(false),
                JsonTokenType.Null => new Value(),
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.AsObject, options);
        }
    }

    private sealed class OpenFeatureStructureJsonConverter : JsonConverter<Structure>
    {
        public override Structure? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<IDictionary<string, Value>>(ref reader, options);
            return dict is null ? null : new Structure(dict);
        }

        public override void Write(Utf8JsonWriter writer, Structure value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.AsDictionary(), options);
        }
    }

    [JsonSerializable(typeof(Value))]
    [JsonSerializable(typeof(Structure))]
    [JsonSerializable(typeof(IDictionary<string, Value>))]
    [JsonSerializable(typeof(IImmutableDictionary<string, Value>))]
    private partial class OpenFeatureJsonSerializerContext : JsonSerializerContext
    {
    }
}
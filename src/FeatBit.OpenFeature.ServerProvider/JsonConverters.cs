using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace FeatBit.OpenFeature.ServerProvider;

internal static partial class JsonConverters
{
    internal static bool TryDeserializeValue(string? json, out Value value)
    {
        if (json is null or "null")
        {
            value = new Value();
            return true;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize(json, OpenFeatureJsonSerializerContext.Default.Value);
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
        return JsonSerializer.Serialize(value, OpenFeatureJsonSerializerContext.Default.Value);
    }

    [JsonSerializable(typeof(Value))]
    private partial class OpenFeatureJsonSerializerContext : JsonSerializerContext
    {
    }
}
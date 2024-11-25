using OpenFeature.Model;

namespace FeatBit.OpenFeature.ServerProvider;

internal delegate bool ValueConverter<TValue>(string value, out TValue converted);

internal static class ValueConverters
{
    internal static readonly ValueConverter<bool> Bool = (string value, out bool converted) =>
        bool.TryParse(value, out converted);

    internal static readonly ValueConverter<string> String = (string value, out string converted) =>
    {
        converted = value;
        return true;
    };

    public static readonly ValueConverter<int> Int = (string value, out int converted) =>
        int.TryParse(value, out converted);

    public static readonly ValueConverter<double> Double = (string value, out double converted) =>
        double.TryParse(value, out converted);

    public static readonly ValueConverter<Value> Value = (string value, out Value converted) =>
        JsonConverters.TryDeserializeValue(value, out converted);
}
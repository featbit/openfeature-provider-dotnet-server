using System.Globalization;

using FeatBit.Sdk.Server.Model;

using OpenFeature.Model;

namespace FeatBit.OpenFeature.Provider;

internal static class EvaluationContextConversions
{
    public static FbUser ToFbUser(this EvaluationContext? context)
    {
        var builder = FbUser.Builder(context?.TargetingKey);
        if (context is not null)
        {
            foreach (var pair in context)
            {
                if (pair.Key != "targetingKey" && TryGetFbValue(pair.Value, out var fbValue))
                {
                    builder.AddAttribute(pair.Key, fbValue);
                }
            }
        }

        return builder.Build();
    }

    private static void AddAttribute(this IFbUserBuilder builder, string key, string? value)
    {
        if (key == "name")
        {
            builder.Name(value);
        }
        else
        {
            builder.Custom(key, value);
        }
    }

    private static bool TryGetFbValue(Value value, out string? result)
    {
        switch (value.AsObject)
        {
            case string s:
                result = s;
                return true;

            case bool b:
                result = b.ToString();
                return true;

            case double d:
                result = d.ToString(CultureInfo.InvariantCulture);
                return true;

            case DateTime time:
                result = time.ToString("O", CultureInfo.InvariantCulture);
                return true;

            case Structure or IEnumerable<Value>:
                result = JsonConversions.SerializeValue(value);
                return true;

            default:
                result = default;
                return false;
        }
    }
}

using System.Globalization;
using FeatBit.Sdk.Server.Model;
using OpenFeature.Model;

namespace FeatBit.OpenFeature.ServerProvider;

internal static class EvaluationContextExtensions
{
    private static readonly FbUser EmptyFbUser = FbUser.Builder(string.Empty).Build();

    public static FbUser AsFbUser(this EvaluationContext? context)
    {
        if (context is null)
        {
            return EmptyFbUser;
        }

        var builder = FbUser.Builder(context.TargetingKey ?? string.Empty);

        foreach (var pair in context)
        {
            if (pair.Key == "targetingKey")
            {
                continue;
            }

            if (TryGetFbValue(pair.Value, out var fbValue))
            {
                builder.AddAttribute(pair.Key, fbValue);
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
                result = JsonConverters.SerializeValue(value);
                return true;

            default:
                result = default;
                return false;
        }
    }
}
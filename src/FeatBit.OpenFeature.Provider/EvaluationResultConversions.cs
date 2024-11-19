using FeatBit.Sdk.Server.Evaluation;

using OpenFeature.Constant;
using OpenFeature.Model;

namespace FeatBit.OpenFeature.Provider;

internal static class EvaluationResultConversions
{
    public static ResolutionDetails<T> ToResolutionDetails<T>(this EvalDetail<T> detail, string flagKey, T defaultValue)
    {
        return ToResolutionDetails<T, T>(detail, flagKey, defaultValue, static v => v);
    }

    public static ResolutionDetails<TOut> ToResolutionDetails<TIn, TOut>(this EvalDetail<TIn> detail, string flagKey,
        TOut defaultValue, Func<TIn, TOut> convert)
    {
        return detail.Kind switch
        {
            ReasonKind.Off =>
                GetSuccessfulResolution(flagKey, detail.Value, Reason.Disabled, convert, defaultValue),

            ReasonKind.Fallthrough =>
                GetSuccessfulResolution(flagKey, detail.Value, Reason.Default, convert, defaultValue),

            ReasonKind.TargetMatch or ReasonKind.RuleMatch =>
                GetSuccessfulResolution(flagKey, detail.Value, Reason.TargetingMatch, convert, defaultValue),

            ReasonKind.ClientNotReady =>
                GetFailedResolution(flagKey, ErrorType.ProviderNotReady, detail.Reason, defaultValue),

            ReasonKind.WrongType =>
                GetFailedResolution(flagKey, ErrorType.TypeMismatch, detail.Reason, defaultValue),

            ReasonKind.Error when detail.Reason == "flag not found" =>
                GetFailedResolution(flagKey, ErrorType.FlagNotFound, detail.Reason, defaultValue),

            ReasonKind.Error =>
                GetFailedResolution(flagKey, ErrorType.General, detail.Reason, defaultValue),

            _ => new ResolutionDetails<TOut>(
                flagKey: flagKey,
                value: defaultValue,
                errorType: ErrorType.General,
                reason: Reason.Unknown,
                errorMessage: $"Unknown reason kind: {detail.Kind}")
        };
    }

    private static ResolutionDetails<TOut> GetFailedResolution<TOut>(string flagKey, ErrorType errorType, string reason,
        TOut defaultValue)
    {
        return new ResolutionDetails<TOut>(
            flagKey: flagKey,
            value: defaultValue,
            errorType: errorType,
            reason: reason,
            errorMessage: reason);
    }

    private static ResolutionDetails<TOut> GetSuccessfulResolution<TIn, TOut>(string flagKey, TIn value, string reason,
        Func<TIn, TOut> convert, TOut defaultValue)
    {
        TOut convertedValue;
        try
        {
            convertedValue = convert(value);
        }
        catch (Exception ex)
        {
            return GetFailedResolution(flagKey, ErrorType.TypeMismatch, ex.Message, defaultValue);
        }

        return new ResolutionDetails<TOut>(
            flagKey: flagKey,
            value: convertedValue,
            errorType: ErrorType.None,
            reason: reason,
            // FeatBit does not return the variation name so use stringified value as OpenTelemetry suggests
            // https://opentelemetry.io/docs/specs/semconv/feature-flags/feature-flags-spans/
            variant: value?.ToString());
    }
}

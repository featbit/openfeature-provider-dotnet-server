using FeatBit.Sdk.Server.Evaluation;
using OpenFeature.Constant;

namespace FeatBit.OpenFeature.ServerProvider;

internal static class EvalDetailExtensions
{
    public static bool IsError<TValue>(this EvalDetail<TValue> evalDetail)
    {
        var kind = evalDetail.Kind;

        return kind is
            ReasonKind.ClientNotReady or
            ReasonKind.WrongType or
            ReasonKind.Error;
    }

    public static ErrorType GetErrorType<TValue>(this EvalDetail<TValue> evalDetail)
    {
        var kind = evalDetail.Kind;
        var reason = evalDetail.Reason;

        return kind switch
        {
            ReasonKind.ClientNotReady => ErrorType.ProviderNotReady,
            ReasonKind.WrongType => ErrorType.TypeMismatch,
            ReasonKind.Error => reason == "flag not found" ? ErrorType.FlagNotFound : ErrorType.General,
            _ => ErrorType.None
        };
    }

    public static string GetOpenFeatureReason<TValue>(this EvalDetail<TValue> evalDetail)
    {
        var kind = evalDetail.Kind;

        return kind switch
        {
            ReasonKind.ClientNotReady => Reason.Error,
            ReasonKind.WrongType => Reason.Error,
            ReasonKind.Error => Reason.Error,

            ReasonKind.Off => Reason.Disabled,
            ReasonKind.TargetMatch => Reason.TargetingMatch,
            ReasonKind.RuleMatch => Reason.Split,
            ReasonKind.Fallthrough => Reason.Default,

            _ => Reason.Unknown
        };
    }
}
using FeatBit.Sdk.Server.Evaluation;
using OpenFeature.Constant;
using Xunit;

namespace FeatBit.OpenFeature.ServerProvider.Tests;

public class EvalDetailTests
{
    [Theory]
    [InlineData(ReasonKind.ClientNotReady, "client not ready", ErrorType.ProviderNotReady)]
    [InlineData(ReasonKind.WrongType, "wrong type", ErrorType.TypeMismatch)]
    [InlineData(ReasonKind.Error, "flag not found", ErrorType.FlagNotFound)]
    [InlineData(ReasonKind.Error, "malformed flag", ErrorType.General)]
    [InlineData(ReasonKind.Off, "flag off", ErrorType.None)]
    [InlineData(ReasonKind.TargetMatch, "target match", ErrorType.None)]
    [InlineData(ReasonKind.RuleMatch, "match rule qa-rule", ErrorType.None)]
    [InlineData(ReasonKind.Fallthrough, "fall through targets and rules", ErrorType.None)]
    public void GetErrorType(ReasonKind kind, string reason, ErrorType expected)
    {
        var detail = new EvalDetail<string>("key", kind, reason, "value");

        var actual = detail.GetErrorType();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(ReasonKind.ClientNotReady, Reason.Error)]
    [InlineData(ReasonKind.WrongType, Reason.Error)]
    [InlineData(ReasonKind.Error, Reason.Error)]
    [InlineData(ReasonKind.Off, Reason.Disabled)]
    [InlineData(ReasonKind.TargetMatch, Reason.TargetingMatch)]
    [InlineData(ReasonKind.RuleMatch, Reason.Split)]
    [InlineData(ReasonKind.Fallthrough, Reason.Default)]
    [InlineData((ReasonKind)10, Reason.Unknown)]
    public void GetOpenFeatureReason(ReasonKind kind, string expected)
    {
        var detail = new EvalDetail<string>("key", kind, "reason", "value");

        var actual = detail.GetOpenFeatureReason();
        Assert.Equal(expected, actual);
    }
}
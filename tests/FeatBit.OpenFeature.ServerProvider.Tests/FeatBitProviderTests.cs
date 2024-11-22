using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Evaluation;
using FeatBit.Sdk.Server.Model;
using Moq;
using OpenFeature.Constant;
using OpenFeature.Model;
using Xunit;

namespace FeatBit.OpenFeature.ServerProvider.Tests;

public sealed class FeatBitProviderTests
{
    private static readonly EvaluationContext Context = EvaluationContext
        .Builder()
        .SetTargetingKey("test-key")
        .Build();

    private readonly Mock<IFbClient> _fbClient;
    private readonly FeatBitProvider _subject;

    public FeatBitProviderTests()
    {
        _fbClient = new Mock<IFbClient>();
        _subject = new FeatBitProvider(_fbClient.Object);
    }

    [Fact]
    public void FbClientCannotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeatBitProvider(null!));
    }

    [Fact]
    public void MetadataName()
    {
        Assert.Equal("FeatBit.OpenFeature.ServerProvider", _subject.GetMetadata().Name);
    }

    [Theory]
    [InlineData(ReasonKind.ClientNotReady, "client not ready", ErrorType.ProviderNotReady)]
    [InlineData(ReasonKind.WrongType, "wrong type", ErrorType.TypeMismatch)]
    [InlineData(ReasonKind.Error, "flag not found", ErrorType.FlagNotFound)]
    [InlineData(ReasonKind.Error, "malformed flag", ErrorType.General)]
    public async Task ResolveStringError(ReasonKind kind, string reason, ErrorType expectedErrorType)
    {
        Setup(kind, reason, "value");

        var detail = await _subject.ResolveStringValueAsync("flag-key", "fallback-value-on-error", Context);

        Assert.Equal("flag-key", detail.FlagKey);
        Assert.Equal("fallback-value-on-error", detail.Value);
        Assert.Equal(Reason.Error, detail.Reason);

        Assert.Equal(expectedErrorType, detail.ErrorType);
        Assert.Equal(reason, detail.ErrorMessage);

        Assert.Null(detail.Variant);
    }

    [Theory]
    [InlineData(ReasonKind.Off, "flag off", Reason.Disabled)]
    [InlineData(ReasonKind.TargetMatch, "target match", Reason.TargetingMatch)]
    [InlineData(ReasonKind.RuleMatch, "match rule qa-rule", Reason.Split)]
    [InlineData(ReasonKind.Fallthrough, "fall through targets and rules", Reason.Default)]
    public async Task ResolveStringSuccess(ReasonKind kind, string reason, string expectedReason)
    {
        Setup(kind, reason, "value");

        var detail = await _subject.ResolveStringValueAsync("flag-key", "fallback-value-on-error", Context);

        Assert.Equal("flag-key", detail.FlagKey);
        Assert.Equal("value", detail.Value);
        Assert.Equal(expectedReason, detail.Reason);

        Assert.Equal(ErrorType.None, detail.ErrorType);
        Assert.Null(detail.ErrorMessage);

        Assert.Equal("value", detail.Variant);
    }

    [Fact]
    public async Task ResolveDouble()
    {
        SetupValue("1.5");

        var detail = await _subject.ResolveDoubleValueAsync("flag-key", 0.5, Context);
        Assert.Equal(1.5, detail.Value);
    }

    [Fact]
    public async Task ResolveInteger()
    {
        SetupValue("1");

        var detail = await _subject.ResolveIntegerValueAsync("flag-key", -1, Context);
        Assert.Equal(1, detail.Value);
    }

    [Fact]
    public async Task ResolveStructureValue()
    {
        SetupValue("""["foo","bar"]""");

        var actual =
            await _subject.ResolveStructureValueAsync("flag-key", new Value("fallback-value-on-error"), Context);
        var expected = new Value([new Value("foo"), new Value("bar")]);

        Assert.Equal(expected, actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveTypeMismatchError()
    {
        SetupValue("abc");

        var detail = await _subject.ResolveDoubleValueAsync("flag-key", -1, Context);

        Assert.Equal("flag-key", detail.FlagKey);
        Assert.Equal(-1, detail.Value);
        Assert.Equal(Reason.Error, detail.Reason);

        Assert.Equal(ErrorType.TypeMismatch, detail.ErrorType);
        Assert.Equal("cannot convert value 'abc' to the desired type", detail.ErrorMessage);

        Assert.Null(detail.Variant);
    }

    private void Setup(ReasonKind kind, string reason, string value)
    {
        _fbClient
            .Setup(client => client.StringVariationDetail("flag-key", It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>("flag-key", kind, reason, value));
    }

    private void SetupValue(string value) => Setup(ReasonKind.Fallthrough, "fall through targets and rules", value);
}
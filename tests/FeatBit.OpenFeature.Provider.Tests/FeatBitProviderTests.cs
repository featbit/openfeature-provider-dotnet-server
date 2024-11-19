using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Evaluation;
using FeatBit.Sdk.Server.Model;

using Moq;

using OpenFeature.Constant;
using OpenFeature.Model;

using Xunit;

namespace FeatBit.OpenFeature.Provider.Tests;

public sealed class FeatBitProviderTests
{
    private const string FlagKey = "flag key";

    private readonly Mock<IFbClient> _fbClient;
    private readonly FeatBitProvider _subject;

    public FeatBitProviderTests()
    {
        _fbClient = new Mock<IFbClient>();
        _subject = new FeatBitProvider(_fbClient.Object);
    }

    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptions()
    {
        Assert.Throws<ArgumentNullException>(() => new FeatBitProvider(null!));
    }

    [Fact]
    public async Task EvaluationContextShouldBeConvertedToFbUser()
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, ReasonKind.Fallthrough, "", true));
        var context = EvaluationContext.Builder()
            .SetTargetingKey("k")
            .Set("name", "n")
            .Set("string", "s")
            .Set("bool", true)
            .Set("double", 1.5)
            .Set("int", 1)
            .Set("time", new Value(new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc)))
            .Set("list", new Value([new Value("a"), new Value("b")]))
            .Set("structure", new Value(new Structure(new Dictionary<string, Value> { { "x", new Value("y") } })))
            .Set("null", new Value())
            .Build();

        await _subject.ResolveBooleanValueAsync(FlagKey, false, context);

        _fbClient.Verify(fb => fb.BoolVariationDetail(
            FlagKey,
            It.Is<FbUser>(u =>
                u.Key == "k" &&
                u.Name == "n" &&
                u.Custom.Count == 7 &&
                u.Custom["string"] == "s" &&
                u.Custom["bool"] == "True" &&
                u.Custom["double"] == "1.5" &&
                u.Custom["int"] == "1" &&
                u.Custom["time"] == "2000-01-02T03:04:05.0000000Z" &&
                u.Custom["list"] == """["a","b"]""" &&
                u.Custom["structure"] == """{"x":"y"}"""),
            false), Times.Once);
    }

    [Fact]
    public void MetadataShouldContainName()
    {
        Assert.Equal("FeatBit Provider", _subject.GetMetadata().Name);
    }

    [Theory]
    [InlineData(ReasonKind.ClientNotReady, "", ErrorType.ProviderNotReady)]
    [InlineData(ReasonKind.WrongType, "", ErrorType.TypeMismatch)]
    [InlineData(ReasonKind.Error, "flag not found", ErrorType.FlagNotFound)]
    [InlineData(ReasonKind.Error, "", ErrorType.General)]
    public async Task ResolveBooleanValueAsyncShouldConvertFailureReasonsToErrorTypes(
        ReasonKind kind, string reason, ErrorType expected)
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, kind, reason, false));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, false);

        Assert.Equal(expected, actual.ErrorType);
    }

    [Theory]
    [InlineData(ReasonKind.Off, Reason.Disabled)]
    [InlineData(ReasonKind.Fallthrough, Reason.Default)]
    [InlineData(ReasonKind.TargetMatch, Reason.TargetingMatch)]
    [InlineData(ReasonKind.RuleMatch, Reason.TargetingMatch)]
    public async Task ResolveBooleanValueAsyncShouldConvertSuccessReasons(ReasonKind kind, string expected)
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, kind, "", false));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, false);

        Assert.Equal(expected, actual.Reason);
    }

    [Fact]
    public async Task ResolveBooleanValueAsyncShouldConvertUnknownReasonsToErrors()
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, (ReasonKind)(-1), "something went wrong", false));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, false);

        Assert.Multiple(() =>
        {
            Assert.Equal(FlagKey, actual.FlagKey);
            Assert.False(actual.Value);
            Assert.Equal(ErrorType.General, actual.ErrorType);
            Assert.Equal("Unknown reason kind: -1", actual.ErrorMessage);
            Assert.Equal(Reason.Unknown, actual.Reason);
            Assert.Null(actual.Variant);
            Assert.Null(actual.FlagMetadata);
        });
    }

    [Fact]
    public async Task ResolveBooleanValueAsyncShouldSetTheDetailPropertiesOnError()
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, ReasonKind.Error, "something went wrong", true));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, false);

        Assert.Multiple(() =>
        {
            Assert.Equal(FlagKey, actual.FlagKey);
            Assert.False(actual.Value);
            Assert.Equal(ErrorType.General, actual.ErrorType);
            Assert.Equal("something went wrong", actual.ErrorMessage);
            Assert.Equal("something went wrong", actual.Reason);
            Assert.Null(actual.Variant);
            Assert.Null(actual.FlagMetadata);
        });
    }

    [Fact]
    public async Task ResolveBooleanValueAsyncShouldSetTheDetailPropertiesOnSuccess()
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, ReasonKind.Fallthrough, "on with fall through", true));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, false);

        Assert.Multiple(() =>
        {
            Assert.Equal(FlagKey, actual.FlagKey);
            Assert.True(actual.Value);
            Assert.Equal(ErrorType.None, actual.ErrorType);
            Assert.Null(actual.ErrorMessage);
            Assert.Equal(Reason.Default, actual.Reason);
            Assert.Equal(bool.TrueString, actual.Variant);
            Assert.Null(actual.FlagMetadata);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveBooleanValueAsyncShouldUseTheDefaultValueOnError(bool defaultValue)
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), defaultValue))
            .Returns(new EvalDetail<bool>(FlagKey, ReasonKind.Error, "", false));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, defaultValue);

        Assert.Equal(defaultValue, actual.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveBooleanValueAsyncShouldUseTheReturnedValueOnSuccess(bool returnedValue)
    {
        _fbClient.Setup(fb => fb.BoolVariationDetail(FlagKey, It.IsAny<FbUser>(), false))
            .Returns(new EvalDetail<bool>(FlagKey, ReasonKind.Fallthrough, "", returnedValue));

        var actual = await _subject.ResolveBooleanValueAsync(FlagKey, false);

        Assert.Equal(returnedValue, actual.Value);
    }

    [Fact]
    public async Task ResolveDoubleValueAsyncShouldUseTheReturnedValueOnSuccess()
    {
        _fbClient.Setup(fb => fb.DoubleVariationDetail(FlagKey, It.IsAny<FbUser>(), 0.5))
            .Returns(new EvalDetail<double>(FlagKey, ReasonKind.Fallthrough, "", 1.5));

        var actual = await _subject.ResolveDoubleValueAsync(FlagKey, 0.5);

        Assert.Equal(1.5, actual.Value);
    }

    [Fact]
    public async Task ResolveIntegerValueAsyncShouldUseTheReturnedValueOnSuccess()
    {
        _fbClient.Setup(fb => fb.IntVariationDetail(FlagKey, It.IsAny<FbUser>(), 1))
            .Returns(new EvalDetail<int>(FlagKey, ReasonKind.Fallthrough, "", 2));

        var actual = await _subject.ResolveIntegerValueAsync(FlagKey, 1);

        Assert.Equal(2, actual.Value);
    }

    [Fact]
    public async Task ResolveStringValueAsyncShouldUseTheReturnedValueOnSuccess()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), "default"))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "returned"));

        var actual = await _subject.ResolveStringValueAsync(FlagKey, "default");

        Assert.Equal("returned", actual.Value);
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonArrays()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", """["foo","bar"]"""));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Equal(new Value([new Value("foo"), new Value("bar")]), actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonFalse()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "false"));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Equal(new Value(false), actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonNull()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "null"));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Equal(new Value(), actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonNumbers()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "0.5"));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Equal(new Value(0.5), actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonObjects()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", """{"foo":"bar"}"""));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        var expected = new Value(new Structure(new Dictionary<string, Value> { { "foo", new Value("bar") } }));
        Assert.Equal(expected, actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonStrings()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "\"returned\""));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Equal(new Value("returned"), actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldDeserializeJsonTrue()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "true"));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Equal(new Value(true), actual.Value, new ValueComparer());
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldReturnATypeMismatchErrorWhenJsonIsNotValid()
    {
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Fallthrough, "", "this is invalid json"));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, new Value("default"));

        Assert.Multiple(() =>
        {
            const string expectedReason = "'this is invalid json' is an invalid JSON literal.";
            Assert.Equal(FlagKey, actual.FlagKey);
            Assert.Equal(new Value("default"), actual.Value, new ValueComparer());
            Assert.Equal(ErrorType.TypeMismatch, actual.ErrorType);
            Assert.StartsWith(expectedReason, actual.ErrorMessage);
            Assert.StartsWith(expectedReason, actual.Reason);
            Assert.Null(actual.Variant);
            Assert.Null(actual.FlagMetadata);
        });
    }

    [Fact]
    public async Task ResolveStructureValueAsyncShouldUseTheDefaultValueOnError()
    {
        var defaultValue = new Value("default");
        _fbClient.Setup(fb => fb.StringVariationDetail(FlagKey, It.IsAny<FbUser>(), null))
            .Returns(new EvalDetail<string>(FlagKey, ReasonKind.Error, "", ""));

        var actual = await _subject.ResolveStructureValueAsync(FlagKey, defaultValue);

        Assert.Equal(defaultValue, actual.Value, new ValueComparer());
    }

    private sealed class ValueComparer : IEqualityComparer<Value>
    {
        public bool Equals(Value? a, Value? b)
        {
            if (a?.AsObject is null)
            {
                return b?.AsObject is null;
            }

            if (b?.AsObject is null)
            {
                return a.AsObject is null;
            }

            if (a.IsList && b.IsList)
            {
                return a.AsList!.SequenceEqual(b.AsList!, this);
            }

            if (a.IsStructure && b.IsStructure)
            {
                return Equals(a.AsStructure!, b.AsStructure!);
            }

            return a.AsObject.Equals(b.AsObject);
        }

        public int GetHashCode(Value obj)
        {
            throw new NotSupportedException();
        }

        private bool Equals(Structure a, Structure b)
        {
            var aDict = a.AsDictionary();
            var bDict = b.AsDictionary();

            return aDict.Count == bDict.Count &&
                   aDict.All(pair => bDict.TryGetValue(pair.Key, out var bValue) && Equals(pair.Value, bValue));
        }
    }
}

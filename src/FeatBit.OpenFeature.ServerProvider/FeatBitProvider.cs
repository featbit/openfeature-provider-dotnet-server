using FeatBit.Sdk.Server;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace FeatBit.OpenFeature.ServerProvider;

public sealed class FeatBitProvider : FeatureProvider
{
    private static readonly Metadata Metadata = new("FeatBit.OpenFeature.ServerProvider");

    private readonly IFbClient _client;

    public FeatBitProvider(IFbClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public override Metadata GetMetadata()
    {
        return Metadata;
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResolveCore(flagKey, defaultValue, context, ValueConverters.Bool));

    /// <inheritdoc />
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResolveCore(flagKey, defaultValue, context, ValueConverters.Double));

    /// <inheritdoc />
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResolveCore(flagKey, defaultValue, context, ValueConverters.Int));

    /// <inheritdoc />
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResolveCore(flagKey, defaultValue, context, ValueConverters.String));

    /// <inheritdoc />
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ResolveCore(flagKey, defaultValue, context, ValueConverters.Value));

    private ResolutionDetails<TValue> ResolveCore<TValue>(
        string flagKey,
        TValue defaultValue,
        EvaluationContext? context,
        ValueConverter<TValue> converter)
    {
        var user = context.AsFbUser();
        var detail = _client.StringVariationDetail(flagKey, user, null);

        // if fbClient returns error
        if (detail.IsError())
        {
            return new ResolutionDetails<TValue>(
                flagKey: flagKey,
                value: defaultValue,
                reason: detail.GetOpenFeatureReason(),
                errorType: detail.GetErrorType(),
                errorMessage: detail.Reason
            );
        }

        // if failed to convert value
        if (!converter(detail.Value, out var converted))
        {
            return new ResolutionDetails<TValue>(
                flagKey: flagKey,
                value: defaultValue,
                reason: Reason.Error,
                errorType: ErrorType.TypeMismatch,
                errorMessage: $"cannot convert value '{detail.Value}' to the desired type"
            );
        }

        return new ResolutionDetails<TValue>(
            flagKey: flagKey,
            value: converted,
            reason: detail.GetOpenFeatureReason(),
            errorType: ErrorType.None,
            errorMessage: null,
            // FeatBit does not return the variation name, so use stringified value as OpenTelemetry suggests
            // https://opentelemetry.io/docs/specs/semconv/feature-flags/feature-flags-spans/
            variant: detail.Value
        );
    }
}
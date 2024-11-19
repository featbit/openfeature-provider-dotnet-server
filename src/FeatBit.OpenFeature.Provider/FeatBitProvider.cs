using FeatBit.Sdk.Server;

using OpenFeature;
using OpenFeature.Model;

namespace FeatBit.OpenFeature.Provider;

public sealed class FeatBitProvider : FeatureProvider
{
    private static readonly Metadata Metadata = new("FeatBit Provider");

    private readonly IFbClient _client;

    public FeatBitProvider(IFbClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override Metadata GetMetadata()
    {
        return Metadata;
    }

    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
        EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var detail = _client.BoolVariationDetail(flagKey, context.ToFbUser(), defaultValue);
        return Task.FromResult(detail.ToResolutionDetails(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var detail = _client.DoubleVariationDetail(flagKey, context.ToFbUser(), defaultValue);
        return Task.FromResult(detail.ToResolutionDetails(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
        EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var detail = _client.IntVariationDetail(flagKey, context.ToFbUser(), defaultValue);
        return Task.FromResult(detail.ToResolutionDetails(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue,
        EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var detail = _client.StringVariationDetail(flagKey, context.ToFbUser(), defaultValue);
        return Task.FromResult(detail.ToResolutionDetails(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var detail = _client.StringVariationDetail(flagKey, context.ToFbUser(), null);
        return Task.FromResult(detail.ToResolutionDetails(flagKey, defaultValue, JsonConversions.DeserializeValue));
    }
}

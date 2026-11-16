using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Options;
using OpenFeature.Constant;
using OpenFeature.Model;
using Xunit;

namespace FeatBit.OpenFeature.ServerProvider.Tests;

public sealed class TargetingKeyCompatibilityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("missing")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("user-key")]
    public async Task EvaluatesFlagWithOptionalTargetingKey(string? targetingKey)
    {
        const string bootstrap = """
            {
              "messageType": "data-sync",
              "data": {
                "eventType": "full",
                "featureFlags": [{
                  "id": "174c7138-426d-4434-8a91-af8c00306de3", "key": "test-flag", "isEnabled": false,
                  "variationType": "boolean",
                  "variations": [{ "id": "on", "value": "true" }],
                  "disabledVariationId": "on",
                  "targetUsers": [], "rules": []
                }],
                "segments": []
              }
            }
            """;
        var client = new FbClient(new FbOptionsBuilder()
            .Offline(true)
            .UseJsonBootstrapProvider(bootstrap)
            .Build());
        try
        {
            var context = targetingKey switch
            {
                null => null,
                "missing" => EvaluationContext.Builder().Build(),
                _ => EvaluationContext.Builder().SetTargetingKey(targetingKey).Build()
            };
            var provider = new FeatBitProvider(client);
            var result = await provider.ResolveBooleanValueAsync("test-flag", false, context);

            Assert.True(result.Value);
            Assert.Equal(ErrorType.None, result.ErrorType);
            Assert.Equal(Reason.Disabled, result.Reason);
        }
        finally
        {
            await client.CloseAsync();
        }
    }
}

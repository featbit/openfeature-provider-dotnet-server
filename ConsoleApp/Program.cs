using FeatBit.OpenFeature.ServerProvider;
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Options;
using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Model;

// setup SDK options
var consoleLoggerFactory = LoggerFactory.Create(opt => opt.AddConsole().SetMinimumLevel(LogLevel.Debug));
var options = new FbOptionsBuilder("Gg81S_N-HEybgVTcA6xnpQpb52_tWTZkaegJQvR5WOuw")
    .Event(new Uri("http://localhost:5100"))
    .Streaming(new Uri("ws://localhost:5100"))
    .LoggerFactory(consoleLoggerFactory)
    .Build();

// Creates a new client instance that connects to FeatBit with the custom option.
var fbClient = new FbClient(options);
if (!fbClient.Initialized)
{
    Console.WriteLine("Failed to initialize FeatBit client. Change log level to LogLevel.Debug to see more details.");
    return;
}

// use the FeatBit client with OpenFeature
var provider = new FeatBitProvider(fbClient);
await Api.Instance.SetProviderAsync(provider);
var client = Api.Instance.GetClient();

// flag to be evaluated
const string flagKey = "game-runner";

// create an evaluation context
var context = EvaluationContext.Builder().SetTargetingKey("anonymous").Build();

// evaluate a boolean flag for a given context
var boolVariation = await client.GetBooleanValueAsync(flagKey, defaultValue: false, context);
Console.WriteLine($"flag '{flagKey}' returns {boolVariation} for {context.TargetingKey}");

// evaluate a boolean flag for a given context with evaluation detail
var boolVariationDetail = await client.GetBooleanDetailsAsync(flagKey, defaultValue: false, context);
Console.WriteLine(
    $"flag '{flagKey}' returns {boolVariationDetail.Value} for {context.TargetingKey}. " +
    $"Reason: {boolVariationDetail.Reason}"
);

// shut down OpenFeature
await Api.Instance.ShutdownAsync();

// close the client to ensure that all insights are sent out before the app exits
await fbClient.CloseAsync();
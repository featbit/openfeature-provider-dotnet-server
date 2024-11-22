# FeatBit OpenFeature Provider for .NET Server-Side SDK

## Introduction

This is the [OpenFeature](https://github.com/open-feature/dotnet-sdk) provider for the
[FeatBit .NET Server SDK](https://github.com/featbit/featbit-dotnet-sdk), for the 100% open-source feature flags
management platform [FeatBit](https://github.com/featbit/featbit).

## Get Started

### Installation

The latest stable version is available on [NuGet](https://www.nuget.org/packages/FeatBit.OpenFeature.ServerProvider/).

```sh
dotnet add package FeatBit.OpenFeature.ServerProvider
```

### Prerequisite

Before using the SDK, you need to obtain the environment secret and SDK URLs.

Follow the documentation below to retrieve these values

- [How to get the environment secret](https://docs.featbit.co/sdk/faq#how-to-get-the-environment-secret)
- [How to get the SDK URLs](https://docs.featbit.co/sdk/faq#how-to-get-the-sdk-urls)

### Quick Start

The following code demonstrates basic usage of FeatBit.OpenFeature.ServerProvider.

```cs
using FeatBit.OpenFeature.ServerProvider;
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Options;
using OpenFeature;
using OpenFeature.Model;

// setup SDK options
var options = new FbOptionsBuilder("<replace-with-your-env-secret>")
    .Event(new Uri("<replace-with-your-event-url>"))
    .Streaming(new Uri("<replace-with-your-streaming-url>"))
    .Build();

// Creates a new client instance that connects to FeatBit with the custom option.
var fbClient = new FbClient(options);

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
```

## Getting support

- If you have a specific question about using this sdk, we encourage you
  to [ask it in our slack](https://join.slack.com/t/featbit/shared_invite/zt-1ew5e2vbb-x6Apan1xZOaYMnFzqZkGNQ).
- If you encounter a bug or would like to request a
  feature, [submit an issue](https://github.com/featbit/openfeature-provider-dotnet-server/issues/new).

using OpenFeature.Model;
using Xunit;

namespace FeatBit.OpenFeature.ServerProvider.Tests;

public class JsonConverterTests
{
    [Fact]
    public void DeserializeStructuredValue()
    {
        List<KeyValuePair<string, Value>> pairs =
        [
            new("""["foo","bar"]""", new Value([new Value("foo"), new Value("bar")])),
            new("""{"foo":"bar"}""", new Value(new Structure(new Dictionary<string, Value> { { "foo", new Value("bar") } }))),
            new("true", new Value(true)),
            new("false", new Value(false)),
            new("null", new Value()),
            new("1", new Value(1)),
            new("1.5", new Value(1.5)),
            new("\"foo\"", new Value("foo")),
            new(null!, new Value())
        ];

        foreach (var pair in pairs)
        {
            var convertSuccess = JsonConverters.TryDeserializeValue(pair.Key, out var actual);

            Assert.True(convertSuccess);
            Assert.Equal(pair.Value, actual, new ValueComparer());
        }
    }
}
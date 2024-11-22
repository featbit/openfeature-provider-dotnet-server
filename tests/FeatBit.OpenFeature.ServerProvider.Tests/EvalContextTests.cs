using OpenFeature.Model;
using Xunit;

namespace FeatBit.OpenFeature.ServerProvider.Tests;

public class EvalContextTests
{
    [Fact]
    public void ContextToUser()
    {
        var ctx = EvaluationContext.Builder()
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

        var user = ctx.AsFbUser();

        Assert.Equal("k", user.Key);
        Assert.Equal("n", user.Name);
        Assert.Equal(7, user.Custom.Count);
        Assert.Equal("s", user.ValueOf("string"));
        Assert.Equal("True", user.ValueOf("bool"));
        Assert.Equal("1.5", user.ValueOf("double"));
        Assert.Equal("1", user.ValueOf("int"));
        Assert.Equal("2000-01-02T03:04:05.0000000Z", user.ValueOf("time"));
        Assert.Equal("""["a","b"]""", user.ValueOf("list"));
        Assert.Equal("""{"x":"y"}""", user.ValueOf("structure"));
    }
}
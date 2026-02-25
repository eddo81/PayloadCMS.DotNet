namespace Payload.CMS.Tests;

public class QueryStringEncoderTests
{
    // addQueryPrefix: false mirrors the TS test setup
    private readonly QueryStringEncoder _encoder = new(addQueryPrefix: false);

    [Fact]
    public void ShouldSerializeFlatObject()
    {
        var obj = new Dictionary<string, object?> { ["limit"] = 10, ["page"] = 2 };
        var queryString = TestHelpers.Normalize(_encoder.Stringify(obj));
        var expected = TestHelpers.Normalize("limit=10&page=2");

        Assert.Equal(expected, queryString);
    }

    [Fact]
    public void ShouldEncodeNestedObjects()
    {
        var obj = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?> { ["key"] = "value" }
        };
        var queryString = _encoder.Stringify(obj);

        Assert.Equal("nested[key]=value", queryString);
    }

    [Fact]
    public void ShouldEncodeArraysWithIndices()
    {
        var obj = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { "a", "b" }
        };
        var queryString = TestHelpers.Normalize(_encoder.Stringify(obj));
        var expected = TestHelpers.Normalize("items[0]=a&items[1]=b");

        Assert.Equal(expected, queryString);
    }

    [Fact]
    public void ShouldEncodeSpecialCharactersInKeysAndValues()
    {
        var obj = new Dictionary<string, object?> { ["spaced key"] = "hello world" };
        var queryString = _encoder.Stringify(obj);

        Assert.Equal("spaced%20key=hello%20world", queryString);
    }

    [Fact]
    public void ShouldEncodeDateValuesAsIsoStrings()
    {
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(obj);

        // C# "O" format: 2024-01-01T12:00:00.0000000Z  — colons become %3A
        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.0000000Z", queryString);
    }

    [Fact]
    public void ShouldSkipNullValues()
    {
        var obj = new Dictionary<string, object?>
        {
            ["keep"] = "yes",
            ["skip"] = null
        };
        var queryString = _encoder.Stringify(obj);

        Assert.Equal("keep=yes", queryString);
    }

    [Fact]
    public void ShouldSkipUnsupportedTypes()
    {
        // In C# the unsupported-type equivalents are anything not in the
        // _isPrimitive set (string, int, long, double, float, decimal, bool, DateTime).
        // Guid and anonymous objects are skipped.
        var obj = new Dictionary<string, object?>
        {
            ["ok"] = "fine",
            ["nope"] = Guid.NewGuid()
        };
        var queryString = _encoder.Stringify(obj);

        Assert.Equal("ok=fine", queryString);
    }

    [Fact]
    public void ShouldReturnEmptyStringForEmptyObject()
    {
        var obj = new Dictionary<string, object?>();
        var queryString = _encoder.Stringify(obj);

        Assert.Equal("", queryString);
    }
}

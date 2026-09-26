using System.Globalization;

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

        // 2024-01-01T12:00:00.000Z — colons become %3A
        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldTreatDatesWithNoKindAsUtc()
    {
        // DateTimeKind.Unspecified — the default for a DateTime built this way.
        var date = new DateTime(2024, 1, 1, 12, 0, 0);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(obj);

        // The reading is kept as written and labelled UTC — no timezone conversion applies.
        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldConvertLocalDatesToUtc()
    {
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(obj);

        // A literal expectation here would depend on the machine's timezone and break on a UTC
        // build agent, so assert the instant instead: whatever was emitted must reparse to the
        // same moment the caller supplied.
        var emitted = Uri.UnescapeDataString(queryString.Replace("createdAt=", ""));
        var reparsed = DateTime.Parse(emitted, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        Assert.EndsWith("Z", emitted);
        Assert.Equal(date.ToUniversalTime(), reparsed);
    }

    [Fact]
    public void ShouldTruncateRatherThanRoundSubMillisecondDates()
    {
        // 0.9999 of a millisecond past the second: truncation gives .000, rounding would give .001.
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(9999);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(obj);

        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldEncodeDatesIndependentlyOfTheAmbientCulture()
    {
        // Under fi-FI the time separator is "." rather than ":", so a date formatted without an
        // explicit culture yields 2024-01-01T12.00.00.000Z.
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fi-FI");

            var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var obj = new Dictionary<string, object?> { ["createdAt"] = date };
            var queryString = _encoder.Stringify(obj);

            Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
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

    [Fact]
    public void ShouldEncodeDecimalNumbersInvariantOfCulture()
    {
        // sv-SE renders 3.14 as "3,14" via ToString() — the encoder must stay invariant
        // because "," is deliberately left unescaped in Payload query strings.
        var originalCulture = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");

            var obj = new Dictionary<string, object?>
            {
                ["price"] = 3.14,
                ["ratio"] = 0.5f,
                ["amount"] = 19.95m
            };
            var queryString = TestHelpers.Normalize(_encoder.Stringify(obj));
            var expected = TestHelpers.Normalize("price=3.14&ratio=0.5&amount=19.95");

            Assert.Equal(expected, queryString);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
}

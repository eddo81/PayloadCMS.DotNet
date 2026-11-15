using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Payload.CMS.Tests;

public class QueryStringEncoderTests
{
    // addQueryPrefix: false mirrors the TS test setup
    private readonly QueryStringEncoder _encoder = new(addQueryPrefix: false);

    // The expectations in the strict-encoding tests below are the literal output of `qs-esm` 7.0.2
    // — the package Payload itself recommends — captured by running `stringify` on the same input.
    private readonly QueryStringEncoder _strictEncoder = new(addQueryPrefix: false, strictEncoding: true);

    // The SDK hands the encoder a JSON tree made by the platform serializer; the tests build it the
    // same way, so each expectation covers the conversion and the encoding together.
    private static JsonObject Tree(Dictionary<string, object?> obj)
    {
        return obj.ToNode()!.AsObject();
    }

    [Fact]
    public void ShouldSerializeFlatObject()
    {
        var obj = new Dictionary<string, object?> { ["limit"] = 10, ["page"] = 2 };
        var queryString = TestHelpers.Normalize(_encoder.Stringify(Tree(obj)));
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
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("nested[key]=value", queryString);
    }

    [Fact]
    public void ShouldEncodeArraysWithIndices()
    {
        var obj = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { "a", "b" }
        };
        var queryString = TestHelpers.Normalize(_encoder.Stringify(Tree(obj)));
        var expected = TestHelpers.Normalize("items[0]=a&items[1]=b");

        Assert.Equal(expected, queryString);
    }

    [Fact]
    public void ShouldEncodeSpecialCharactersInKeysAndValues()
    {
        var obj = new Dictionary<string, object?> { ["spaced key"] = "hello world" };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("spaced%20key=hello%20world", queryString);
    }

    [Fact]
    public void ShouldEncodeDateValuesAsIsoStrings()
    {
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(Tree(obj));

        // 2024-01-01T12:00:00.000Z — colons become %3A
        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldTreatDatesWithNoKindAsUtc()
    {
        // DateTimeKind.Unspecified — the default for a DateTime built this way.
        var date = new DateTime(2024, 1, 1, 12, 0, 0);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(Tree(obj));

        // The reading is kept as written and labelled UTC — no timezone conversion applies.
        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldConvertLocalDatesToUtc()
    {
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var obj = new Dictionary<string, object?> { ["createdAt"] = date };
        var queryString = _encoder.Stringify(Tree(obj));

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
        var queryString = _encoder.Stringify(Tree(obj));

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
            var queryString = _encoder.Stringify(Tree(obj));

            Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ShouldRenderNullAsAnEmptyValue()
    {
        var obj = new Dictionary<string, object?>
        {
            ["keep"] = "yes",
            ["empty"] = null
        };
        var queryString = _encoder.Stringify(Tree(obj));

        // The key survives. Dropping it would remove the filter and widen the result set.
        Assert.Equal("keep=yes&empty=", queryString);
    }

    [Fact]
    public void ShouldRenderNestedNullAsAnEmptyValue()
    {
        var obj = new Dictionary<string, object?>
        {
            ["where"] = new Dictionary<string, object?>
            {
                ["views"] = new Dictionary<string, object?> { ["equals"] = null }
            }
        };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("where[views][equals]=", queryString);
    }

    [Fact]
    public void ShouldRenderNullArrayElementsAsEmptyValues()
    {
        var obj = new Dictionary<string, object?>
        {
            ["ids"] = new List<object?> { null, 1 }
        };
        var queryString = _encoder.Stringify(Tree(obj));

        // The index must stay aligned with the caller's list, so the empty element is emitted
        // rather than collapsed away.
        Assert.Equal("ids[0]=&ids[1]=1", queryString);
    }

    [Theory]
    [MemberData(nameof(PlatformMappedValues))]
    public void ShouldEncodeOtherValuesAsTheSerializerWritesThem(object value, string expected)
    {
        var obj = new Dictionary<string, object?>
        {
            ["where"] = new Dictionary<string, object?> { ["field"] = value }
        };

        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal(expected, queryString);
    }

    public static TheoryData<object, string> PlatformMappedValues => new()
    {
        { new TimeOnly(12, 30, 45), "where[field]=12%3A30%3A45" },
        { TimeSpan.FromMinutes(90), "where[field]=01%3A30%3A00" },
        { DayOfWeek.Sunday, "where[field]=0" },
        { new { nested = 1 }, "where[field][nested]=1" },
        { new GeoPoint(12, 55), "where[field][Latitude]=12&where[field][Longitude]=55" },
        { new HashSet<int> { 1 }, "where[field][0]=1" },
        { Enumerable.Range(1, 2).Select(number => number), "where[field][0]=1&where[field][1]=2" },
        { new Uri("https://example.com"), "where[field]=https%3A%2F%2Fexample.com" },
        { new Dictionary<int, string> { [1] = "a" }, "where[field][1]=a" },
    };

    private sealed record GeoPoint(double Latitude, double Longitude);

    [Theory]
    [MemberData(nameof(NumericValues))]
    public void ShouldEncodeEveryNumericType(object value, string expected)
    {
        var obj = new Dictionary<string, object?> { ["n"] = value };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal($"n={expected}", queryString);
    }

    public static TheoryData<object, string> NumericValues => new()
    {
        { (sbyte)-5, "-5" },
        { (byte)5, "5" },
        { (short)-10, "-10" },
        { (ushort)10, "10" },
        { (uint)42, "42" },
        { ulong.MaxValue, "18446744073709551615" },
        { Int128.MaxValue, "170141183460469231731687303715884105727" },
        { (UInt128)7, "7" },
        { System.Numerics.BigInteger.Parse("123456789012345678901234567890"), "123456789012345678901234567890" },
        { (Half)1.5, "1.5" },
    };

    [Fact]
    public void ShouldEncodeStringKeyedDictionariesOfAnyValueType()
    {
        var obj = new Dictionary<string, object?>
        {
            ["where"] = new Dictionary<string, Dictionary<string, int>>
            {
                ["views"] = new Dictionary<string, int> { ["greater_than"] = 10 }
            }
        };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("where[views][greater_than]=10", queryString);
    }

    [Fact]
    public void ShouldEncodeTypedArrays()
    {
        var obj = new Dictionary<string, object?> { ["ids"] = new[] { 1, 2 } };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("ids[0]=1&ids[1]=2", queryString);
    }

    [Fact]
    public void ShouldEncodeAnyValueInsideAList()
    {
        var obj = new Dictionary<string, object?>
        {
            ["ids"] = new List<object?> { 1, TimeSpan.Zero }
        };

        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("ids[0]=1&ids[1]=00%3A00%3A00", queryString);
    }

    [Fact]
    public void ShouldRenderAJsonElementNullAsAnEmptyValue()
    {
        // A null from any source, here a JsonElement holding null, becomes an empty value.
        var obj = new Dictionary<string, object?>
        {
            ["a"] = JsonDocument.Parse("null").RootElement,
            ["list"] = new List<object?> { JsonDocument.Parse("null").RootElement },
        };

        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("a=&list[0]=", queryString);
    }

    [Fact]
    public void ShouldEncodeASharedValueTwice()
    {
        var shared = new Dictionary<string, object?> { ["equals"] = 1 };
        var obj = new Dictionary<string, object?> { ["a"] = shared, ["b"] = shared };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("a[equals]=1&b[equals]=1", queryString);
    }

    [Fact]
    public void ShouldEncodeGuidValues()
    {
        var obj = new Dictionary<string, object?>
        {
            ["id"] = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301")
        };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("id=3f2504e0-4f89-11d3-9a0c-0305e82c3301", queryString);
    }

    [Fact]
    public void ShouldEncodeCharValues()
    {
        var obj = new Dictionary<string, object?> { ["grade"] = 'A' };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("grade=A", queryString);
    }

    [Fact]
    public void ShouldEncodeDateTimeOffsetValuesAsUtc()
    {
        var value = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
        var obj = new Dictionary<string, object?> { ["createdAt"] = value };
        var queryString = _encoder.Stringify(Tree(obj));

        // The offset is resolved, so 12:00 at +02:00 becomes 10:00Z.
        Assert.Equal("createdAt=2024-01-01T10%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldEncodeDateTimeOffsetWithZeroOffsetUsingZ()
    {
        var value = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var obj = new Dictionary<string, object?> { ["createdAt"] = value };
        var queryString = _encoder.Stringify(Tree(obj));

        // Formatting the value directly would yield "+00:00" here rather than "Z".
        Assert.Equal("createdAt=2024-01-01T12%3A00%3A00.000Z", queryString);
    }

    [Fact]
    public void ShouldEncodeDateOnlyValues()
    {
        var obj = new Dictionary<string, object?> { ["publishedOn"] = new DateOnly(2024, 1, 1) };
        var queryString = _encoder.Stringify(Tree(obj));

        Assert.Equal("publishedOn=2024-01-01", queryString);
    }

    [Fact]
    public void ShouldPercentEncodeStructuralBracketsInStrictMode()
    {
        var obj = new Dictionary<string, object?>
        {
            ["where"] = new Dictionary<string, object?>
            {
                ["title"] = new Dictionary<string, object?> { ["equals"] = "foo" }
            }
        };

        // The brackets the encoder generates itself, not ones that arrived in the data.
        Assert.Equal("where%5Btitle%5D%5Bequals%5D=foo", _strictEncoder.Stringify(Tree(obj)));
        Assert.Equal("where[title][equals]=foo", _encoder.Stringify(Tree(obj)));
    }

    [Fact]
    public void ShouldPercentEncodeArrayIndexBracketsInStrictMode()
    {
        var obj = new Dictionary<string, object?>
        {
            ["where"] = new Dictionary<string, object?>
            {
                ["or"] = new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        ["title"] = new Dictionary<string, object?> { ["equals"] = "foo" }
                    }
                }
            }
        };

        Assert.Equal("where%5Bor%5D%5B0%5D%5Btitle%5D%5Bequals%5D=foo", _strictEncoder.Stringify(Tree(obj)));
        Assert.Equal("where[or][0][title][equals]=foo", _encoder.Stringify(Tree(obj)));
    }

    [Fact]
    public void ShouldPercentEncodeCommasInStrictMode()
    {
        var obj = new Dictionary<string, object?> { ["sort"] = "a,-b" };

        // A comma that arrived in the data rather than one the encoder generated.
        Assert.Equal("sort=a%2C-b", _strictEncoder.Stringify(Tree(obj)));
        Assert.Equal("sort=a,-b", _encoder.Stringify(Tree(obj)));
    }

    [Fact]
    public void ShouldReturnEmptyStringForEmptyObject()
    {
        var obj = new Dictionary<string, object?>();
        var queryString = _encoder.Stringify(Tree(obj));

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
            var queryString = TestHelpers.Normalize(_encoder.Stringify(Tree(obj)));
            var expected = TestHelpers.Normalize("price=3.14&ratio=0.5&amount=19.95");

            Assert.Equal(expected, queryString);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
}

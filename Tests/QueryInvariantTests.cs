using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using PayloadCMS.DotNet.Enums;
using PayloadCMS.DotNet.Extensions;
using PayloadCMS.DotNet.Query;

namespace Payload.CMS.Tests;

/// <summary>
/// Invariants of query building that hold whatever the builders use internally. Each test reads
/// its result through the request URL the SDK sends, or through the JSON text of a built query, so
/// it does not depend on the type <c>Build()</c> returns.
/// </summary>
public class QueryInvariantTests
{
    private const string EmptyPage = """{ "docs": [] }""";

    private static async Task<string> SentQuery(QueryBuilder query)
    {
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, EmptyPage);

        await sdk.Find("posts", query);

        var url = handler.LastRequest!.RequestUri!.OriginalString;

        return url.Substring(url.IndexOf('?'));
    }

    private static QueryBuilder SampleQuery()
    {
        return new QueryBuilder()
            .Limit(10)
            .Select(new[] { "title", "meta.description" })
            .Populate("users", new[] { "name" })
            .Where(where => where
                .Field("title", Operator.Equals, "Hello")
                .Field("views", Operator.In, new List<object?> { 1, 2 })
                .Or(alternative => alternative.Field("featured", Operator.Equals, true)))
            .Join(join => join
                .Limit("comments", 5)
                .Where("comments", where => where.Field("approved", Operator.Equals, true)))
            .AddCustomParam("ids", new List<object?> { 1, 2 });
    }

    // ── Build() ─────────────────────────────────────────────────

    [Fact]
    public void Build_CalledTwice_ReturnsEqualResults()
    {
        var query = SampleQuery();

        var first = JsonSerializer.Serialize(query.Build());
        var second = JsonSerializer.Serialize(query.Build());

        Assert.Equal(first, second);
    }

    [Fact]
    public void Build_LaterBuilderCalls_DoNotChangeAnEarlierResult()
    {
        var query = SampleQuery();
        var earlier = query.Build();
        var earlierText = JsonSerializer.Serialize(earlier);

        query
            .Limit(99)
            .Where(where => where.Field("title", Operator.Equals, "Changed"))
            .Select(new[] { "title" });

        Assert.Equal(earlierText, JsonSerializer.Serialize(earlier));
    }

    [Fact]
    public void Build_ChangingAResult_DoesNotChangeTheBuilder()
    {
        var query = SampleQuery();
        var expected = JsonSerializer.Serialize(query.Build());
        var result = query.Build();

        result["limit"] = 99;
        result.Remove("where");

        Assert.Equal(expected, JsonSerializer.Serialize(query.Build()));
    }

    [Fact]
    public void Build_ChangingANestedPartOfAResult_DoesNotChangeTheBuilder()
    {
        var query = SampleQuery();
        var expected = JsonSerializer.Serialize(query.Build());
        var result = query.Build();

        result["where"]!["views"]!["in"]!.AsArray().Add(3);

        Assert.Equal(expected, JsonSerializer.Serialize(query.Build()));
    }

    // ── Values are read when they are passed ────────────────────

    [Fact]
    public async Task Field_ValueChangedByTheCallerAfterwards_DoesNotChangeTheQuery()
    {
        var tags = new List<object?> { "a" };
        var query = new QueryBuilder().Where(where => where.Field("tags", Operator.In, tags));

        tags.Add("b");

        Assert.Equal("?where%5Btags%5D%5Bin%5D%5B0%5D=a", await SentQuery(query));
    }

    [Fact]
    public void AddCustomParam_ValueChangedByTheCallerAfterwards_DoesNotChangeTheQuery()
    {
        var values = new List<object?> { 1 };
        var query = new QueryBuilder().AddCustomParam("ids", values);
        var expected = JsonSerializer.Serialize(query.Build());

        values.Add(2);

        Assert.Equal(expected, JsonSerializer.Serialize(query.Build()));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Field_WithANumberJsonCannotRepresent_ThrowsWhenPassed(double value)
    {
        var where = new WhereBuilder();

        Assert.Throws<ArgumentException>(() => where.Field("views", Operator.Equals, value));
    }

    [Fact]
    public void Field_WithAValueThatContainsItself_ThrowsWhenPassed()
    {
        var cyclic = new Dictionary<string, object?>();
        cyclic["self"] = cyclic;
        var where = new WhereBuilder();

        Assert.Throws<JsonException>(() => where.Field("data", Operator.Equals, cyclic));
    }

    // ── Reuse ───────────────────────────────────────────────────

    [Fact]
    public async Task Find_ReusingAQueryBuilder_SendsTheSameQueryEachTime()
    {
        var query = SampleQuery();

        var first = await SentQuery(query);
        var second = await SentQuery(query);

        Assert.Equal(first, second);
    }

    // ── Joins ───────────────────────────────────────────────────

    [Fact]
    public async Task Join_Disable_WinsRegardlessOfCallOrder()
    {
        var disabledFirst = await SentQuery(new QueryBuilder()
            .Join(join => join.Disable("comments").Limit("comments", 5)));
        var disabledLast = await SentQuery(new QueryBuilder()
            .Join(join => join.Limit("comments", 5).Disable("comments")));

        Assert.Equal("?joins%5Bcomments%5D=false", disabledFirst);
        Assert.Equal(disabledFirst, disabledLast);
    }

    // ── Wire output ─────────────────────────────────────────────

    [Fact]
    public async Task Find_WithEverySupportedValueAndParameter_SendsTheRecordedQuery()
    {
        var polygon = new Dictionary<string, object?>
        {
            ["type"] = "Polygon",
            ["coordinates"] = new List<object?>
            {
                new List<object?>
                {
                    new List<object?> { 12, 55 },
                    new List<object?> { 13, 55 },
                    new List<object?> { 12, 56 },
                    new List<object?> { 12, 55 },
                },
            },
        };

        var query = new QueryBuilder()
            .Limit(10)
            .Page(2)
            .Pagination(false)
            .Sort("title")
            .SortByDescending("views")
            .Depth(1)
            .Locale("sv")
            .FallbackLocale("en")
            .Select(new[] { "title", "meta.description" })
            .Populate("users", new[] { "name" })
            .Where(where => where
                .Field("title", Operator.Equals, "Hello & ü, [x]")
                .Field("views", Operator.GreaterThan, (short)10)
                .Field("views", Operator.LessThan, 42L)
                .Field("rating", Operator.LessThan, 3.14)
                .Field("price", Operator.Equals, 3.10m)
                .Field("ratio", Operator.Equals, 0.5f)
                .Field("big", Operator.Equals, BigInteger.Parse("12345678901234567890", CultureInfo.InvariantCulture))
                .Field("featured", Operator.Equals, true)
                .Field("publishedAt", Operator.GreaterThan, new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc))
                .Field("updatedAt", Operator.LessThan, new DateTimeOffset(2026, 3, 15, 12, 30, 0, TimeSpan.FromHours(2)))
                .Field("day", Operator.Equals, new DateOnly(2026, 3, 1))
                .Field("ref", Operator.Equals, Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"))
                .Field("initial", Operator.Equals, 'A')
                .Field("deletedAt", Operator.Equals, null)
                .Field("tags", Operator.In, new List<object?> { "a", "b", null })
                .Field("location", Operator.Within, polygon)
                .And(group => group.Field("status", Operator.Equals, "published"))
                .Or(alternative => alternative.Field("author", Operator.Exists, true))
                .Or(alternative => alternative.Field("editor", Operator.Exists, false)))
            .Join(join => join
                .Limit("comments", 5)
                .Sort("comments", "createdAt")
                .Count("comments")
                .Where("comments", where => where.Field("approved", Operator.Equals, true)))
            .Draft(true);

        var sent = await SentQuery(query);

        var expected = "?" + string.Join("&", new[]
        {
            "limit=10",
            "page=2",
            "pagination=false",
            "sort=title%2C-views",
            "depth=1",
            "locale=sv",
            "fallback-locale=en",
            "select%5Btitle%5D=true",
            "select%5Bmeta%5D%5Bdescription%5D=true",
            "populate%5Busers%5D%5Bname%5D=true",
            "where%5Btitle%5D%5Bequals%5D=Hello%20%26%20%C3%BC%2C%20%5Bx%5D",
            "where%5Bviews%5D%5Bgreater_than%5D=10",
            "where%5Bviews%5D%5Bless_than%5D=42",
            "where%5Brating%5D%5Bless_than%5D=3.14",
            "where%5Bprice%5D%5Bequals%5D=3.10",
            "where%5Bratio%5D%5Bequals%5D=0.5",
            "where%5Bbig%5D%5Bequals%5D=12345678901234567890",
            "where%5Bfeatured%5D%5Bequals%5D=true",
            "where%5BpublishedAt%5D%5Bgreater_than%5D=2026-03-15T10%3A30%3A00.000Z",
            "where%5BupdatedAt%5D%5Bless_than%5D=2026-03-15T10%3A30%3A00.000Z",
            "where%5Bday%5D%5Bequals%5D=2026-03-01",
            "where%5Bref%5D%5Bequals%5D=0f8fad5b-d9cb-469f-a165-70867728950e",
            "where%5Binitial%5D%5Bequals%5D=A",
            "where%5BdeletedAt%5D%5Bequals%5D=",
            "where%5Btags%5D%5Bin%5D%5B0%5D=a",
            "where%5Btags%5D%5Bin%5D%5B1%5D=b",
            "where%5Btags%5D%5Bin%5D%5B2%5D=",
            "where%5Blocation%5D%5Bwithin%5D%5Btype%5D=Polygon",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B0%5D%5B0%5D=12",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B0%5D%5B1%5D=55",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B1%5D%5B0%5D=13",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B1%5D%5B1%5D=55",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B2%5D%5B0%5D=12",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B2%5D%5B1%5D=56",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B3%5D%5B0%5D=12",
            "where%5Blocation%5D%5Bwithin%5D%5Bcoordinates%5D%5B0%5D%5B3%5D%5B1%5D=55",
            "where%5Band%5D%5B0%5D%5Bstatus%5D%5Bequals%5D=published",
            "where%5Bor%5D%5B0%5D%5Bauthor%5D%5Bexists%5D=true",
            "where%5Bor%5D%5B1%5D%5Beditor%5D%5Bexists%5D=false",
            "joins%5Bcomments%5D%5Blimit%5D=5",
            "joins%5Bcomments%5D%5Bsort%5D=createdAt",
            "joins%5Bcomments%5D%5Bcount%5D=true",
            "joins%5Bcomments%5D%5Bwhere%5D%5Bapproved%5D%5Bequals%5D=true",
            "draft=true",
        });

        Assert.Equal(expected, sent);
    }
}

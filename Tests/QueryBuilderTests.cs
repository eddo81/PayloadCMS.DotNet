using Payload.CMS.Public;
using Payload.CMS.Public.Enums;

namespace Payload.CMS.Tests;

public class QueryBuilderTests
{
    private readonly QueryStringEncoder _encoder = new(addQueryPrefix: false);

    [Fact]
    public void SelectShouldSerializeAsCommaSeparatedList()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "title", "author" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select=title,author", actual);
    }

    [Fact]
    public void SortAndSortByDescendingShouldSerializeAsCommaSeparatedList()
    {
        var params_ = new QueryBuilder()
            .Sort("date")
            .SortByDescending("title")
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("sort=date,-title", actual);
    }

    [Fact]
    public void PopulateShouldSerializeAsCommaSeparatedList()
    {
        var params_ = new QueryBuilder()
            .Populate(new[] { "author", "comments" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("populate=author,comments", actual);
    }

    [Fact]
    public void WhereWithNestedOrGroupShouldFlattenCorrectly()
    {
        var params_ = new QueryBuilder()
            .Or(group =>
            {
                group.Where("title", Operator.Equals, "foo")
                     .Where("title", Operator.Equals, "bar");
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "where[or][0][title][equals]=foo&where[or][1][title][equals]=bar";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MixedLogicalNestingAndContainingOrShouldFlattenCorrectly()
    {
        var params_ = new QueryBuilder()
            .And(group =>
            {
                group.Where("status", Operator.Equals, "published")
                     .Or(inner =>
                     {
                         inner.Where("title", Operator.Equals, "foo")
                              .Where("title", Operator.Equals, "bar");
                     });
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = string.Join("&", new[]
        {
            "where[and][0][status][equals]=published",
            "where[and][1][or][0][title][equals]=foo",
            "where[and][1][or][1][title][equals]=bar"
        });

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultipleWhereCallsShouldMergeFieldsCorrectly()
    {
        var params_ = new QueryBuilder()
            .Where("title", Operator.Equals, "foo")
            .Where("status", Operator.Equals, "published")
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "where[title][equals]=foo&where[status][equals]=published";

        Assert.Equal(TestHelpers.Normalize(expected), TestHelpers.Normalize(actual));
    }

    [Fact]
    public void OrGroupsShouldProduceCorrectNestedTree()
    {
        var params_ = new QueryBuilder()
            .Or(group =>
            {
                group.Where("title", Operator.Equals, "foo")
                     .Where("status", Operator.Equals, "published");
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "where[or][0][title][equals]=foo&where[or][1][status][equals]=published";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void QueryBuilderShouldOverwriteWhereResultInsteadOfMerging()
    {
        var params_ = new QueryBuilder()
            .Where("title", Operator.Equals, "foo")
            .Where("title", Operator.Equals, "bar")
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("where[title][equals]=bar", actual);
    }
}

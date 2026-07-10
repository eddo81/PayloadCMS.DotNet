using PayloadCMS.DotNet.Enums;
using PayloadCMS.DotNet.Query;

namespace Payload.CMS.Tests;

public class QueryBuilderTests
{
    private readonly QueryStringEncoder _encoder = new(addQueryPrefix: false);

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
    public void PopulateShouldSerializeAsCollectionKeyedSelectShape()
    {
        var params_ = new QueryBuilder()
            .Populate("users", new[] { "name" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("populate[users][name]=true", actual);
    }

    [Fact]
    public void PopulateShouldSupportMultipleCollections()
    {
        var params_ = new QueryBuilder()
            .Populate("users", new[] { "name" })
            .Populate("boards", new[] { "title" })
            .Build();

        var actual = TestHelpers.Normalize(_encoder.Stringify(params_));
        var expected = TestHelpers.Normalize("populate[users][name]=true&populate[boards][title]=true");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PopulateRepeatedCallsForSameCollectionShouldMergeAdditively()
    {
        var params_ = new QueryBuilder()
            .Populate("users", new[] { "name" })
            .Populate("users", new[] { "avatar" })
            .Build();

        var actual = TestHelpers.Normalize(_encoder.Stringify(params_));
        var expected = TestHelpers.Normalize("populate[users][name]=true&populate[users][avatar]=true");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PopulateShouldExpandDotNotationIntoNestedShape()
    {
        var params_ = new QueryBuilder()
            .Populate("users", new[] { "group.number" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("populate[users][group][number]=true", actual);
    }

    [Fact]
    public void PopulateWithEmptyCollectionSlugShouldBeSkipped()
    {
        var params_ = new QueryBuilder()
            .Populate("", new[] { "name" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void DraftShouldSerializeAsLowercaseBoolean()
    {
        var params_ = new QueryBuilder()
            .Draft(true)
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("draft=true", actual);
    }

    [Fact]
    public void TrashShouldSerializeAsLowercaseBoolean()
    {
        var params_ = new QueryBuilder()
            .Trash(true)
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("trash=true", actual);
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

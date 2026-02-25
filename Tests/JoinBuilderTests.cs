using Payload.CMS.Public;
using Payload.CMS.Public.Enums;

namespace Payload.CMS.Tests;

public class JoinBuilderTests
{
    private readonly QueryStringEncoder _encoder = new(addQueryPrefix: false);

    [Fact]
    public void JoinBuilderShouldProduceCorrectNestedObjectStructure()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Where("posts", "author", Operator.Equals, "Alice")
                    .SortByDescending("posts", "title")
                    .Limit("posts", 1);
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][limit]=1&joins[posts][sort]=-title&joins[posts][where][author][equals]=Alice";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldOverwriteDuplicateResults()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Where("posts", "author", Operator.Equals, "Alice")
                    .Where("posts", "author", Operator.Equals, "Bob");
            })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("joins[posts][where][author][equals]=Bob", actual);
    }

    [Fact]
    public void JoinBuilderShouldOmitEmptyValues()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder.Sort("posts", "");
            })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void JoinBuilderShouldSupportMultipleJoinFields()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Limit("posts", 5)
                    .Sort("posts", "title")
                    .Count("comments")
                    .Limit("comments", 10);
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][limit]=5&joins[posts][sort]=title&joins[comments][limit]=10&joins[comments][count]=true";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldIgnoreInvalidOperationsButKeepValidOnes()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Sort("posts", "")
                    .Limit("posts", 3);
            })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("joins[posts][limit]=3", actual);
    }

    [Fact]
    public void JoinBuilderShouldAccumulateAcrossMultipleJoinCalls()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder.Limit("posts", 2);
            })
            .Join(joinBuilder =>
            {
                joinBuilder.SortByDescending("posts", "title");
            })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("joins[posts][limit]=2&joins[posts][sort]=-title", actual);
    }

    [Fact]
    public void JoinBuilderShouldAccumulateMultipleWhereOnDifferentFields()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Where("posts", "status", Operator.Equals, "published")
                    .Where("posts", "author", Operator.Equals, "Alice");
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][where][status][equals]=published&joins[posts][where][author][equals]=Alice";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldSupportNestedAndGroups()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder.And("posts", group =>
                {
                    group
                        .Where("status", Operator.Equals, "published")
                        .Where("author", Operator.Equals, "Alice");
                });
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][where][and][0][status][equals]=published&joins[posts][where][and][1][author][equals]=Alice";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldSupportNestedOrGroups()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder.Or("posts", group =>
                {
                    group
                        .Where("author", Operator.Equals, "Alice")
                        .Where("author", Operator.Equals, "Bob");
                });
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][where][or][0][author][equals]=Alice&joins[posts][where][or][1][author][equals]=Bob";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldSupportComplexNestedAndOrCombinations()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder.And("posts", group =>
                {
                    group
                        .Where("status", Operator.Equals, "published")
                        .Or(inner =>
                        {
                            inner
                                .Where("author", Operator.Equals, "Alice")
                                .Where("author", Operator.Equals, "Bob");
                        });
                });
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][where][and][0][status][equals]=published&joins[posts][where][and][1][or][0][author][equals]=Alice&joins[posts][where][and][1][or][1][author][equals]=Bob";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldCombineWhereWithOtherJoinOperations()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Where("posts", "status", Operator.Equals, "published")
                    .Limit("posts", 5)
                    .Sort("posts", "createdAt");
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][limit]=5&joins[posts][sort]=createdAt&joins[posts][where][status][equals]=published";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinBuilderShouldAccumulateWhereAndAndOnTheSameJoinField()
    {
        var params_ = new QueryBuilder()
            .Join(joinBuilder =>
            {
                joinBuilder
                    .Where("posts", "status", Operator.Equals, "published")
                    .And("posts", group =>
                    {
                        group
                            .Where("rating", Operator.GreaterThan, 3)
                            .Where("featured", Operator.Equals, true);
                    });
            })
            .Build();

        var actual = _encoder.Stringify(params_);
        var expected = "joins[posts][where][status][equals]=published&joins[posts][where][and][0][rating][greater_than]=3&joins[posts][where][and][1][featured][equals]=true";

        Assert.Equal(expected, actual);
    }
}

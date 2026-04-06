using PayloadCMS.DotNet.Query;

namespace Payload.CMS.Tests;

public class SelectBuilderTests
{
    private readonly QueryStringEncoder _encoder = new(addQueryPrefix: false);

    [Fact]
    public void SelectShouldSerializeAsBracketNotation()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "title", "author" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[title]=true&select[author]=true", actual);
    }

    [Fact]
    public void SelectWithDotNotationShouldSerializeAsNestedBracketNotation()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "title", "group.number" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[title]=true&select[group][number]=true", actual);
    }

    [Fact]
    public void SelectWithSiblingNestedFieldsShouldDeepMergeUnderSharedParent()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "group.number", "group.text" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[group][number]=true&select[group][text]=true", actual);
    }

    [Fact]
    public void ExcludeShouldSerializeAsFalse()
    {
        var params_ = new QueryBuilder()
            .Exclude(new[] { "content" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[content]=false", actual);
    }

    [Fact]
    public void MixedSelectAndExcludeShouldSerializeCorrectly()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "title" })
            .Exclude(new[] { "content" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[title]=true&select[content]=false", actual);
    }

    [Fact]
    public void EmptySelectShouldProduceNoOutput()
    {
        var params_ = new QueryBuilder()
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void MultipleSelectCallsShouldAccumulateFields()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "title" })
            .Select(new[] { "author" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[title]=true&select[author]=true", actual);
    }
}

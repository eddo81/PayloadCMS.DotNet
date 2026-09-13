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

    [Theory]
    [InlineData("")]
    [InlineData("a..b")]
    [InlineData(".title")]
    [InlineData("title.")]
    public void SelectWithEmptyFieldOrEmptyPathSegmentShouldProduceNoOutput(string field)
    {
        var params_ = new QueryBuilder()
            .Select(new[] { field })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void SelectShouldKeepValidFieldsWhenAnotherFieldIsSkipped()
    {
        var params_ = new QueryBuilder()
            .Select(new[] { "title", "a..b", "group.number" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[title]=true&select[group][number]=true", actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a..b")]
    [InlineData(".title")]
    [InlineData("title.")]
    public void ExcludeWithEmptyFieldOrEmptyPathSegmentShouldProduceNoOutput(string field)
    {
        var params_ = new QueryBuilder()
            .Exclude(new[] { field })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void ExcludeShouldKeepValidFieldsWhenAnotherFieldIsSkipped()
    {
        var params_ = new QueryBuilder()
            .Exclude(new[] { "content", "a..b" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[content]=false", actual);
    }

    [Fact]
    public void SelectWithNullFieldShouldProduceNoOutput()
    {
        var params_ = new QueryBuilder()
            .Select(new string[] { null! })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void ExcludeWithNullFieldShouldProduceNoOutput()
    {
        var params_ = new QueryBuilder()
            .Exclude(new string[] { null! })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void SelectShouldKeepValidFieldsWhenANullFieldIsSkipped()
    {
        var params_ = new QueryBuilder()
            .Select(new string[] { "title", null! })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("select[title]=true", actual);
    }

    [Fact]
    public void PopulateWithEmptyFieldShouldProduceNoOutput()
    {
        var params_ = new QueryBuilder()
            .Populate("users", new[] { "" })
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("", actual);
    }

    [Fact]
    public void LimitZeroShouldStillSerialize()
    {
        var params_ = new QueryBuilder()
            .Limit(0)
            .Build();

        var actual = _encoder.Stringify(params_);

        Assert.Equal("limit=0", actual);
    }
}

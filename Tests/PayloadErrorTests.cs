using PayloadCMS.DotNet;

namespace Payload.CMS.Tests;

public class PayloadErrorTests
{
    // ── cause is not a navigable dictionary ─────────────────────

    [Fact]
    public void GetDetails_WhenCauseIsNull_ReturnsEmptyList()
    {
        var error = new PayloadError(400, cause: null);

        Assert.Empty(error.GetDetails());
    }

    [Fact]
    public void GetDetails_WhenCauseIsNotADictionary_ReturnsEmptyList()
    {
        var error = new PayloadError(400, cause: "unexpected string");

        Assert.Empty(error.GetDetails());
    }

    // ── errors array ─────────────────────────────────────────────

    [Fact]
    public void GetDetails_WithErrorsArray_ReturnsMessageAndField()
    {
        var cause = new Dictionary<string, object?>
        {
            ["errors"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["message"] = "The following field has failed validation: email",
                    ["field"] = "email",
                },
            },
        };

        var error = new PayloadError(400, cause: cause);
        var details = error.GetDetails();

        Assert.Single(details);
        Assert.Equal("The following field has failed validation: email", details[0].Message);
        Assert.Equal("email", details[0].Field);
    }

    [Fact]
    public void GetDetails_WithErrorsArrayItemMissingField_ReturnsNullField()
    {
        var cause = new Dictionary<string, object?>
        {
            ["errors"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["message"] = "Something went wrong",
                },
            },
        };

        var error = new PayloadError(400, cause: cause);
        var details = error.GetDetails();

        Assert.Single(details);
        Assert.Equal("Something went wrong", details[0].Message);
        Assert.Null(details[0].Field);
    }

    [Fact]
    public void GetDetails_WithErrorsArrayContainingInvalidItems_SkipsInvalidItems()
    {
        var cause = new Dictionary<string, object?>
        {
            ["errors"] = new List<object?>
            {
                null,
                new Dictionary<string, object?> { ["message"] = "Valid error", ["field"] = "email" },
                new Dictionary<string, object?> { ["field"] = "password" },
            },
        };

        var error = new PayloadError(400, cause: cause);
        var details = error.GetDetails();

        Assert.Single(details);
        Assert.Equal("Valid error", details[0].Message);
        Assert.Equal("email", details[0].Field);
    }

    [Fact]
    public void GetDetails_WithEmptyErrorsArray_ReturnsEmptyList()
    {
        var cause = new Dictionary<string, object?>
        {
            ["errors"] = new List<object?>(),
        };

        var error = new PayloadError(400, cause: cause);

        Assert.Empty(error.GetDetails());
    }

    // ── top-level message fallback ────────────────────────────────

    [Fact]
    public void GetDetails_WithTopLevelMessage_ReturnsSingleItemWithNoField()
    {
        var cause = new Dictionary<string, object?>
        {
            ["message"] = "You are not allowed to perform this action.",
        };

        var error = new PayloadError(401, cause: cause);
        var details = error.GetDetails();

        Assert.Single(details);
        Assert.Equal("You are not allowed to perform this action.", details[0].Message);
        Assert.Null(details[0].Field);
    }

    // ── unrecognised shape ────────────────────────────────────────

    [Fact]
    public void GetDetails_WithNoErrorsOrMessage_ReturnsEmptyList()
    {
        var cause = new Dictionary<string, object?>
        {
            ["status"] = 400,
        };

        var error = new PayloadError(400, cause: cause);

        Assert.Empty(error.GetDetails());
    }
}

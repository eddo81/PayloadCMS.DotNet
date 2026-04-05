using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Models.Errors;

namespace Payload.CMS.Tests;

public class PayloadErrorTests
{
    // ── body is null or non-JSON ──────────────────────────────────

    [Fact]
    public void Result_WhenBodyIsNull_ReturnsEmptyList()
    {
        var error = new PayloadError(400, body: null);

        Assert.Empty(error.Result);
    }

    [Fact]
    public void Result_WhenBodyIsNotJson_ReturnsEmptyList()
    {
        var error = new PayloadError(400, body: "Internal Server Error");

        Assert.Empty(error.Result);
    }

    [Fact]
    public void Result_WhenBodyHasNoErrorsKey_ReturnsEmptyList()
    {
        var error = new PayloadError(400, body: """{"status":400}""");

        Assert.Empty(error.Result);
    }

    [Fact]
    public void Result_WhenErrorsArrayIsEmpty_ReturnsEmptyList()
    {
        var error = new PayloadError(400, body: """{"errors":[]}""");

        Assert.Empty(error.Result);
    }

    // ── base fields ───────────────────────────────────────────────

    [Fact]
    public void Result_PopulatesName()
    {
        var body = """{"errors":[{"name":"ValidationError","message":"The following field is invalid: title"}]}""";
        var error = new PayloadError(400, body: body);

        Assert.Single(error.Result);
        Assert.Equal("ValidationError", error.Result[0].Name);
    }

    [Fact]
    public void Result_PopulatesMessage()
    {
        var body = """{"errors":[{"name":"Forbidden","message":"You are not allowed to perform this action."}]}""";
        var error = new PayloadError(403, body: body);

        Assert.Equal("You are not allowed to perform this action.", error.Result[0].Message);
    }

    [Fact]
    public void Result_PopulatesField_ForMongooseValidationItems()
    {
        var body = """{"errors":[{"message":"Value must be unique","field":"email"}]}""";
        var error = new PayloadError(400, body: body);

        Assert.Equal("email", error.Result[0].Field);
    }

    [Fact]
    public void Result_NullName_WhenNameAbsent()
    {
        var body = """{"errors":[{"message":"Something went wrong"}]}""";
        var error = new PayloadError(400, body: body);

        Assert.Null(error.Result[0].Name);
    }

    [Fact]
    public void Result_NullField_WhenFieldAbsent()
    {
        var body = """{"errors":[{"name":"Forbidden","message":"No access."}]}""";
        var error = new PayloadError(403, body: body);

        Assert.Null(error.Result[0].Field);
    }

    // ── Json escape hatch ─────────────────────────────────────────

    [Fact]
    public void Result_JsonContainsRawEntry()
    {
        var body = """
            {
              "errors": [{
                "name": "ValidationError",
                "message": "The following field is invalid: title",
                "data": {
                  "collection": "posts",
                  "errors": [{ "message": "Required", "path": "title" }]
                }
              }]
            }
            """;
        var error = new PayloadError(400, body: body);

        Assert.True(error.Result[0].Json.ContainsKey("data"));
    }

    [Fact]
    public void Result_JsonAllowsConsumerToReadDataBlock()
    {
        var body = """
            {
              "errors": [{
                "name": "ValidationError",
                "message": "The following field is invalid: title",
                "data": {
                  "collection": "posts",
                  "errors": [{ "message": "Required", "path": "title" }]
                }
              }]
            }
            """;
        var error = new PayloadError(400, body: body);
        var data = error.Result[0].Json["data"] as Dictionary<string, object?>;

        Assert.NotNull(data);
        Assert.Equal("posts", data["collection"]);
    }

    // ── multiple errors ───────────────────────────────────────────

    [Fact]
    public void Result_MultipleItems_AllPopulated()
    {
        var body = """
            {
              "errors": [
                { "name": "Forbidden", "message": "No access." },
                { "message": "Something went wrong" }
              ]
            }
            """;
        var error = new PayloadError(400, body: body);

        Assert.Equal(2, error.Result.Count);
        Assert.Equal("Forbidden", error.Result[0].Name);
        Assert.Null(error.Result[1].Name);
    }

    [Fact]
    public void Result_SkipsNullItemsInErrorsArray()
    {
        var body = """
            {
              "errors": [
                null,
                { "name": "Forbidden", "message": "No access." }
              ]
            }
            """;
        var error = new PayloadError(400, body: body);

        Assert.Single(error.Result);
        Assert.Equal("Forbidden", error.Result[0].Name);
    }

    // ── Body and ServerStack passthrough ────────────────────────────────

    [Fact]
    public void Body_IsPreservedVerbatim()
    {
        var body = """{"errors":[{"name":"Forbidden","message":"No access."}]}""";
        var error = new PayloadError(403, body: body);

        Assert.Equal(body, error.Body);
    }

    [Fact]
    public void ServerStack_IsNullWhenAbsent()
    {
        var error = new PayloadError(400, body: """{"errors":[]}""");

        Assert.Null(error.ServerStack);
    }

    [Fact]
    public void ServerStack_IsPopulatedFromBody()
    {
        var body = """{"errors":[],"stack":"Error\n    at Object.<anonymous>"}""";
        var error = new PayloadError(400, body: body);

        Assert.Equal("Error\n    at Object.<anonymous>", error.ServerStack);
    }

    // ── StatusCode and Message ────────────────────────────────────

    [Fact]
    public void StatusCode_IsSet()
    {
        var error = new PayloadError(422);

        Assert.Equal(422, error.StatusCode);
    }

    [Fact]
    public void Message_DefaultsToStatusCodeMessage()
    {
        var error = new PayloadError(404);

        Assert.Equal("[PayloadError] Request failed with status: 404", error.Message);
    }
}

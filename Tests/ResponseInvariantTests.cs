using System.Net;
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Enums;
using PayloadCMS.DotNet.Models.Collection;
using PayloadCMS.DotNet.Query;

namespace Payload.CMS.Tests;

/// <summary>
/// How responses become DTOs. Each test feeds a raw response body through the public SDK method and
/// checks the DTO it returns, so it does not depend on how the SDK represents JSON internally.
/// </summary>
public class ResponseInvariantTests
{
    private static async Task<PaginatedDocsDTO> FindWith(string body)
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, body);

        return await sdk.Find("posts");
    }

    private static async Task<DocumentDTO> FindByIdWith(string body)
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, body);

        return await sdk.FindById("posts", "x");
    }

    // Only these types may appear anywhere inside a DTO's Json.
    private static void AssertPlainJsonValues(object? value, string path)
    {
        if (value is Dictionary<string, object?> dictionary)
        {
            foreach (var (key, nested) in dictionary)
            {
                AssertPlainJsonValues(nested, $"{path}.{key}");
            }

            return;
        }

        if (value is List<object?> list)
        {
            for (int index = 0; index < list.Count; index++)
            {
                AssertPlainJsonValues(list[index], $"{path}[{index}]");
            }

            return;
        }

        var allowed = value is null || value is string || value is bool
            || value is int || value is long || value is double;

        Assert.True(allowed, $"{path} holds a {value?.GetType().FullName}");
    }

    // ── Json ────────────────────────────────────────────────────

    [Fact]
    public async Task DocumentJson_HoldsOnlyPlainValues_WithNumbersAsIntLongOrDouble()
    {
        var doc = await FindByIdWith("""
            {
              "id": "a",
              "text": "hello",
              "flag": true,
              "nothing": null,
              "small": 7,
              "large": 9007199254740993,
              "fraction": 2.5,
              "whole": 3.0,
              "nested": { "list": [1, "two", [3, { "deep": false }], null] }
            }
            """);

        AssertPlainJsonValues(doc.Json, "Json");
        Assert.IsType<int>(doc.Json["small"]);
        Assert.IsType<long>(doc.Json["large"]);
        Assert.IsType<double>(doc.Json["fraction"]);
        Assert.IsType<double>(doc.Json["whole"]);
        Assert.Null(doc.Json["nothing"]);
        Assert.Equal("hello", doc.Json["text"]);
        Assert.Equal(true, doc.Json["flag"]);

        var nested = Assert.IsType<Dictionary<string, object?>>(doc.Json["nested"]);
        var list = Assert.IsType<List<object?>>(nested["list"]);
        Assert.Equal(4, list.Count);
        Assert.Equal("two", list[1]);
        var deep = Assert.IsType<Dictionary<string, object?>>(Assert.IsType<List<object?>>(list[2])[1]);
        Assert.Equal(false, deep["deep"]);
    }

    [Fact]
    public async Task DocumentJson_KeepsEveryPropertyIncludingTheMappedOnes()
    {
        var doc = await FindByIdWith("""{ "id": "a", "createdAt": "2024-01-01T00:00:00.000Z", "title": "T" }""");

        Assert.Equal(new[] { "id", "createdAt", "title" }, doc.Json.Keys);
        Assert.Equal("2024-01-01T00:00:00.000Z", doc.Json["createdAt"]);
    }

    [Fact]
    public async Task DocumentJson_WithADuplicateKey_KeepsTheLastValue()
    {
        var doc = await FindByIdWith("""{ "id": "first", "title": "T", "id": "second" }""");

        Assert.Equal("second", doc.Id);
        Assert.Equal("second", doc.Json["id"]);
        Assert.Equal(2, doc.Json.Count);
    }

    // ── DocumentDTO.Id ──────────────────────────────────────────

    [Theory]
    [InlineData("\"abc\"", "abc")]
    [InlineData("42", "42")]
    [InlineData("9007199254740993", "9007199254740993")]
    [InlineData("3.5", "3.5")]
    [InlineData("3.0", "3")]
    [InlineData("1e3", "1000")]
    [InlineData("true", "")]
    [InlineData("null", "")]
    [InlineData("{}", "")]
    public async Task DocumentId_IsReadFromAStringOrNumber(string raw, string expected)
    {
        var doc = await FindByIdWith("{ \"id\": " + raw + " }");

        Assert.Equal(expected, doc.Id);
    }

    [Fact]
    public async Task DocumentId_WhenMissing_IsEmpty()
    {
        var doc = await FindByIdWith("{ \"title\": \"T\" }");

        Assert.Equal("", doc.Id);
    }

    // ── DocumentDTO dates ───────────────────────────────────────

    [Fact]
    public async Task DocumentDates_WithZ_AreUtc()
    {
        var doc = await FindByIdWith("""{ "createdAt": "2024-01-01T00:00:00.000Z", "updatedAt": "2024-06-01T12:30:00.000Z" }""");

        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), doc.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, doc.CreatedAt!.Value.Kind);
        Assert.Equal(new DateTime(2024, 6, 1, 12, 30, 0, DateTimeKind.Utc), doc.UpdatedAt);
    }

    [Fact]
    public async Task DocumentDates_WithAnOffset_AreTheSameInstantInLocalTime()
    {
        var doc = await FindByIdWith("""{ "createdAt": "2024-01-01T02:00:00+02:00" }""");

        Assert.Equal(DateTimeKind.Local, doc.CreatedAt!.Value.Kind);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), doc.CreatedAt.Value.ToUniversalTime());
    }

    [Theory]
    [InlineData("\"2024-01-01T00:00:00\"")]
    [InlineData("\"2024-01-01\"")]
    public async Task DocumentDates_WithNoZone_AreUnspecified(string raw)
    {
        var doc = await FindByIdWith("{ \"createdAt\": " + raw + " }");

        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0), doc.CreatedAt);
        Assert.Equal(DateTimeKind.Unspecified, doc.CreatedAt!.Value.Kind);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"nope\"")]
    [InlineData("5")]
    [InlineData("null")]
    public async Task DocumentDates_ThatAreNotDateStrings_AreNull(string raw)
    {
        var doc = await FindByIdWith("{ \"createdAt\": " + raw + ", \"updatedAt\": " + raw + " }");

        Assert.Null(doc.CreatedAt);
        Assert.Null(doc.UpdatedAt);
    }

    // ── PaginatedDocsDTO ────────────────────────────────────────

    [Fact]
    public async Task Find_MapsEveryPaginationField()
    {
        var page = await FindWith("""
            {
              "docs": [{ "id": "a" }, { "id": "b" }],
              "totalDocs": 12, "limit": 2, "totalPages": 6, "page": 3,
              "hasNextPage": true, "hasPrevPage": true, "nextPage": 4, "prevPage": 2
            }
            """);

        Assert.Equal(new[] { "a", "b" }, page.Docs.Select(doc => doc.Id));
        Assert.Equal(12, page.TotalDocs);
        Assert.Equal(2, page.Limit);
        Assert.Equal(6, page.TotalPages);
        Assert.Equal(3, page.Page);
        Assert.True(page.HasNextPage);
        Assert.True(page.HasPrevPage);
        Assert.Equal(4, page.NextPage);
        Assert.Equal(2, page.PrevPage);
    }

    [Theory]
    [InlineData("3", 3)]
    [InlineData("3.0", 3)]
    [InlineData("3.9", 3)]
    [InlineData("-3.9", -3)]
    [InlineData("1e3", 1000)]
    [InlineData("2147483648", -2147483648)]
    [InlineData("9007199254740993", 1)]
    public async Task Find_NumberFields_AreReadAsInt(string raw, int expected)
    {
        var page = await FindWith("{ \"docs\": [], \"totalDocs\": " + raw + ", \"page\": " + raw + " }");

        Assert.Equal(expected, page.TotalDocs);
        Assert.Equal(expected, page.Page);
    }

    [Theory]
    [InlineData("\"5\"")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task Find_NumberFieldsThatAreNotNumbers_KeepTheirDefaults(string raw)
    {
        var page = await FindWith(
            "{ \"docs\": [], \"totalDocs\": " + raw + ", \"limit\": " + raw + ", \"totalPages\": " + raw +
            ", \"page\": " + raw + ", \"nextPage\": " + raw + ", \"prevPage\": " + raw + " }");

        Assert.Equal(0, page.TotalDocs);
        Assert.Equal(10, page.Limit);
        Assert.Equal(1, page.TotalPages);
        Assert.Null(page.Page);
        Assert.Null(page.NextPage);
        Assert.Null(page.PrevPage);
    }

    [Fact]
    public async Task Find_BooleanFieldsThatAreNotBooleans_StayFalse()
    {
        var page = await FindWith("""{ "docs": [], "hasNextPage": "true", "hasPrevPage": 1 }""");

        Assert.False(page.HasNextPage);
        Assert.False(page.HasPrevPage);
    }

    [Fact]
    public async Task Find_DocsThatAreNotObjects_AreSkipped()
    {
        var page = await FindWith("""{ "docs": [{ "id": "a" }, 1, null, "x", [], { "id": "b" }] }""");

        Assert.Equal(new[] { "a", "b" }, page.Docs.Select(doc => doc.Id));
    }

    [Fact]
    public async Task Find_DocsThatAreNotAList_GiveNoDocs()
    {
        var page = await FindWith("""{ "docs": { "id": "a" }, "totalDocs": 1 }""");

        Assert.Empty(page.Docs);
        Assert.Equal(1, page.TotalDocs);
    }

    // ── TotalDocsDTO ────────────────────────────────────────────

    [Theory]
    [InlineData("""{ "totalDocs": 7 }""", 7)]
    [InlineData("""{ "totalDocs": 7.9 }""", 7)]
    [InlineData("""{ "totalDocs": "7" }""", 0)]
    [InlineData("""{ }""", 0)]
    public async Task Count_ReadsTotalDocs(string body, int expected)
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, body);

        Assert.Equal(expected, await sdk.Count("posts"));
    }

    // ── doc / result unwrapping ─────────────────────────────────

    [Theory]
    [InlineData("""{ "doc": { "id": "a", "title": "T" }, "message": "Created" }""", "a", 2)]
    [InlineData("""{ "message": "Created" }""", "", 0)]
    [InlineData("""{ "doc": "a" }""", "", 0)]
    [InlineData("""{ "doc": null }""", "", 0)]
    public async Task Create_UnwrapsDoc(string body, string expectedId, int expectedKeys)
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.Created, body);

        var doc = await sdk.Create("posts", new Dictionary<string, object?>());

        Assert.Equal(expectedId, doc.Id);
        Assert.Equal(expectedKeys, doc.Json.Count);
    }

    [Theory]
    [InlineData("""{ "result": { "id": "g", "title": "T" }, "message": "Updated" }""", "g", 2)]
    [InlineData("""{ "doc": { "id": "g" } }""", "", 0)]
    public async Task UpdateGlobal_UnwrapsResult(string body, string expectedId, int expectedKeys)
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, body);

        var doc = await sdk.UpdateGlobal("settings", new Dictionary<string, object?>());

        Assert.Equal(expectedId, doc.Id);
        Assert.Equal(expectedKeys, doc.Json.Count);
    }

    // ── Bulk operations ─────────────────────────────────────────

    [Fact]
    public async Task Update_MapsDocsAndErrors_SkippingItemsThatAreNotObjects()
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, """
            {
              "docs": [{ "id": "a" }, 5, { "id": 7 }],
              "errors": [{ "id": 8, "message": "Locked" }, "x", { "id": "c", "message": 3 }]
            }
            """);

        var result = await sdk.Update("posts", new Dictionary<string, object?>(),
            new QueryBuilder().Where(where => where.Field("title", Operator.Equals, "T")));

        Assert.Equal(new[] { "a", "7" }, result.Docs.Select(doc => doc.Id));
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("8", result.Errors[0].Id);
        Assert.Equal("Locked", result.Errors[0].Message);
        Assert.Equal("c", result.Errors[1].Id);
        Assert.Equal("", result.Errors[1].Message);
    }

    [Fact]
    public async Task Delete_PartialFailureWith400_ReturnsTheResult()
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.BadRequest, """
            { "docs": [{ "id": "a" }], "errors": [{ "id": "b", "message": "Locked" }] }
            """);

        var result = await sdk.Delete("posts",
            new QueryBuilder().Where(where => where.Field("title", Operator.Equals, "T")));

        Assert.Equal("a", Assert.Single(result.Docs).Id);
        Assert.Equal("b", Assert.Single(result.Errors).Id);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, """{ "errors": [{ "message": "Missing where" }] }""")]
    [InlineData(HttpStatusCode.BadRequest, """{ "docs": [] }""")]
    [InlineData(HttpStatusCode.BadRequest, "not json")]
    [InlineData(HttpStatusCode.Forbidden, """{ "docs": [], "errors": [] }""")]
    public async Task Delete_OtherFailures_Throw(HttpStatusCode status, string body)
    {
        var (sdk, _) = SdkFactory.Create(status, body);

        await Assert.ThrowsAsync<PayloadError>(() => sdk.Delete("posts",
            new QueryBuilder().Where(where => where.Field("title", Operator.Equals, "T"))));
    }

    // ── PayloadError ────────────────────────────────────────────

    [Fact]
    public void PayloadError_ReadsErrorsAndStack_SkippingItemsThatAreNotObjects()
    {
        var error = new PayloadError(400, body: """
            {
              "errors": [
                { "name": "ValidationError", "message": "Invalid", "field": "title", "data": { "path": "title" } },
                "x",
                { "message": 5, "name": null }
              ],
              "stack": "at somewhere"
            }
            """);

        Assert.Equal("at somewhere", error.ServerStack);
        Assert.Equal(2, error.Result.Count);
        Assert.Equal("ValidationError", error.Result[0].Name);
        Assert.Equal("Invalid", error.Result[0].Message);
        Assert.Equal("title", error.Result[0].Field);
        var data = Assert.IsType<Dictionary<string, object?>>(error.Result[0].Json["data"]);
        Assert.Equal("title", data["path"]);
        AssertPlainJsonValues(error.Result[0].Json, "Result[0].Json");
        Assert.Null(error.Result[1].Name);
        Assert.Null(error.Result[1].Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "errors": { "message": "x" } }""")]
    [InlineData("""[{ "message": "x" }]""")]
    [InlineData("""{ "stack": 5 }""")]
    public void PayloadError_WithABodyItCannotRead_HasNoResult(string body)
    {
        var error = new PayloadError(500, body: body);

        Assert.Empty(error.Result);
        Assert.Null(error.ServerStack);
    }

    // ── Auth DTOs ───────────────────────────────────────────────

    [Fact]
    public async Task Login_MapsEveryField()
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, """
            { "token": "t", "exp": 1735689600, "user": { "id": "u", "email": "a@b.c" }, "message": "Auth Passed" }
            """);

        var login = await sdk.Login("users", new Dictionary<string, object?>());

        Assert.Equal("t", login.Token);
        Assert.Equal(1735689600, login.Exp);
        Assert.Equal("u", login.User!.Id);
        Assert.Equal("a@b.c", login.User.Json["email"]);
        Assert.Equal("Auth Passed", login.Message);
    }

    [Fact]
    public async Task Me_MapsEveryField_AndAMissingUserIsNull()
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, """
            { "user": { "id": "u" }, "token": "t", "exp": 17.9, "collection": "users", "strategy": "local-jwt" }
            """);

        var me = await sdk.Me("users");

        Assert.Equal("u", me.User!.Id);
        Assert.Equal("t", me.Token);
        Assert.Equal(17, me.Exp);
        Assert.Equal("users", me.Collection);
        Assert.Equal("local-jwt", me.Strategy);

        var (anonymousSdk, _) = SdkFactory.Create(HttpStatusCode.OK, """{ "user": null, "message": "Account" }""");
        var anonymous = await anonymousSdk.Me("users");

        Assert.Null(anonymous.User);
    }

    [Fact]
    public async Task RefreshToken_MapsEveryField()
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, """
            { "refreshedToken": "r", "exp": 99, "user": { "id": "u" } }
            """);

        var refresh = await sdk.RefreshToken("users");

        Assert.Equal("r", refresh.RefreshedToken);
        Assert.Equal(99, refresh.Exp);
        Assert.Equal("u", refresh.User!.Id);
    }

    [Theory]
    [InlineData("2200000000", 2200000000L)]   // after 2038-01-19, beyond the int range
    [InlineData("1735689600", 1735689600L)]
    [InlineData("17.9", 17L)]                 // a fraction is truncated
    [InlineData("\"1735689600\"", 0L)]        // a string is not a number: the default is kept
    public async Task Exp_ReadsWholeUnixSeconds_OnEveryAuthResult(string exp, long expected)
    {
        var (loginSdk, _) = SdkFactory.Create(HttpStatusCode.OK, $$"""{ "token": "t", "exp": {{exp}} }""");
        var (meSdk, _) = SdkFactory.Create(HttpStatusCode.OK, $$"""{ "token": "t", "exp": {{exp}} }""");
        var (refreshSdk, _) = SdkFactory.Create(HttpStatusCode.OK, $$"""{ "refreshedToken": "r", "exp": {{exp}} }""");

        var login = await loginSdk.Login("users", new Dictionary<string, object?>());
        var me = await meSdk.Me("users");
        var refresh = await refreshSdk.RefreshToken("users");

        Assert.Equal(expected, login.Exp);
        Assert.Equal(expected, me.Exp);
        Assert.Equal(expected, refresh.Exp);
    }

    [Fact]
    public async Task ResetPassword_MapsEveryField()
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, """{ "user": { "id": "u" }, "token": "t" }""");

        var reset = await sdk.ResetPassword("users", new Dictionary<string, object?>());

        Assert.Equal("u", reset.User!.Id);
        Assert.Equal("t", reset.Token);
    }

    [Theory]
    [InlineData("""{ "message": "Sent" }""", "Sent")]
    [InlineData("""{ "message": 5 }""", "")]
    [InlineData("""{ }""", "")]
    public async Task ForgotPassword_ReadsTheMessage(string body, string expected)
    {
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, body);

        var result = await sdk.ForgotPassword("users", new Dictionary<string, object?>());

        Assert.Equal(expected, result.Message);
    }
}

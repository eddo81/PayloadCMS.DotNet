using System.Net;
using System.Text;
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Config;
using PayloadCMS.DotNet.Enums;
using PayloadCMS.DotNet.Query;
using PayloadCMS.DotNet.Models;

namespace Payload.CMS.Tests;

/// <summary>
/// Mock HTTP handler that returns a preset response for every request.
/// Captures the last outbound request for assertion.
/// </summary>
internal sealed class MockHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
        };

        return Task.FromResult(response);
    }
}

/// <summary>
/// Builds a <see cref="PayloadSDK"/> wired to a <see cref="MockHandler"/>.
/// </summary>
internal static class SdkFactory
{
    internal static (PayloadSDK Sdk, MockHandler Handler) Create(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new MockHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var sdk = new PayloadSDK(httpClient, "http://localhost:3000");

        return (sdk, handler);
    }
}

public class PayloadSDKTests
{
    // ── Find ────────────────────────────────────────────────────

    [Fact]
    public async Task Find_ReturnsPaginatedDocsDTO()
    {
        const string json = """
            {
              "docs": [{ "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }],
              "totalDocs": 1, "limit": 10, "totalPages": 1, "page": 1,
              "pagingCounter": 1, "hasPrevPage": false, "hasNextPage": false,
              "prevPage": null, "nextPage": null
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.Find("posts");

        Assert.Equal(1, result.TotalDocs);
        Assert.Single(result.Docs);
        Assert.Equal("abc123", result.Docs[0].Id);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/api/posts", handler.LastRequest.RequestUri!.ToString());
    }

    // ── FindById ────────────────────────────────────────────────

    [Fact]
    public async Task FindById_ReturnsDocumentDTO()
    {
        const string json = """
            { "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindById("posts", "abc123");

        Assert.Equal("abc123", result.Id);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/api/posts/abc123", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task FindById_NumericId_IsNormalizedToString()
    {
        // Postgres/SQLite adapters return numeric ids instead of Mongo-style strings.
        const string json = """
            { "id": 42, "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            """;
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindById("posts", "42");

        Assert.Equal("42", result.Id);
    }

    // ── Create ──────────────────────────────────────────────────

    [Fact]
    public async Task Create_PostsJsonBodyAndReturnsDocumentDTO()
    {
        const string json = """
            { "doc": { "id": "new1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.Created, json);

        var data = new Dictionary<string, object?> { ["title"] = "Hello" };
        var result = await sdk.Create("posts", data);

        Assert.Equal("new1", result.Id);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/posts", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task Create_WithDraftQuery_AppendsDraftParamToUrl()
    {
        // Draft writes: saving a draft requires ?draft=true on the POST (Payload v3 drafts).
        const string json = """
            { "doc": { "id": "new1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.Created, json);

        var data = new Dictionary<string, object?> { ["title"] = "Hello" };
        var query = new QueryBuilder().Draft(true);
        await sdk.Create("posts", data, query);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("draft=true", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task Create_WithFile_SendsMultipartWithPlainStringPayloadPart()
    {
        const string json = """
            { "doc": { "id": "new1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.Created, json);

        var data = new Dictionary<string, object?> { ["alt"] = "My image" };
        var file = new PayloadCMS.DotNet.Upload.FileUpload(new byte[] { 1, 2, 3 }, "photo.png", "image/png");
        await sdk.Create("media", data, file: file);

        var multipart = Assert.IsType<MultipartFormDataContent>(handler.LastRequest!.Content);
        HttpContent? filePart = null;
        HttpContent? payloadPart = null;

        foreach (var part in multipart)
        {
            if (part.Headers.ContentDisposition!.Name!.Trim('"') == "file")
            {
                filePart = part;
            }

            if (part.Headers.ContentDisposition!.Name!.Trim('"') == "_payload")
            {
                payloadPart = part;
            }
        }

        Assert.NotNull(filePart);
        Assert.Equal("image/png", filePart!.Headers.ContentType!.MediaType);

        // _payload must be a plain string field (TS: formData.append('_payload', JSON.stringify(data))),
        // NOT an application/json part — some multipart parsers treat typed parts as files.
        Assert.NotNull(payloadPart);
        Assert.NotEqual("application/json", payloadPart!.Headers.ContentType?.MediaType);
        var payloadText = await payloadPart.ReadAsStringAsync();
        Assert.Contains("My image", payloadText);
    }

    // ── UpdateById ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateById_PatchesDocumentAndReturnsDTO()
    {
        const string json = """
            { "doc": { "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["title"] = "Updated" };
        var result = await sdk.UpdateById("posts", "abc123", data);

        Assert.Equal("abc123", result.Id);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Contains("/api/posts/abc123", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateById_WithDraftQuery_AppendsDraftParamToUrl()
    {
        const string json = """
            { "doc": { "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["title"] = "Draft edit" };
        var query = new QueryBuilder().Draft(true);
        await sdk.UpdateById("posts", "abc123", data, query);

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Contains("/api/posts/abc123", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("draft=true", handler.LastRequest.RequestUri!.Query);
    }

    // ── DeleteById ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteById_DeletesDocumentAndReturnsDTO()
    {
        const string json = """
            { "doc": { "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.DeleteById("posts", "abc123");

        Assert.Equal("abc123", result.Id);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/api/posts/abc123", handler.LastRequest.RequestUri!.ToString());
    }

    // ── Update (bulk) ───────────────────────────────────────────

    [Fact]
    public async Task Update_PatchesAndReturnsBulkOperationDTO()
    {
        const string json = """
            {
              "docs": [{ "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }],
              "errors": []
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["published"] = false };
        var query = new QueryBuilder().Where("title", Operator.Equals, "Hello");
        var result = await sdk.Update("posts", data, query);

        Assert.Single(result.Docs);
        Assert.Empty(result.Errors);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
    }

    // ── Delete (bulk) ───────────────────────────────────────────

    [Fact]
    public async Task Delete_DeletesAndReturnsBulkOperationDTO()
    {
        const string json = """
            {
              "docs": [{ "id": "abc123", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }],
              "errors": []
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var query = new QueryBuilder().Where("title", Operator.Contains, "Test");
        var result = await sdk.Delete("posts", query);

        Assert.Single(result.Docs);
        Assert.Empty(result.Errors);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }

    // ── Count ───────────────────────────────────────────────────

    [Fact]
    public async Task Count_ReturnsTotalDocs()
    {
        const string json = """{ "totalDocs": 42 }""";
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.Count("posts");

        Assert.Equal(42, result);
    }

    // ── FindGlobal ──────────────────────────────────────────────

    [Fact]
    public async Task FindGlobal_ReturnsDocumentDTO()
    {
        const string json = """
            { "id": "g1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindGlobal("site-settings");

        Assert.Equal("g1", result.Id);
        Assert.Contains("/api/globals/site-settings", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── UpdateGlobal ────────────────────────────────────────────

    [Fact]
    public async Task UpdateGlobal_PostsAndReturnsDocumentDTO()
    {
        const string json = """
            { "result": { "id": "g1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["siteName"] = "Test" };
        var result = await sdk.UpdateGlobal("site-settings", data);

        Assert.Equal("g1", result.Id);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/globals/site-settings", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateGlobal_WithDraftQuery_AppendsDraftParamToUrl()
    {
        const string json = """
            { "result": { "id": "g1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["siteName"] = "Draft name" };
        var query = new QueryBuilder().Draft(true);
        await sdk.UpdateGlobal("site-settings", data, query);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("draft=true", handler.LastRequest.RequestUri!.Query);
    }

    // ── FindVersions ────────────────────────────────────────────

    [Fact]
    public async Task FindVersions_ReturnsPaginatedDocsDTO()
    {
        const string json = """
            {
              "docs": [{ "id": "v1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }],
              "totalDocs": 1, "limit": 10, "totalPages": 1, "page": 1,
              "pagingCounter": 1, "hasPrevPage": false, "hasNextPage": false,
              "prevPage": null, "nextPage": null
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindVersions("posts");

        Assert.Single(result.Docs);
        Assert.Contains("/api/posts/versions", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── FindVersionById ─────────────────────────────────────────

    [Fact]
    public async Task FindVersionById_ReturnsDocumentDTO()
    {
        const string json = """
            { "id": "v1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindVersionById("posts", "v1");

        Assert.Equal("v1", result.Id);
        Assert.Contains("/api/posts/versions/v1", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── RestoreVersion ──────────────────────────────────────────

    [Fact]
    public async Task RestoreVersion_PostsAndReturnsDocumentDTO()
    {
        const string json = """
            { "id": "v1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.RestoreVersion("posts", "v1");

        Assert.Equal("v1", result.Id);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/posts/versions/v1", handler.LastRequest.RequestUri!.ToString());
    }

    // ── FindGlobalVersions ──────────────────────────────────────

    [Fact]
    public async Task FindGlobalVersions_ReturnsPaginatedDocsDTO()
    {
        const string json = """
            {
              "docs": [{ "id": "gv1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }],
              "totalDocs": 1, "limit": 10, "totalPages": 1, "page": 1,
              "pagingCounter": 1, "hasPrevPage": false, "hasNextPage": false,
              "prevPage": null, "nextPage": null
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindGlobalVersions("site-settings");

        Assert.Single(result.Docs);
        Assert.Contains("/api/globals/site-settings/versions", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── FindGlobalVersionById ───────────────────────────────────

    [Fact]
    public async Task FindGlobalVersionById_ReturnsDocumentDTO()
    {
        const string json = """
            { "id": "gv1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.FindGlobalVersionById("site-settings", "gv1");

        Assert.Equal("gv1", result.Id);
        Assert.Contains("/api/globals/site-settings/versions/gv1", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── RestoreGlobalVersion ────────────────────────────────────

    [Fact]
    public async Task RestoreGlobalVersion_PostsAndReturnsDocumentDTO()
    {
        const string json = """
            { "doc": { "id": "gv1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.RestoreGlobalVersion("site-settings", "gv1");

        Assert.Equal("gv1", result.Id);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/globals/site-settings/versions/gv1", handler.LastRequest.RequestUri!.ToString());
    }

    // ── Login ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_ReturnsLoginResultDTO()
    {
        const string json = """
            {
              "message": "Auth Passed",
              "token": "tok123",
              "exp": 1700000000,
              "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var credentials = new Dictionary<string, object?> { ["email"] = "a@b.com", ["password"] = "pass" };
        var result = await sdk.Login("users", credentials);

        Assert.Equal("tok123", result.Token);
        Assert.Equal(1700000000, result.Exp);
        Assert.Equal("u1", result.User.Id);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/users/login", handler.LastRequest.RequestUri!.ToString());
    }

    // ── Me ──────────────────────────────────────────────────────

    [Fact]
    public async Task Me_ReturnsMeResultDTO()
    {
        const string json = """
            {
              "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" },
              "collection": "users",
              "token": "tok123",
              "exp": 1700000000
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.Me("users");

        Assert.Equal("u1", result.User.Id);
        Assert.Equal("users", result.Collection);
        Assert.Contains("/api/users/me", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── RefreshToken ────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_ReturnsRefreshResultDTO()
    {
        const string json = """
            {
              "message": "Token refresh successful",
              "refreshedToken": "newtok",
              "exp": 1700000999,
              "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.RefreshToken("users");

        Assert.Equal("newtok", result.RefreshedToken);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/users/refresh-token", handler.LastRequest.RequestUri!.ToString());
    }

    // ── ForgotPassword ──────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_ReturnsMessageDTO()
    {
        const string json = """{ "message": "Success" }""";
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["email"] = "a@b.com" };
        var result = await sdk.ForgotPassword("users", data);

        Assert.Equal("Success", result.Message);
        Assert.Contains("/api/users/forgot-password", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── ResetPassword ───────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ReturnsResetPasswordResultDTO()
    {
        const string json = """
            {
              "token": "tok999",
              "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" }
            }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["token"] = "reset-tok", ["password"] = "newpass" };
        var result = await sdk.ResetPassword("users", data);

        Assert.Equal("u1", result.User.Id);
        Assert.Contains("/api/users/reset-password", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── Logout ──────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ReturnsMessageDTO()
    {
        const string json = """{ "message": "You have been logged out successfully." }""";
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var result = await sdk.Logout("users");

        Assert.Equal("You have been logged out successfully.", result.Message);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/api/users/logout", handler.LastRequest.RequestUri!.ToString());
    }

    // ── Unlock ──────────────────────────────────────────────────

    [Fact]
    public async Task Unlock_ReturnsMessageDTO()
    {
        const string json = """{ "message": "Success" }""";
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var data = new Dictionary<string, object?> { ["email"] = "a@b.com" };
        var result = await sdk.Unlock("users", data);

        Assert.Equal("Success", result.Message);
        Assert.Contains("/api/users/unlock", handler.LastRequest!.RequestUri!.ToString());
    }

    // ── Request escape hatch ────────────────────────────────────

    [Fact]
    public async Task Request_Get_ReturnsRawJson()
    {
        const string json = """{ "docs": [] }""";
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var config = new RequestConfig(HttpMethod.Get, "/api/posts");
        var result = await sdk.Request(config);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("docs"));
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task Request_Post_WithBody_SendsJson()
    {
        const string json = """{ "doc": { "id": "x1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" } }""";
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var requestBody = new Dictionary<string, object?> { ["title"] = "Hello" };
        var config = new RequestConfig(HttpMethod.Post, "/api/posts", requestBody);
        var result = await sdk.Request(config);

        Assert.NotNull(result);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        var responseBody = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("Hello", responseBody);
    }

    [Fact]
    public async Task Request_WithQuery_AppendsQueryString()
    {
        const string json = """{ "docs": [] }""";
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        var query = new QueryBuilder().Where("title", Operator.Equals, "Test");
        var config = new RequestConfig(HttpMethod.Get, "/api/posts", null, query);
        await sdk.Request(config);

        Assert.Contains("where", handler.LastRequest!.RequestUri!.Query);
    }

    // ── PayloadError ────────────────────────────────────────────

    [Fact]
    public async Task Find_NonOkResponse_ThrowsPayloadError()
    {
        const string json = """{ "errors": [{ "message": "Not found" }] }""";
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.NotFound, json);

        var exception = await Assert.ThrowsAsync<PayloadError>(() => sdk.Find("posts"));

        Assert.Equal(404, exception.StatusCode);
        Assert.NotEmpty(exception.Result);
    }

    [Fact]
    public async Task Find_NonOkResponseWithNonJsonBody_ThrowsPayloadError()
    {
        // A proxy or gateway can answer with an HTML error page. The status check must
        // run before JSON parsing so this still surfaces as PayloadError, not JsonException.
        const string html = "<html><body>502 Bad Gateway</body></html>";
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.BadGateway, html);

        var exception = await Assert.ThrowsAsync<PayloadError>(() => sdk.Find("posts"));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal(html, exception.Body);
        Assert.Empty(exception.Result);
    }

    [Fact]
    public async Task Login_Unauthorized_ThrowsPayloadError()
    {
        const string json = """{ "errors": [{ "message": "Email or password is incorrect." }] }""";
        var (sdk, _) = SdkFactory.Create(HttpStatusCode.Unauthorized, json);

        var credentials = new Dictionary<string, object?> { ["email"] = "a@b.com", ["password"] = "wrong" };
        var exception = await Assert.ThrowsAsync<PayloadError>(() => sdk.Login("users", credentials));

        Assert.Equal(401, exception.StatusCode);
    }

    // ── Auth credential lifecycle ───────────────────────────────

    [Fact]
    public async Task SetJwtAuth_AddsAuthorizationHeader()
    {
        const string json = """
            { "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" },
              "collection": "users", "token": "t", "exp": 0 }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        sdk.SetJwtAuth(new JwtAuth("tok123"));
        await sdk.Me("users");

        Assert.True(handler.LastRequest!.Headers.Contains("Authorization"));
        Assert.Contains("tok123", handler.LastRequest.Headers.GetValues("Authorization").First());
    }

    [Fact]
    public async Task SetApiKeyAuth_AddsApiKeyHeader()
    {
        const string json = """
            { "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" },
              "collection": "users", "token": "", "exp": 0 }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        sdk.SetApiKeyAuth(new ApiKeyAuth("users", "key-abc"));
        await sdk.Me("users");

        Assert.True(handler.LastRequest!.Headers.Contains("Authorization"));
        var authHeader = handler.LastRequest.Headers.GetValues("Authorization").First();
        Assert.Contains("users", authHeader);
        Assert.Contains("key-abc", authHeader);
    }

    [Fact]
    public async Task ClearAuth_RemovesAuthorizationHeader()
    {
        const string json = """
            { "user": { "id": "u1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z" },
              "collection": "users", "token": "", "exp": 0 }
            """;
        var (sdk, handler) = SdkFactory.Create(HttpStatusCode.OK, json);

        sdk.SetJwtAuth(new JwtAuth("tok123"));
        sdk.ClearAuth();
        await sdk.Me("users");

        Assert.False(handler.LastRequest!.Headers.Contains("Authorization"));
    }

    // ── CancellationToken propagation ───────────────────────────

    [Fact]
    public async Task Find_WithCancelledToken_ThrowsWithAbortedMessage()
    {
        var handler = new MockHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var sdk = new PayloadSDK(httpClient, "http://localhost:3000");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // _Fetch wraps TaskCanceledException into a plain Exception with an "aborted" message
        var exception = await Assert.ThrowsAsync<Exception>(
            () => sdk.Find("posts", cancellationToken: cts.Token));

        Assert.Contains("aborted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

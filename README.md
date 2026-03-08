# Payload CMS HTTP Client

A lightweight HTTP client for the [Payload CMS](https://payloadcms.com/) REST API. Built in C# (.NET 6.0 and .NET 8.0) as part of a cross-language port alongside TypeScript and Dart implementations.

- Typed methods for collections, globals, auth, and versions
- Fluent query builder with where clauses, joins, sorting, and pagination
- File upload support via multipart form data
- API key and JWT authentication
- Custom endpoint escape hatch via `Request()`
- Optional ASP.NET Core DI integration via `AddPayloadSDK()`

## Installation

```bash
dotnet add package PayloadCMS.DotNet
```

## Usage

```csharp
using PayloadCMS.DotNet;

var httpClient = new System.Net.Http.HttpClient();
var sdk = new PayloadSDK(httpClient, "http://localhost:3000");
```

> **Note:** `PayloadSDK` requires an externally managed `System.Net.Http.HttpClient` instance. The caller is responsible for its lifetime and disposal. In ASP.NET Core applications, use `IHttpClientFactory` or the `AddPayloadSDK()` DI extension.

### ASP.NET Core DI

```csharp
// Program.cs
builder.Services.AddPayloadSDK("https://cms.example.com");

// Or with custom HttpClient configuration:
builder.Services.AddPayloadSDK("https://cms.example.com", httpClient =>
{
    httpClient.Timeout = TimeSpan.FromSeconds(30);
});
```

Inject `PayloadSDK` directly into controllers or services — it is registered as a scoped service backed by a named `IHttpClientFactory`-managed `HttpClient`. Each HTTP request gets its own `PayloadSDK` instance, which means `SetJwtAuth()`, `SetApiKeyAuth()`, and `ClearAuth()` are safe to call per-request without affecting other concurrent users.

### Constructor

```csharp
new PayloadSDK(
    System.Net.Http.HttpClient httpClient,
    string baseUrl
)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `httpClient` | `HttpClient` | The HTTP client instance to use. Caller owns the lifetime. |
| `baseUrl` | `string` | Payload CMS instance URL. Trailing slashes are stripped automatically. |

### Set headers

Replaces the custom headers included with every request.

```csharp
void SetHeaders(Dictionary<string, string> headers)
```

### Set API key auth

Sets an API key credential for all subsequent requests.

```csharp
void SetApiKeyAuth(ApiKeyAuth auth)
```

### Set JWT auth

Sets a JWT bearer token credential for all subsequent requests.

```csharp
void SetJwtAuth(JwtAuth auth)
```

### Clear auth

Clears the current authentication credential. Subsequent requests are sent without authorization headers.

```csharp
void ClearAuth()
```

---

## Collections

### Find documents

Retrieves a paginated list of documents.

```csharp
Task<PaginatedDocsDTO> Find(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `query` | `QueryBuilder?` | Optional query parameters (where, sort, limit, etc.). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
PaginatedDocsDTO result = await client.Find("posts");

// result.Docs        — List<DocumentDTO>
// result.TotalDocs   — 42
// result.TotalPages  — 5
// result.Page        — 1
// result.Limit       — 10
// result.HasNextPage — true
// result.HasPrevPage — false
```

### Find by ID

Retrieves a single document by ID.

```csharp
Task<DocumentDTO> FindById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `id` | `string` | Document ID. |
| `query` | `QueryBuilder?` | Optional query parameters. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
DocumentDTO document = await client.FindById("posts", "123");

// document.Id        — "123"
// document.Json      — Dictionary<string, object?> with full payload
// document.CreatedAt — DateTime?
// document.UpdatedAt — DateTime?
```

### Count

Returns the total count of documents matching an optional query.

```csharp
Task<int> Count(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `query` | `QueryBuilder?` | Optional query parameters to filter the count. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
int total = await client.Count("posts");

// total — 42
```

### Create

Creates a new document. Supports file uploads on upload-enabled collections.

```csharp
Task<DocumentDTO> Create(string slug, Dictionary<string, object?> data, FileUpload? file = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `data` | `Dictionary<string, object?>` | Document data. |
| `file` | `FileUpload?` | Optional file to upload (for upload-enabled collections). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
DocumentDTO document = await client.Create("posts", new Dictionary<string, object?>
{
    ["title"] = "Hello World",
    ["content"] = "My first post.",
});

// document.Id   — "abc123"
// document.Json — Dictionary containing id, title, content, etc.
```

#### File Uploads

```csharp
new FileUpload(byte[] content, string fileName, string? mimeType = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `content` | `byte[]` | The file content. |
| `fileName` | `string` | The filename (including extension). |
| `mimeType` | `string?` | Optional MIME type (e.g. `image/png`). |

#### Example
```csharp
using PayloadCMS.DotNet.Upload;

var file = new FileUpload(
    content: File.ReadAllBytes("photo.png"),
    fileName: "photo.png",
    mimeType: "image/png"
);

DocumentDTO document = await client.Create("media", new Dictionary<string, object?>
{
    ["alt"] = "My image",
}, file);
```

### Update by ID

Updates a single document by ID. Supports file replacement.

```csharp
Task<DocumentDTO> UpdateById(string slug, string id, Dictionary<string, object?> data, FileUpload? file = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `id` | `string` | Document ID. |
| `data` | `Dictionary<string, object?>` | Fields to update. |
| `file` | `FileUpload?` | Optional replacement file. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
DocumentDTO document = await client.UpdateById("posts", "123", new Dictionary<string, object?>
{
    ["title"] = "Updated Title",
});
```

### Bulk update

Bulk-updates all documents matching a query. Supports file uploads.

```csharp
Task<PaginatedDocsDTO> Update(string slug, Dictionary<string, object?> data, QueryBuilder query, FileUpload? file = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `data` | `Dictionary<string, object?>` | Fields to update on all matching documents. |
| `query` | `QueryBuilder` | Query to select documents to update. |
| `file` | `FileUpload?` | Optional file to upload. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var query = new QueryBuilder()
    .Where("status", Operator.Equals, "draft");

PaginatedDocsDTO result = await client.Update("posts", new Dictionary<string, object?>
{
    ["status"] = "published",
}, query);
```

### Delete by ID

Deletes a single document by ID.

```csharp
Task<DocumentDTO> DeleteById(string slug, string id, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `id` | `string` | Document ID. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
DocumentDTO document = await client.DeleteById("posts", "123");
```

### Bulk delete

Bulk-deletes all documents matching a query.

```csharp
Task<PaginatedDocsDTO> Delete(string slug, QueryBuilder query, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `query` | `QueryBuilder` | Query to select documents to delete. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var query = new QueryBuilder()
    .Where("status", Operator.Equals, "archived");

PaginatedDocsDTO result = await client.Delete("posts", query);
```

---

## Globals

### Find global

Retrieves a global document.

```csharp
Task<DocumentDTO> FindGlobal(string slug, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await client.FindGlobal("site-settings");
```

### Update global

Updates a global document.

```csharp
Task<DocumentDTO> UpdateGlobal(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await client.UpdateGlobal("site-settings", new Dictionary<string, object?>
{
    ["siteName"] = "My Site",
});
```

---

## Authentication

### Login

Authenticates a user and returns a JWT token.

```csharp
Task<LoginResultDTO> Login(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `data` | `Dictionary<string, object?>` | Credentials (e.g. `{ email, password }`). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
LoginResultDTO result = await client.Login("users", new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
    ["password"] = "secret",
});

// result.Token   — "eyJhbGciOi..."
// result.Exp     — 1700000000
// result.User    — DocumentDTO
// result.Message — "Authentication Passed"
```

### Me

Retrieves the currently authenticated user.

```csharp
Task<MeResultDTO> Me(string slug, CancellationToken cancellationToken = default)
```

#### Example
```csharp
MeResultDTO me = await client.Me("users");

// me.User       — DocumentDTO
// me.Token      — "eyJhbGciOi..."
// me.Exp        — 1700000000
// me.Collection — "users"
// me.Strategy   — "local-jwt"
```

### Refresh token

Refreshes the current JWT token.

```csharp
Task<RefreshResultDTO> RefreshToken(string slug, CancellationToken cancellationToken = default)
```

#### Example
```csharp
RefreshResultDTO result = await client.RefreshToken("users");

// result.RefreshedToken — "eyJhbGciOi..."
// result.Exp            — 1700003600
// result.User           — DocumentDTO
```

### Forgot password

Initiates the forgot-password flow.

```csharp
Task<MessageDTO> ForgotPassword(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

#### Example
```csharp
MessageDTO result = await client.ForgotPassword("users", new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
});

// result.Message — "Success"
```

### Reset password

Completes a password reset using a reset token.

```csharp
Task<ResetPasswordResultDTO> ResetPassword(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

#### Example
```csharp
ResetPasswordResultDTO result = await client.ResetPassword("users", new Dictionary<string, object?>
{
    ["token"] = "reset-token",
    ["password"] = "newPassword123",
});

// result.User  — DocumentDTO
// result.Token — "eyJhbGciOi..."
```

### Verify email

Verifies a user's email address.

```csharp
Task<MessageDTO> VerifyEmail(string slug, string token, CancellationToken cancellationToken = default)
```

#### Example
```csharp
MessageDTO result = await client.VerifyEmail("users", "verification-token");

// result.Message — "Email verified successfully."
```

### Logout

Logs out the currently authenticated user.

```csharp
Task<MessageDTO> Logout(string slug, CancellationToken cancellationToken = default)
```

#### Example
```csharp
MessageDTO result = await client.Logout("users");
```

### Unlock

Unlocks a user account that has been locked due to failed login attempts.

```csharp
Task<MessageDTO> Unlock(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

#### Example
```csharp
MessageDTO result = await client.Unlock("users", new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
});
```

### JWT Authentication

```csharp
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Config;

var sdk = new PayloadSDK(httpClient, "http://localhost:3000");

// Login to get a token
LoginResultDTO loginResult = await sdk.Login("users", new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
    ["password"] = "secret",
});

// Set the token on the client
sdk.SetJwtAuth(new JwtAuth(loginResult.Token!));

// Authenticated requests now include the Bearer token
MeResultDTO me = await sdk.Me("users");
```

### API Key Authentication

```csharp
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Config;

var sdk = new PayloadSDK(httpClient, "http://localhost:3000");
sdk.SetApiKeyAuth(new ApiKeyAuth("users", "your-api-key-here"));
```

#### `ApiKeyAuth`

Sets the `Authorization` header to `{collectionSlug} API-Key {apiKey}`.

```csharp
new ApiKeyAuth(string collectionSlug, string apiKey)
```

#### `JwtAuth`

Sets the `Authorization` header to `Bearer {token}`.

```csharp
new JwtAuth(string token)
```

Use `SetApiKeyAuth()` or `SetJwtAuth()` to apply credentials to the client, or `ClearAuth()` to remove them.

---

## Versions

### Find versions

Retrieves a paginated list of versions for a collection.

```csharp
Task<PaginatedDocsDTO> FindVersions(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
PaginatedDocsDTO result = await client.FindVersions("posts");
```

### Find version by ID

Retrieves a single version by ID.

```csharp
Task<DocumentDTO> FindVersionById(string slug, string id, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await client.FindVersionById("posts", "version-id");
```

### Restore version

Restores a collection document to a specific version.

```csharp
Task<DocumentDTO> RestoreVersion(string slug, string id, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await client.RestoreVersion("posts", "version-id");
```

### Find global versions

Retrieves a paginated list of versions for a global.

```csharp
Task<PaginatedDocsDTO> FindGlobalVersions(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
PaginatedDocsDTO result = await client.FindGlobalVersions("site-settings");
```

### Find global version by ID

Retrieves a single global version by ID.

```csharp
Task<DocumentDTO> FindGlobalVersionById(string slug, string id, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await client.FindGlobalVersionById("site-settings", "version-id");
```

### Restore global version

Restores a global document to a specific version.

```csharp
Task<DocumentDTO> RestoreGlobalVersion(string slug, string id, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await client.RestoreGlobalVersion("site-settings", "version-id");
```

---

## Custom Endpoints

Escape hatch for custom endpoints. Returns raw JSON instead of a DTO.

```csharp
Task<Dictionary<string, object?>?> Request(RequestConfig config, CancellationToken cancellationToken = default)
```

`RequestConfig` is a record that groups all request options:

```csharp
new RequestConfig(
    Method: System.Net.Http.HttpMethod method,
    Path: string path,
    Body: Dictionary<string, object?>? body = null,
    Query: QueryBuilder? query = null
)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `Method` | `System.Net.Http.HttpMethod` | HTTP method (e.g. `HttpMethod.Get`, `HttpMethod.Post`). |
| `Path` | `string` | URL path appended to base URL (e.g. `/api/custom-endpoint`). |
| `Body` | `Dictionary<string, object?>?` | Optional JSON request body. |
| `Query` | `QueryBuilder?` | Optional query parameters. |

#### Example
```csharp
using PayloadCMS.DotNet.Config;

Dictionary<string, object?>? result = await sdk.Request(new RequestConfig(
    Method: HttpMethod.Post,
    Path: "/api/custom-endpoint",
    Body: new Dictionary<string, object?> { ["key"] = "value" }
));
```

---

## Querying

### QueryBuilder

Fluent builder for query parameters. All methods return `this` for chaining.

#### Example
```csharp
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Enums;

var query = new QueryBuilder()
    .Where("status", Operator.Equals, "published")
    .Sort("createdAt")
    .Limit(10)
    .Page(2);

PaginatedDocsDTO result = await client.Find("posts", query);

// Serializes to: ?where[status][equals]=published&sort=createdAt&limit=10&page=2
```

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Limit` | `int value` | Maximum documents per page. |
| `Page` | `int value` | Page number. |
| `Sort` | `string field` | Sort ascending by field. |
| `SortByDescending` | `string field` | Sort descending by field. |
| `Depth` | `int value` | Population depth for relationships. |
| `Locale` | `string value` | Locale for localized fields. |
| `FallbackLocale` | `string value` | Fallback locale. |
| `Select` | `string[] fields` | Fields to include in response. |
| `Populate` | `string[] fields` | Relationships to populate. |
| `Where` | `string field, Operator op, object? value` | Add a where condition. |
| `And` | `Action<WhereBuilder> callback` | Nested AND group. |
| `Or` | `Action<WhereBuilder> callback` | Nested OR group. |
| `Join` | `Action<JoinBuilder> callback` | Configure joins. |

### WhereBuilder

Used inside `And()` and `Or()` callbacks to compose nested where clauses.

#### Example
```csharp
var query = new QueryBuilder()
    .Where("status", Operator.Equals, "published")
    .Or(builder =>
    {
        builder
            .Where("category", Operator.Equals, "news")
            .Where("category", Operator.Equals, "blog");
    });

// Serializes to: ?where[status][equals]=published&where[or][0][category][equals]=news&where[or][1][category][equals]=blog
```

Nested AND groups work the same way:

```csharp
var query = new QueryBuilder()
    .Where("status", Operator.Equals, "published")
    .And(builder =>
    {
        builder
            .Where("views", Operator.GreaterThan, 100)
            .Where("featured", Operator.Equals, true);
    });
```

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Where` | `string field, Operator op, object? value` | Add a where condition. |
| `And` | `Action<WhereBuilder> callback` | Nested AND group. |
| `Or` | `Action<WhereBuilder> callback` | Nested OR group. |

### JoinBuilder

Used inside the `Join()` callback to configure relationship joins.

#### Example
```csharp
var query = new QueryBuilder()
    .Join(join =>
    {
        join
            .Limit("comments", 5)
            .Sort("comments", "createdAt")
            .Where("comments", "status", Operator.Equals, "approved");
    });

PaginatedDocsDTO result = await client.Find("posts", query);
```

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Limit` | `string on, int value` | Limit documents for a join field. |
| `Page` | `string on, int value` | Page number for a join field. |
| `Sort` | `string on, string field` | Sort ascending by field. |
| `SortByDescending` | `string on, string field` | Sort descending by field. |
| `Count` | `string on, bool? value = null` | Enable/disable counting. |
| `Where` | `string on, string field, Operator op, object? value` | Where condition on a join field. |
| `And` | `string on, Action<WhereBuilder> callback` | Nested AND group on a join field. |
| `Or` | `string on, Action<WhereBuilder> callback` | Nested OR group on a join field. |
| `Disable` | — | Disable all joins. |
| `IsDisabled` | — | (getter) Whether joins are disabled. |

---

## DTOs

The included DTOs represent the **lowest common denominator** of a Payload CMS response. Because Payload collections are schema-defined by the consumer, this library cannot know the shape of your documents at compile time. Instead, `DocumentDTO` captures the universal fields (`Id`, `CreatedAt`, `UpdatedAt`) and exposes the full response as a raw `Dictionary<string, object?>`.

These DTOs are **not intended to be your final domain models**. They serve as a transport-level representation that you should map into richer, typed models in your own application.

A convenient pattern is to write a `DocumentDTO` extension method using `System.Text.Json` — property names are matched case-insensitively by default, and `[JsonPropertyName]` can be used for explicit mappings:

```csharp
using System.Text.Json;
using PayloadCMS.DotNet.Models.Collection;

public static class DocumentDTOExtensions
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public static T As<T>(this DocumentDTO dto) where T : new()
    {
        var json = JsonSerializer.Serialize(dto.Json);
        return JsonSerializer.Deserialize<T>(json, _options) ?? new T();
    }
}
```

Define your domain model — no attributes needed for fields whose names match the CMS field names (case-insensitively), and use `[JsonPropertyName]` only where an explicit mapping is required:

```csharp
public class BlogPost
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}
```

Then map from the DTO in a single call:

```csharp
DocumentDTO dto = await client.FindById("posts", "123");
BlogPost post = dto.As<BlogPost>();

// Works the same for paginated results:
PaginatedDocsDTO result = await client.Find("posts");
List<BlogPost> posts = result.Docs.Select(doc => doc.As<BlogPost>()).ToList();
```

### DocumentDTO

Returned by single-document operations (`Create`, `FindById`, `UpdateById`, `DeleteById`, globals, versions).

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Document ID. |
| `Json` | `Dictionary<string, object?>` | The full raw JSON payload. |
| `CreatedAt` | `DateTime?` | Creation timestamp. |
| `UpdatedAt` | `DateTime?` | Last update timestamp. |

### PaginatedDocsDTO

Returned by paginated operations (`Find`, `Update`, `Delete`, `FindVersions`).

| Property | Type | Description |
|----------|------|-------------|
| `Docs` | `List<DocumentDTO>` | List of documents. |
| `TotalDocs` | `int` | Total matching documents. |
| `TotalPages` | `int` | Total pages. |
| `Page` | `int?` | Current page. |
| `Limit` | `int` | Documents per page. |
| `HasNextPage` | `bool` | Whether a next page exists. |
| `HasPrevPage` | `bool` | Whether a previous page exists. |
| `NextPage` | `int?` | Next page number. |
| `PrevPage` | `int?` | Previous page number. |

### Auth DTOs

| DTO | Returned by | Properties |
|-----|-------------|------------|
| `LoginResultDTO` | `Login()` | `Token`, `Exp`, `User` (DocumentDTO), `Message` |
| `MeResultDTO` | `Me()` | `User`, `Token`, `Exp`, `Collection`, `Strategy` |
| `RefreshResultDTO` | `RefreshToken()` | `RefreshedToken`, `Exp`, `User` |
| `ResetPasswordResultDTO` | `ResetPassword()` | `User`, `Token` |
| `MessageDTO` | `ForgotPassword()`, `VerifyEmail()`, `Logout()`, `Unlock()` | `Message` |

---

## Error Handling

`PayloadError` is thrown when a Payload CMS API request fails with a non-2xx status code.

```csharp
public class PayloadError : Exception
{
    public readonly int StatusCode;
    public readonly HttpResponseMessage? Response;
    public readonly object? Cause;
}
```

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `int` | HTTP status code. |
| `Response` | `HttpResponseMessage?` | The originating HTTP response. |
| `Message` | `string` | Error message (from `Exception`). |
| `Cause` | `object?` | The parsed JSON error body (if available). |

```csharp
using PayloadCMS.DotNet;

try
{
    DocumentDTO document = await sdk.FindById("posts", "nonexistent");
}
catch (PayloadError ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");
    Console.WriteLine($"Message: {ex.Message}");
    // ex.Cause contains the parsed JSON error body if the server returned one
}
catch (Exception ex)
{
    // Network failure, timeout, or parsing error
}
```

---

## Types

### Type mappings

This library uses standard .NET types throughout. The mapping from the TypeScript version is:

| TypeScript | C# |
|---|---|
| `Json` / `JsonObject` | `Dictionary<string, object?>` |
| `JsonValue` | `object?` |
| `JsonArray` | `List<object?>` |
| `string \| undefined` | `string?` |
| `number` | `int` |
| `boolean` | `bool` |
| `Date` | `DateTime` |
| `Promise<T>` | `Task<T>` |
| `Record<string, string>` | `Dictionary<string, string>` |

### Operator

All supported Payload CMS where operators:

```csharp
public enum Operator
{
    Equals, 
    NotEquals, 
    Contains, 
    Like, 
    NotLike,
    In, 
    NotIn, 
    All, 
    Exists,
    GreaterThan, 
    GreaterThanEqual, 
    LessThan, 
    LessThanEqual,
    Within, 
    Intersects, 
    Near,
}
```

# Payload CMS .NET SDK

A lightweight, strongly typed C# SDK for interacting with the [Payload CMS](https://payloadcms.com/) REST API. This library handles HTTP communication, authentication, query construction, and response parsing. Requires .NET 8.0 or later. 

## Features

- Typed methods for collections, globals, auth, and versions
- Fluent query builder with where clauses, joins, sorting, and pagination
- File upload support via multipart form data
- API key and JWT authentication

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
PaginatedDocsDTO result = await sdk.Find("posts");

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
DocumentDTO document = await sdk.FindById("posts", "123");

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
int total = await sdk.Count("posts");

// total — 42
```

### Create

Creates a new document. Supports file uploads on upload-enabled collections.

```csharp
Task<DocumentDTO> Create(string slug, Dictionary<string, object?> data, QueryBuilder? query = null, FileUpload? file = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `data` | `Dictionary<string, object?>` | Document data. |
| `query` | `QueryBuilder?` | Optional write-time params — e.g. `Locale`, `Depth`, `Limit` etc. |
| `file` | `FileUpload?` | Optional file to upload (for upload-enabled collections). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["title"] = "Hello World",
    ["content"] = "My first post.",
};

DocumentDTO document = await sdk.Create("posts", data);

// document.Id   — "abc123"
// document.Json — Dictionary containing id, title, content, etc.
```

#### File Uploads

// Add description

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

var data = new Dictionary<string, object?>
{
    ["alt"] = "My image",
};

DocumentDTO document = await sdk.Create("media", data, file: file);
```

### Update by ID

Updates a single document by ID. Supports file replacement.

```csharp
Task<DocumentDTO> UpdateById(string slug, string id, Dictionary<string, object?> data, QueryBuilder? query = null, FileUpload? file = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `id` | `string` | Document ID. |
| `data` | `Dictionary<string, object?>` | Fields to update. |
| `query` | `QueryBuilder?` | Optional write-time params — e.g. `Draft(true)` to save the edit as a draft version. |
| `file` | `FileUpload?` | Optional replacement file. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["title"] = "Updated Title",
};

DocumentDTO document = await sdk.UpdateById("posts", "123", data);
```

### Bulk update

Bulk-updates all documents matching a query. Supports file uploads.

```csharp
Task<BulkOperationDTO> Update(string slug, Dictionary<string, object?> data, QueryBuilder query, FileUpload? file = null, CancellationToken cancellationToken = default)
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
    .Where("id", Operator.Equals, "123");

var data = new Dictionary<string, object?>
{
    ["title"] = "New title",
};

BulkOperationDTO result = await sdk.Update("posts", data, query);
```

### Delete by ID

Deletes a single document by ID.

```csharp
Task<DocumentDTO> DeleteById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Collection slug. |
| `id` | `string` | Document ID. |
| `query` | `QueryBuilder?` | Optional write-time params — e.g. `Trash(true)` to permanently delete an already soft-deleted document. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
DocumentDTO document = await sdk.DeleteById("posts", "123");
```

### Bulk delete

Bulk-deletes all documents matching a query.

```csharp
Task<BulkOperationDTO> Delete(string slug, QueryBuilder query, CancellationToken cancellationToken = default)
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

BulkOperationDTO result = await sdk.Delete("posts", query);
```

## Draft, Trash & Autosave

`Draft`, `Trash`, and `Autosave` correspond to optional Payload features. Because these features are configured per collection, the SDK cannot determine whether they are enabled on the collection you're working with.

The three methods are available through PayloadCMS.DotNet.Extensions:

```csharp
using PayloadCMS.DotNet.Extensions;
```

| Method | Parameters | Description |
|--------|-----------|-------------|
| Draft	| bool value	| Selects the draft version on reads and saves changes as a draft on writes.
| Trash	| bool value	| Includes soft-deleted documents in the operation.
| Autosave	| bool value	| Marks a write as an autosave.

> **Note:** These methods depend on the corresponding feature being enabled in Payload. The SDK does not validate the collection configuration, so using a method where the feature is not enabled may have no effect. In particular, Draft(true) on a collection without drafts enabled does not cause an error and may result in a normal write.

### Draft

`Draft()` controls which version of a document is used. It is not a **visibility filter**.

`Draft(false)` (or omitting `Draft()`) uses the latest published version, while `Draft(true)` uses the most recent version, including unpublished changes.

On writes, `Draft(true)` saves the change as a draft without changing the published version. The same option can be used with both reads and writes.

#### Example
```csharp
// Save an edit as a draft — the published document is unchanged
var query = new QueryBuilder().Draft(true);

var data = new Dictionary<string, object?>
{
    ["title"] = "Work-in-progress title",
};

await sdk.UpdateById("posts", "123", data, query);
```

```csharp
// Read the published version
DocumentDTO published = await sdk.FindById("posts", "123");
```

```csharp
// Read the latest version, including the draft
var query = new QueryBuilder().Draft(true);

DocumentDTO draft = await sdk.FindById("posts", "123", query);
```

```csharp
// Publish the draft
var data = new Dictionary<string, object?>
{
    ["_status"] = "published"
};

await sdk.UpdateById("posts", "123", data);
```

If you need to filter documents by their publication status, use a `where` condition on `_status` rather than `Draft()`.

### Trash

`Trash(true)` includes soft-deleted documents in an operation. It does **not** perform the soft-delete itself. To soft-delete a document, update its `deletedAt` field.

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["deletedAt"] = DateTime.UtcNow
};

await sdk.UpdateById("posts", "123", data);
```

Once a document has been soft-deleted, `Trash(true)` is required when retrieving or deleting it.

#### Example
```csharp
var query = new QueryBuilder().Trash(true);

await sdk.DeleteById("posts", "123", query);
```

When performing bulk operations, take care when combining `Trash(true)` with broad `where` conditions: both trashed and non-trashed documents may be included.

### Autosave

`Autosave(true)` marks a write as an autosave. It is only meaningful when Payload's autosave functionality is enabled for the collection.

#### Example
```csharp
var query = new QueryBuilder().Autosave(true);

var data = new Dictionary<string, object?>
{
    ["title"] = "Work-in-progress title"
};

await sdk.UpdateById("posts", "123", data, query);
```

## Globals

### Find global

Retrieves a global document.

```csharp
Task<DocumentDTO> FindGlobal(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await sdk.FindGlobal("site-settings");

var query = new QueryBuilder().Depth(1).Locale("sv");

DocumentDTO localized = await sdk.FindGlobal("site-settings", query);
```

### Update global

Updates a global document.

```csharp
Task<DocumentDTO> UpdateGlobal(string slug, Dictionary<string, object?> data, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["siteName"] = "My Site",
};

DocumentDTO document = await sdk.UpdateGlobal("site-settings", data);
```

## Authentication

Payload supports several authentication mechanisms, and this library provides the corresponding tools for configuring authentication on outgoing requests. Authentication can be configured when creating the `PayloadSDK`, changed later with `SetApiKeyAuth()` or `SetJwtAuth()`, or removed with `ClearAuth()`.

For authentication mechanisms not directly covered by the SDK, use `SetHeaders()` to supply the required headers.

### API key

Sets an API key credential for all subsequent requests.

```csharp
void SetApiKeyAuth(ApiKeyAuth auth)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `auth` | `ApiKeyAuth` | API key credential, sets the `Authorization` header to `{collectionSlug} API-Key {apiKey}`. |

#### Example
```csharp
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Config;

var sdk = new PayloadSDK(httpClient, "http://localhost:3000");

// Create an instance of ApiKeyAuth with your API key
var auth = new ApiKeyAuth("users", "your-api-key-here");

// Set the API key on the client
sdk.SetApiKeyAuth(auth);
```

### JWT

Sets a JWT bearer token credential for all subsequent requests.

```csharp
void SetJwtAuth(JwtAuth auth)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `auth` | `JwtAuth` | JWT bearer token credential, sets the `Authorization` header to `Bearer {token}`. |

#### Example
```csharp
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Config;

var sdk = new PayloadSDK(httpClient, "http://localhost:3000");

var data = new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
    ["password"] = "secret",
};

// Login to get a token
LoginResultDTO loginResult = await sdk.Login("users", data);

// Create an instance of JwtAuth with the token
var auth = new JwtAuth(loginResult.Token!);

// Set the token on the client
sdk.SetJwtAuth(auth);
```

### Clearing authentication

Clears the current authentication credential. Subsequent requests are sent without authorization headers.

```csharp
void ClearAuth()
```

### Custom headers

Replaces the custom headers included with every request.

```csharp
void SetHeaders(Dictionary<string, string> headers)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `headers` | `Dictionary<string, string>` | Headers to include with every subsequent request. |

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
var data = new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
    ["password"] = "secret",
};

LoginResultDTO result = await sdk.Login("users", data);

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

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
MeResultDTO me = await sdk.Me("users");

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

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
RefreshResultDTO result = await sdk.RefreshToken("users");

// result.RefreshedToken — "eyJhbGciOi..."
// result.Exp            — 1700003600
// result.User           — DocumentDTO
```

### Forgot password

Initiates the forgot-password flow.

```csharp
Task<MessageDTO> ForgotPassword(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `data` | `Dictionary<string, object?>` | Credentials (e.g. `{ email }`). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
};

MessageDTO result = await sdk.ForgotPassword("users", data);

// result.Message — "Success"
```

### Reset password

Completes a password reset using a reset token.

```csharp
Task<ResetPasswordResultDTO> ResetPassword(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `data` | `Dictionary<string, object?>` | Reset data (e.g. `{ token, password }`). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["token"] = "reset-token",
    ["password"] = "newPassword123",
};

ResetPasswordResultDTO result = await sdk.ResetPassword("users", data);

// result.User  — DocumentDTO
// result.Token — "eyJhbGciOi..."
```

### Verify email

Verifies a user's email address.

```csharp
Task<MessageDTO> VerifyEmail(string slug, string token, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `token` | `string` | Email verification token. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
MessageDTO result = await sdk.VerifyEmail("users", "verification-token");

// result.Message — "Email verified successfully."
```

### Logout

Logs out the currently authenticated user.

```csharp
Task<MessageDTO> Logout(string slug, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
MessageDTO result = await sdk.Logout("users");

// result.Message — "Logout successful."
```

### Unlock

Unlocks a user account that has been locked due to failed login attempts.

```csharp
Task<MessageDTO> Unlock(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | `string` | Auth-enabled collection slug. |
| `data` | `Dictionary<string, object?>` | Credentials (e.g. `{ email }`). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

#### Example
```csharp
var data = new Dictionary<string, object?>
{
    ["email"] = "user@example.com",
};

MessageDTO result = await sdk.Unlock("users", data);

// result.Message — "Success"
```

## Versions

### Find versions

Retrieves a paginated list of versions for a collection.

```csharp
Task<PaginatedDocsDTO> FindVersions(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
PaginatedDocsDTO result = await sdk.FindVersions("posts");
```

### Find version by ID

Retrieves a single version by ID.

```csharp
Task<DocumentDTO> FindVersionById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await sdk.FindVersionById("posts", "version-id");

var query = new QueryBuilder().Trash(true);

DocumentDTO trashed = await sdk.FindVersionById("posts", "version-id", query);
```

### Restore version

Restores a collection document to a specific version.

```csharp
Task<DocumentDTO> RestoreVersion(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await sdk.RestoreVersion("posts", "version-id");
```

### Find global versions

Retrieves a paginated list of versions for a global.

```csharp
Task<PaginatedDocsDTO> FindGlobalVersions(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
PaginatedDocsDTO result = await sdk.FindGlobalVersions("site-settings");
```

### Find global version by ID

Retrieves a single global version by ID.

```csharp
Task<DocumentDTO> FindGlobalVersionById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await sdk.FindGlobalVersionById("site-settings", "version-id");
```

### Restore global version

Restores a global document to a specific version.

```csharp
Task<DocumentDTO> RestoreGlobalVersion(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
```

#### Example
```csharp
DocumentDTO document = await sdk.RestoreGlobalVersion("site-settings", "version-id");
```

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

## Querying

The SDK handles querying through the `QueryBuilder` class. The `QueryBuilder` provides a fluent API for constructing the query parameters used by Payload's REST API. Use it to control pagination and sorting, select or exclude fields, populate relationships, filter documents with where conditions, and configure joins.

A query can combine these options freely, and the same `QueryBuilder` is used across the SDK's read and write operations that accept query parameters. The builder takes care of translating the C# API into Payload's query-string format.

All methods of the `QueryBuilder` return the builder itself, allowing query options to be chained together.

```csharp
using PayloadCMS.DotNet.Enums;
using PayloadCMS.DotNet.Query;

var query = new QueryBuilder()
    .Where("status", Operator.Equals, "published")
    .Sort("createdAt")
    .Limit(10)
    .Page(2);

PaginatedDocsDTO result = await sdk.Find("posts", query);

// Serializes to: ?limit=10&page=2&sort=createdAt&where[status][equals]=published
```

### Limit

Limits the number of documents returned per page.

Use `Limit()` to control the size of each page of results, or together with `Sort()` when you only need a limited set of ordered documents.

```csharp
QueryBuilder Limit(int value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `int` | Maximum documents per page. |

#### Example
```csharp
// The 5 most recent posts
var query = new QueryBuilder()
    .SortByDescending("createdAt")
    .Limit(5);

// Serializes to: ?limit=5&sort=-createdAt
```

### Page

Selects which page of results to return, based on the current `Limit`. Pages are 1-based, so `Page(1)` is the first page.

```csharp
QueryBuilder Page(int value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `int` | Page number (1-based). |

#### Example
```csharp
// Documents 21–30
var query = new QueryBuilder()
    .Limit(10)
    .Page(3);

PaginatedDocsDTO result = await sdk.Find("posts", query);

// result.Page        — 3
// result.HasNextPage — true / false
// result.TotalPages  — total pages available

// Serializes to: ?limit=10&page=3
```

### Pagination

Toggles pagination for the query. When pagination is disabled, matching documents are returned in a single response rather than split into pages.

An explicit `Limit()` is still respected, allowing you to disable pagination while limiting the number of documents returned. Disabling pagination also avoids the additional count query normally used to calculate pagination totals.

```csharp
QueryBuilder Pagination(bool value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `bool` | `false` disables pagination and returns every match. |

#### Example
```csharp
// Every published post, in one response
var query = new QueryBuilder()
    .Where("status", Operator.Equals, "published")
    .Pagination(false);

// Skip the count query, but still cap the result set
var capped = new QueryBuilder()
    .Pagination(false)
    .Limit(100);

// Serializes to: ?limit=100&pagination=false
```

### Sort

Orders results by a field, ascending. Call it more than once to sort by several fields — the calls
accumulate left to right, so the first is the primary sort.

```csharp
QueryBuilder Sort(string field)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `field` | `string` | Field name to sort by, ascending. |

#### Example
```csharp
// Category first, then title within each category
var query = new QueryBuilder()
    .Sort("category")
    .Sort("title");

// Serializes to: ?sort=category,title
```

### SortByDescending

Same as `Sort()` but in reverse order. The `-` prefix Payload expects is added for you, and passing a field that already has one is safe.

Mixes freely with `Sort()` for multi-field ordering.

```csharp
QueryBuilder SortByDescending(string field)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `field` | `string` | Field name to sort by, descending. |

#### Example
```csharp
// Featured items first, then newest within each group
var query = new QueryBuilder()
    .SortByDescending("featured")
    .SortByDescending("createdAt");

// Serializes to: ?sort=-featured,-createdAt
```

### Depth

Controls how deeply relationships are resolved.

At depth `0`, relationship fields contain their document IDs. At depth `1`, related documents are populated; increasing the depth allows relationships within those documents to be populated as well.

Because the shape of a relationship field depends on the requested depth, code consuming the result should account for the corresponding value type.

```csharp
QueryBuilder Depth(int value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `int` | Levels of relationships to resolve. `0` returns bare IDs. |

#### Example
```csharp
// author comes back as a full user document rather than an id
var query = new QueryBuilder()
    .Depth(1);

DocumentDTO post = await sdk.FindById("posts", "123", query);

// Serializes to: ?depth=1
```

### Select

Use `Select()` to return only the fields that you specify, instead of the complete document. This is the main tool for trimming the size of the response that gets returned by the query.

Field names can use dot notation to reach nested fields. For example, `"group.number"` is expanded into the nested structure Payload expects.

```csharp
QueryBuilder Select(string[] fields)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `fields` | `string[]` | Field names to include. Supports dot notation for nested fields. |

#### Example
```csharp
// A lightweight listing — just what the UI renders
var query = new QueryBuilder()
    .Select(new[] { "title", "createdAt" });

// Serializes to: ?select[title]=true&select[createdAt]=true

// Nested fields via dot notation
var nested = new QueryBuilder()
    .Select(new[] { "title", "group.number" });

// Serializes to: ?select[title]=true&select[group][number]=true
```

### Exclude

The inverse of `Select()`, returns all fields *except* those you specify.

Like `Select()`, field names can use dot notation to target nested fields. `Exclude()` can also be combined with `Select()` in the same query.

```csharp
QueryBuilder Exclude(string[] fields)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `fields` | `string[]` | Field names to exclude. Supports dot notation for nested fields. |

#### Example
```csharp
// Everything except the heavy content field
var query = new QueryBuilder()
    .Exclude(new[] { "content" });

// Combined with Select
var mixed = new QueryBuilder()
    .Select(new[] { "title", "group.number" })
    .Exclude(new[] { "content" });
```

### Populate

Controls which fields are returned when *related* documents are populated.

For example, you can use `Depth(1)` to populate an author's document and `Populate()` to return only the author's name rather than the complete user document.

The three methods `Depth()`, `Select()` and `Populate()` serve different purposes: `Select()` controls the fields returned by the documents being queried, `Populate()` controls the fields returned by related documents, and `Depth()` controls whether those related documents are populated at all.

`Populate()` requires a depth of at least `1`.

The `collection` parameter is the slug of the target collection, not the name of the relationship field.

If the target collection has a `defaultPopulate` configuration, `Populate()` replaces that configuration rather than merging with it.

```csharp
QueryBuilder Populate(string collection, string[] fields)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `collection` | `string` | Slug of the related collection being populated. |
| `fields` | `string[]` | Field names to return on those related documents. Supports dot notation. |

#### Example
```csharp
// Posts with their author resolved, but only the author's name
var query = new QueryBuilder()
    .Depth(1)
    .Populate("users", new[] { "name" });

PaginatedDocsDTO result = await sdk.Find("posts", query);

// Serializes to: ?depth=1&populate[users][name]=true
```

### Locale

Requests localized fields in the specified locale.

Use `"all"` to return values for all available locales.

```csharp
QueryBuilder Locale(string value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `string` | Locale code (e.g. `"en"`, `"sv"`), or `"all"` for every locale. |

#### Example
```csharp
var query = new QueryBuilder()
    .Locale("sv");

// Serializes to: ?locale=sv
```

### FallbackLocale

Controls which locale to use when a field has no value in the requested locale.

Pass a locale code to specify the fallback locale, or `"false"` to disable fallback. Disabling fallback allows missing localized values to remain `null` rather than being supplied from another locale.

```csharp
QueryBuilder FallbackLocale(string value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `string` | Locale code to fall back to, or `"false"` to disable fallback entirely. |

#### Example
```csharp
// Swedish only — untranslated fields come back null instead of silently showing English
var query = new QueryBuilder()
    .Locale("sv")
    .FallbackLocale("false");

// Serializes to: ?locale=sv&fallback-locale=false
```

### Where

Filters which documents get returned by the query. Specify the field to compare, the comparison operator, and the value to compare it against. Each call adds one condition on a field, and multiple calls combine as AND.

```csharp
QueryBuilder Where(string field, Operator op, object? value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `field` | `string` | Field name to filter on. Supports dot notation for nested fields. |
| `op` | `Operator` | Comparison to apply. |
| `value` | `object?` | Value to compare against. |

#### Example
```csharp
// Published posts with more than 100 views
var query = new QueryBuilder()
    .Where("status", Operator.Equals, "published")
    .Where("views", Operator.GreaterThan, 100);

// Serializes to: ?where[status][equals]=published&where[views][greater_than]=100
```

The `Operator` enum supports the following type of comparisons.

```csharp
public enum Operator
{
    Equals,
    Contains,
    NotEquals,
    In,
    All,
    NotIn,
    Exists,
    GreaterThan,
    GreaterThanEqual,
    LessThan,
    LessThanEqual,
    Like,
    NotLike,
    Within,
    Intersects,
    Near,
}
```

**Known limitation**: `Operator.Exists` on the `id` field of a collection always returns zero results, regardless of `true` or `false`.

### And

Groups several conditions into a single AND block. Use it when a plain sequence of `Where()` calls
can't express the shape you need — most often when an AND group has to sit *inside* an OR.

The callback receives a nested builder exposing the same `Where`, `And`, and `Or` methods, so
groups can be composed to any depth.

```csharp
QueryBuilder And(Action<WhereBuilder> callback)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `callback` | `Action<WhereBuilder>` | Receives a nested builder for composing the grouped conditions. |

#### Example
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

### Or

Groups conditions so that *any* of them can match. This is how you express "posts in either of
these two categories" without running two queries.

```csharp
QueryBuilder Or(Action<WhereBuilder> callback)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `callback` | `Action<WhereBuilder>` | Receives a nested builder for composing the grouped conditions. |

#### Example
```csharp
// Published, and in one of two categories
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

### Join

Configures the documents returned through a Payload **join field** — the reverse side of a
relationship, such as a post's comments. Without configuration a join returns Payload's defaults;
`Join()` lets you page, sort, filter, and count that nested set independently of the parent query.

The callback receives a builder whose methods all take an `on` parameter first: the name of the
join field on the collection you're querying (`"comments"` on a `posts` document).

```csharp
QueryBuilder Join(Action<JoinBuilder> callback)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `callback` | `Action<JoinBuilder>` | Receives a builder for configuring join fields. |

Inside the callback:

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Limit` | `string on, int value` | Maximum joined documents to return. |
| `Page` | `string on, int value` | Page of joined documents. |
| `Sort` | `string on, string field` | Sort joined documents ascending. |
| `SortByDescending` | `string on, string field` | Sort joined documents descending. |
| `Count` | `string on, bool value = true` | Include a total count of joined documents. |
| `Where` | `string on, string field, Operator op, object? value` | Filter joined documents. |
| `And` | `string on, Action<WhereBuilder> callback` | Nested AND group on joined documents. |
| `Or` | `string on, Action<WhereBuilder> callback` | Nested OR group on joined documents. |
| `Disable` | — | Turn off all join fields for this query. |
| `IsDisabled` | — | (getter) Whether joins have been disabled. |

#### Example
```csharp
// Posts, each with their 5 newest approved comments
var query = new QueryBuilder()
    .Join(join =>
    {
        join
            .Limit("comments", 5)
            .SortByDescending("comments", "createdAt")
            .Where("comments", "status", Operator.Equals, "approved");
    });

PaginatedDocsDTO result = await sdk.Find("posts", query);
```

Joins can also be switched off entirely, which is worth doing when you don't need the nested data
and want to avoid the lookups:

```csharp
var query = new QueryBuilder()
    .Join(join => join.Disable());

// Serializes to: ?joins=false
```

## DTOs

The included DTOs represent the **lowest common denominator** of a Payload CMS response. `DocumentDTO` captures the universal fields (`Id`, `CreatedAt`, `UpdatedAt`) and exposes the full response as a raw `Dictionary<string, object?>`.

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
DocumentDTO dto = await sdk.FindById("posts", "123");
BlogPost post = dto.As<BlogPost>();

// Works the same for paginated results:
PaginatedDocsDTO result = await sdk.Find("posts");
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

Returned by paginated read operations (`Find`, `FindVersions`, `FindGlobalVersions`).

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

### BulkOperationDTO

Returned by bulk write operations (`Update`, `Delete`). Maps to Payload's `BulkOperationResult` response shape.

| Property | Type | Description |
|----------|------|-------------|
| `Docs` | `List<DocumentDTO>` | Documents successfully affected by the operation. |
| `Errors` | `List<BulkOperationErrorDTO>` | Per-document errors, if any. Each has `Id` and `Message`. |

### Auth DTOs

| DTO | Returned by | Properties |
|-----|-------------|------------|
| `LoginResultDTO` | `Login()` | `Token`, `Exp`, `User` (DocumentDTO), `Message` |
| `MeResultDTO` | `Me()` | `User`, `Token`, `Exp`, `Collection`, `Strategy` |
| `RefreshResultDTO` | `RefreshToken()` | `RefreshedToken`, `Exp`, `User` |
| `ResetPasswordResultDTO` | `ResetPassword()` | `User`, `Token` |
| `MessageDTO` | `ForgotPassword()`, `VerifyEmail()`, `Logout()`, `Unlock()` | `Message` |

### RequestErrorDTO

Found in `PayloadCMS.DotNet.Models.Errors`. Represents one entry in the `errors[]` array from a failed Payload response. Payload's error shape is intentionally dynamic — only the base fields below are guaranteed across all error types. The `Json` property gives access to the full raw entry, including the `data` block present on `ValidationError` and `APIError` responses.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The error class name (e.g. `"ValidationError"`, `"Forbidden"`). Sourced from `errors[n].name`. |
| `Message` | `string?` | The human-readable error message. Sourced from `errors[n].message`. |
| `Field` | `string?` | The field path associated with the error. Set on Mongoose validation items only. |
| `Json` | `Dictionary<string, object?>` | The full raw JSON for this `errors[n]` entry. |

See [Error Handling](#error-handling) for usage examples.

## Error Handling

`PayloadError` is thrown when a Payload CMS API request fails with a non-2xx status code.

```csharp
public class PayloadError : Exception
{
    public readonly int StatusCode;
    public readonly HttpResponseMessage? Response;
    public readonly string? Body;
    public readonly string? ServerStack;
    public readonly IReadOnlyList<RequestErrorDTO> Result;
}
```

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `int` | HTTP status code. |
| `Response` | `HttpResponseMessage?` | The originating HTTP response. |
| `Message` | `string` | Human-readable status code message (from `Exception`). |
| `Body` | `string?` | The raw unparsed JSON response body, if available. |
| `ServerStack` | `string?` | Server-side stack trace. Payload includes this in development mode only. |
| `Result` | `IReadOnlyList<RequestErrorDTO>` | Parsed entries from `errors[]` in the response body. |

Each entry in `Result` is an [`RequestErrorDTO`](#requesterrordto).

### Basic usage

```csharp
using PayloadCMS.DotNet;
using PayloadCMS.DotNet.Models.Errors;

try
{
    DocumentDTO document = await sdk.FindById("posts", "nonexistent");
}
catch (PayloadError ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");

    foreach (RequestErrorDTO entry in ex.Result)
    {
        Console.WriteLine($"{entry.Name ?? "error"}: {entry.Message}");
    }
}
catch (Exception ex)
{
    // Network failure, timeout, or parsing error
}
```

### Accessing richer error data via Json

Payload's `ValidationError` responses include a `data` block with field-level detail. The library does not model this automatically — define your own types and map from the `Json` escape hatch:

```csharp
public class ValidationFieldError
{
    public string? Message { get; set; }
    public string? Path { get; set; }
}

public class ValidationError
{
    public string? Collection { get; set; }
    public string? Global { get; set; }
    public string? Id { get; set; }
    public string? Message { get; set; }
    public List<ValidationFieldError> FieldErrors { get; set; } = new();

    public static ValidationError? FromJson(Dictionary<string, object?> json)
    {
        if (!json.ContainsKey("name") || json["name"] as string != "ValidationError")
        {
            return null;
        }

        if (!json.ContainsKey("data") || json["data"] is not Dictionary<string, object?> data)
        {
            return null;
        }

        var validationError = new ValidationError
        {
            Collection = data.ContainsKey("collection") ? data["collection"] as string : null,
            Global = data.ContainsKey("global") ? data["global"] as string : null,
            Id = data.ContainsKey("id") ? data["id"]?.ToString() : null,
            Message = json.ContainsKey("message") ? json["message"] as string : null,
        };

        if (data.ContainsKey("errors") && data["errors"] is List<object?> fieldErrors)
        {
            foreach (var item in fieldErrors)
            {
                if (item is not Dictionary<string, object?> fieldJson)
                {
                    continue;
                }

                validationError.FieldErrors.Add(new ValidationFieldError
                {
                    Message = fieldJson.ContainsKey("message") ? fieldJson["message"] as string : null,
                    Path = fieldJson.ContainsKey("path") ? fieldJson["path"] as string : null,
                });
            }
        }

        return validationError;
    }
}
```

Then use it when catching a `PayloadError`:

```csharp
catch (PayloadError error)
{
    foreach (RequestErrorDTO result in error.Result)
    {
        ValidationError? validationError = ValidationError.FromJson(result.Json);

        if (validationError == null)
        {
            Console.WriteLine($"{result.Name ?? "error"}: {result.Message}");
            continue;
        }

        Console.WriteLine($"Validation failed on '{validationError.Collection ?? validationError.Global}':");

        foreach (ValidationFieldError fieldError in validationError.FieldErrors)
        {
            Console.WriteLine($"  {fieldError.Path}: {fieldError.Message}");
        }
    }
}
```

## Extending Payload

Payload can be extended with plugins and custom server-side functionality without requiring changes to this SDK. Because the SDK works against Payload's REST API rather than a fixed schema, most extensions can be used through the existing API.

- **New collections or fields** — use the existing `Find`, `Create`, `Update`, etc. methods. Collection slugs are passed as strings, and `DocumentDTO.Json` exposes fields returned by the API.
- **Custom REST endpoints** — use [`Request()`](#custom-endpoints) for endpoints not covered by the built-in methods.
- **Custom query parameters** — use `AddCustomParam()` for query parameters not represented by `QueryBuilder`.

### AddCustomParam

`AddCustomParam()` registers an arbitrary query-string parameter. This is useful for plugin-specific parameters or custom server configuration.

| Method | Parameters | Description |
|--------|-----------|-------------|
| `AddCustomParam` | `string key, object? value` | Registers an arbitrary query-string parameter. |

```csharp
using PayloadCMS.DotNet.Extensions;

var query = new QueryBuilder()
    .AddCustomParam("myPluginParam", "value");
```
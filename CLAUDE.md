# Payload.CMS — C# Port

Port of the TypeScript library `payload-cms-http-client` to a C# class library.
Full design spec: `PROJECT_GUIDELINES.md` in the TS source.

## TypeScript Source Location
Local: `C:\Users\Eduardo\Desktop\payload-cms-http-client`
Read TS source files directly with Read/Glob/Grep for parity reference — no HTTP needed.

## Project Setup
- Target: .NET 6.0 + .NET 8.0 (multi-targeted), nullable enabled, implicit usings enabled
- No external dependencies (zero-dep per design philosophy)
- All files currently sit at project root (solution has logical folders via .sln, not physical subdirectories)
- Solution: `Payload.CMS.sln`, main project: `Payload.CMS.csproj`, tests: `Tests/Payload.CMS.Tests.csproj`

## Core Type Mappings
| TypeScript | C# |
|---|---|
| `Json` / `JsonObject` | `Dictionary<string, object?>` |
| `JsonValue` | `object?` |
| `JsonArray` | `List<object?>` |
| `string \| undefined` | `string?` |
| `number` (DTO pagination) | `int` |
| `number` (exp timestamp) | `int` |
| `Date` | `DateTime` |
| `Promise<T>` | `Task<T>` |
| `Record<string, string>` (headers) | `Dictionary<string, string>` |

## Enum Pattern
Enums use `[StringValue("...")]` attribute + `EnumExtensions.ToStringValue()` extension method (via reflection).
Both `Operator` and `HttpMethod` follow this pattern.
`GetMember` must use `MemberTypes.Field, BindingFlags.Public | BindingFlags.Static` to avoid matching inherited `object` methods when an enum value is named `Equals`.

## Code Style (enforced across all files)
- **Always braces** on `if`, `foreach`, `for` — no bracketless one-liners, ever
- **Descriptive variable names** — no single-character or abbreviated names (`idValue` not `id`, `longValue` not `l`)
- **No switch expressions** — TypeScript has no equivalent; use if/else chains instead
- **No `or` pattern syntax** — use `||` for multi-type checks to match TS structure
- **Structural parity with TypeScript** — treat the TS files as the style authority; if a variable name exists in TS, use the same name in C#; do not invent names that have no TS equivalent
- **DTO field access** — use `ContainsKey` + direct indexer (`data["field"]`) to mirror TypeScript's `data['field']` direct access; avoid `TryGetValue` which forces an invented `out var` name. Exception: DateTime fields where `TryParse` requires an `out` parameter — use a field-derived name (`createdAtDate`, `updatedAtDate`)

## Key Conventions
- `internal` for everything under `lib/internal/` (contracts, clauses, utils, upload)
- `public` for everything under `lib/public/` (builders, client, models, config, enums)
- Private fields: underscore prefix (`_field`)
- Builders return `this` for fluent chaining
- DTOs are **sealed classes** with `{ get; set; }` properties and defaults — matches TS mutable class + field defaults pattern. Static `FromJson` factory colocated on each class.
- Number extraction in DTOs delegates to `JsonParser.TryConvertInt(object? v)` (handles `int`/`long`/`double` → `int`)
- No separate options classes — use named/optional parameters directly
- Inline options pattern (TS) → named parameters (C#): `Find(string slug, QueryBuilder? query = null)`

## Implementation Status

### Done
- `IAuthCredential`, `IClause`, `IFileUpload` — contracts
- `ApiKeyAuth`, `JwtAuth` — auth credentials
- `Operator`, `HttpMethod` — enums with StringValue
- `StringValueAttribute`, `EnumExtensions` — enum support
- `WhereClause`, `AndClause`, `OrClause`, `JoinClause` — internal clause strategy
- `FormDataBuilder` — multipart form data builder
- `QueryStringEncoder` — full recursive implementation complete
- `JsonParser` — JSON serialization, deserialization, CLR conversion, and `TryConvertInt`
- `DocumentDTO`, `PaginatedDocsDTO`, `TotalDocsDTO` — collection DTOs (sealed classes)
- `LoginResultDTO`, `MeResultDTO`, `RefreshResultDTO`, `ResetPasswordResultDTO`, `MessageDTO` — auth DTOs (sealed classes)
- `WhereBuilder` — public fluent expression builder
- `JoinBuilder` — public fluent join builder (with `IsDisabled` getter)
- `QueryBuilder` — public fluent facade over WhereBuilder + JoinBuilder
- `PayloadError` — exception class (extends Exception, has `StatusCode`, `Response`, `Cause`)
- `FileUpload` — public `IFileUpload` sealed record implementation
- `Client` — main client (all public methods + `_Fetch`, `_AppendQueryString`, `_NormalizeUrl`)
- xUnit test suite — 32 tests across `QueryStringEncoder`, `QueryBuilder`, `JoinBuilder`, `ApiKeyAuth`

### Client Notes
- Class named `Client` (in namespace `Payload.CMS.Public`)
- `using HttpMethod = Payload.CMS.Public.Enums.HttpMethod;` alias required due to implicit using conflict
- Internal `RequestConfig` record: `(HttpMethod? Method = null, HttpContent? Body = null)`
- JSON body → `JsonParser.Serialize(data)` → `StringContent`
- File body → `FormDataBuilder.Build(file, data)` → `MultipartFormDataContent`
- `Content-Type` header is skipped when adding to `HttpRequestMessage.Headers` (handled by `HttpContent` automatically)
- JSON parsing via `JsonParser.Parse` / `JsonParser.ConvertElement` (centralized in `Internal/Utils/JsonParser.cs`)
- `json['doc']` inline pattern: `json.ContainsKey("doc") && json["doc"] is Dictionary<string, object?> doc ? doc : new()`
- `IAuthCredential` and `IFileUpload` changed to `public` (required for public API surface)
- `PayloadError.Cause` is `object?` (matches TS's `unknown`), `InnerException` set via `cause as Exception`
- `CancellationToken cancellationToken = default` on all 25 public async methods, propagated to `SendAsync` and `ReadAsStringAsync`

## QueryStringEncoder Rules (critical for parity)
- Nested objects: bracket notation `where[title][equals]=foo`
- Arrays: indexed notation `where[or][0][title]=foo`
- `select`, `sort`: comma-separated (NOT indexed) `select=a,b`
- `null`/`undefined` values: skipped
- `bool`: serialized as `"true"` / `"false"` strings
- `DateTime`: ISO 8601 string
- `[`, `]`, `,` left unescaped after `Uri.EscapeDataString()` — replace `%5B`→`[`, `%5D`→`]`, `%2C`→`,`
- No `?` prefix by default (controlled by `_addQueryPrefix`, defaults true)

## JoinBuilder Notes
- `Build()` returns `Dictionary<string, object?>?` (null if no clauses)
- `IsDisabled` is a separate bool getter (split from original `false` union return)
- Get-or-create pattern keyed by collection name (`On` field)

## Auth Flow
`login()` returns result only — does NOT auto-set auth. Consumer calls `SetAuth()` explicitly.

## Response Unwrapping
- `create`, `updateById`, `deleteById`, `restoreGlobalVersion` → unwrap `doc` key
- `updateGlobal` → unwrap `result` key (NOT `doc`)
- `count` → `TotalDocsDTO.FromJson(json).TotalDocs` returns `int`
- All others → full response into appropriate DTO

# Payload.CMS — C# Port

Port of the TypeScript library `payload-cms-http-client` to a C# class library.
Full design spec: `PROJECT_GUIDELINES.md` in the TS source.

## TypeScript Source Location
Local: `C:\Users\Eduardo\Desktop\payload-cms-http-client`
Read TS source files directly with Read/Glob/Grep for parity reference — no HTTP needed.

## Project Setup
- Target: .NET 8.0 only (no net6.0 — do not add it back), nullable enabled, implicit usings enabled
- Dependencies: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Http` (DI extension), `Microsoft.SourceLink.GitHub` (build-time only)
- Solution: `PayloadCMS.DotNet.sln`, main project: `PayloadCMS.DotNet.csproj` (`PackageId: PayloadCMS.DotNet`, `AssemblyName: PayloadCMS.DotNet`), tests: `Tests/Payload.CMS.Tests.csproj`
- Test project: xUnit v3, targets net8.0 only, `OutputType=Exe`

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
`Operator` follows this pattern.
`GetMember` must use `MemberTypes.Field, BindingFlags.Public | BindingFlags.Static` to avoid matching inherited `object` methods when an enum value is named `Equals`.

## Port Divergences
- **HttpMethod**: TypeScript and Dart use a custom `HttpMethod` enum (no native type). C# uses `System.Net.Http.HttpMethod` (platform-native). This is a justified platform divergence — `HttpMethod` is NOT exported from the C# package.
- **`RequestConfig`**: Public `sealed record` in `PayloadCMS.DotNet.Config`, used as the options object for `PayloadSDK.Request()`. Mirrors the TS inline options object `{ method, path, body?, query? }`. Private `Request` takes `(url, method?, body?)` directly — no private wrapper record.
- **DTO `FromJson`/`ToJson` visibility**: In TypeScript, `fromJson`/`toJson` are public static methods on each DTO class and re-exported from `index.ts`. In C#, they are `internal` — the `Dictionary<string, object?>` wire format is an implementation detail that consumers should never interact with directly. TypeScript has no `internal` equivalent as a first-class language feature (`@internal` JSDoc exists but requires tooling), so this divergence is not mirrored in the TS source.
- **`PayloadError` human-readable message**: TypeScript (`Error.message`) and C# (`Exception.Message`) both surface a human-readable status-code message via inheritance. Dart's `Exception` is an interface with no `message` field — the Dart port must instead override `toString()` to return the equivalent string. Consumer-facing behaviour is identical across all three ports; only the wiring differs.
- **`PayloadError.ServerStack`**: C# uses `ServerStack` (not `Stack`) because `Exception` has no native `Stack` property to conflict with. TypeScript uses `serverStack` (not `stack`) because `Error.stack` is already occupied by the JS call stack. Both ports use the same name — this is a coordinated cross-language choice, not a divergence.

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
- DTOs are **sealed classes** with `{ get; set; }` properties and defaults — matches TS mutable class + field defaults pattern. `internal static FromJson` factory colocated on each class — `internal` because the `Dictionary<string, object?>` wire format is an implementation detail, not part of the public API.
- Number extraction in DTOs delegates to `JsonParser.TryConvertInt(object? v)` (handles `int`/`long`/`double` → `int`)
- No separate options classes — use named/optional parameters directly
- Inline options pattern (TS) → named parameters (C#): `Find(string slug, QueryBuilder? query = null)`

## Implementation Status

### Done
- `IAuthCredential`, `IClause`, `IFileUpload` — contracts
- `ApiKeyAuth`, `JwtAuth` — auth credentials
- `Operator` — enum with StringValue (HttpMethod enum dropped — uses `System.Net.Http.HttpMethod`)
- `StringValueAttribute`, `EnumExtensions` — enum support
- `WhereClause`, `AndClause`, `OrClause`, `JoinClause`, `SelectClause` — internal clause strategy
- `FormDataBuilder` — multipart form data builder
- `QueryStringEncoder` — full recursive implementation complete
- `JsonParser` — JSON serialization, deserialization, CLR conversion, and `TryConvertInt`
- `DocumentDTO`, `PaginatedDocsDTO`, `TotalDocsDTO`, `BulkOperationDTO` — collection DTOs (sealed classes)
- `LoginResultDTO`, `MeResultDTO`, `RefreshResultDTO`, `ResetPasswordResultDTO`, `MessageDTO` — auth DTOs (sealed classes)
- `WhereBuilder` — public fluent expression builder in `Public/Query/`, namespace `PayloadCMS.DotNet.Query`
- `SelectBuilder` — public fluent field-selection builder (delegates to `SelectClause` list, deep-merges results) in `Public/Query/`, namespace `PayloadCMS.DotNet.Query`
- `JoinBuilder` — public fluent join builder (with `IsDisabled` getter) in `Public/Query/`, namespace `PayloadCMS.DotNet.Query`
- `QueryBuilder` — public fluent facade over `WhereBuilder`, `SelectBuilder`, and `JoinBuilder` in `Public/Query/`, namespace `PayloadCMS.DotNet.Query`
- `PayloadError` — exception class (extends Exception, has `StatusCode`, `Response`, `Body`, `ServerStack`, `Result`) in `Public/`, namespace `PayloadCMS.DotNet`
- `RequestErrorDTO` — sealed class in `Public/Models/Errors/`, exposes `Name`, `Message`, `Field`, `Json` (base shape only; `data` block accessible via `Json` for consumer-side mapping)
- `FileUpload` — public `IFileUpload` sealed record implementation
- `RequestConfig` — public `sealed record` in `PayloadCMS.DotNet.Config`; options object for `PayloadSDK.Request()`
- `PayloadSDK` — main client (all public methods + `Fetch`, `AppendQueryString`, `NormalizeUrl`) in namespace `PayloadCMS.DotNet`
- `ServiceCollectionExtensions.AddPayloadSDK()` — ASP.NET Core DI extension in `PayloadCMS.DotNet.Extensions`
- xUnit v3 test suite — 97 tests across `QueryStringEncoder`, `QueryBuilder`, `SelectBuilder`, `JoinBuilder`, `ApiKeyAuth`, `PayloadError`, `PayloadSDK`

### PayloadSDK Notes
- Namespace: `PayloadCMS.DotNet`; class named `PayloadSDK`
- No `headers` constructor parameter — use `SetHeaders()` after construction
- Uses `System.Net.Http.HttpMethod` directly (no custom enum alias needed)
- `Fetch(url, HttpMethod? method, HttpContent? body, ct)` — no wrapper record; callers pass method/body directly. `HttpContent body` typed explicitly where ternary mixes `MultipartFormDataContent` and `StringContent`
- JSON body → `JsonParser.Serialize(data)` → `StringContent`
- File body → `FormDataBuilder.Build(file, data)` → `MultipartFormDataContent`
- `Content-Type` header is skipped when adding to `HttpRequestMessage.Headers` (handled by `HttpContent` automatically)
- JSON parsing via `JsonParser.Parse` / `JsonParser.ConvertElement` (centralized in `Internal/Utils/JsonParser.cs`)
- `doc`/`result` unwrapping pattern: pre-initialize `Dictionary<string, object?> doc = new();`, then `if (json.ContainsKey("doc") && json["doc"] is Dictionary<string, object?> value) { doc = value; }`
- `IAuthCredential` and `IFileUpload` are `public` (required for public API surface)
- `CancellationToken cancellationToken = default` on all public async methods, propagated to `SendAsync` and `ReadAsStringAsync`

## QueryStringEncoder Rules (critical for parity)
- Nested objects: bracket notation `where[title][equals]=foo`
- Arrays: indexed notation `where[or][0][title]=foo`
- `select`: bracket-object notation `select[title]=true&select[author]=true` (NOT comma-separated)
- `sort`: comma-separated (NOT indexed) `sort=a,-b`
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


## Parity Audit Action Plan (2026-07-06)

Findings from the audit against `@shopnex/payload-sdk` (the official Payload TS SDK) and this
project's own design docs. Ordered by importance. Items marked **shared** exist identically in the
TS source — fix C# first, then log the backport in `TYPESCRIPT_BACKPORT.md` so the ports reconverge.

### 1. [x] Culture-invariant number encoding — C#-only bug
`QueryStringEncoder.SerializePrimitive` falls through to `value.ToString()`, which is
culture-sensitive. On a `sv-SE` machine `3.14` encodes as `3,14` — and because `,` is deliberately
left unescaped, the query value is corrupted. TS `String(3.14)` is always invariant.
**Fix**: `Convert.ToString(value, CultureInfo.InvariantCulture)` for the primitive fallback.

### 2. [x] Parse response body only after the status check — C#-only divergence
Private `Request` in `PayloadSDK.cs` calls `JsonParser.Parse(text)` *before* checking
`IsSuccessStatusCode`; TS parses only after the `ok` check. A non-2xx response with a non-JSON body
(proxy HTML page, plain-text 502) therefore throws a generic wrapped `JsonException` instead of
`PayloadError` with the status code. **Fix**: move the parse below the status check (also removes a
wasted parse — `PayloadError` parses the body itself).

### 3. [x] Numeric document IDs — shared, backport to TS
`DocumentDTO.FromJson` and `BulkOperationErrorDTO.FromJson` only accept `id` when it is a string.
Payload on Postgres/SQLite returns numeric IDs, so `Id` silently stays `""`. The official SDK
sidesteps this via generics; our DTO design must normalize instead.
**Fix**: also accept `int`/`long`/`double` ids and normalize to string (invariant culture).
TS backport: `typeof data['id'] === 'number'` → `String(data['id'])`.

### 4. [x] `_payload` multipart part should be a plain string — C#-only divergence
`FormDataBuilder` uses `JsonContent.Create(data)`, which stamps the form part with
`Content-Type: application/json`. TS appends a plain string, and PROJECT_GUIDELINES §8.10
prescribes `StringContent(JsonSerializer.Serialize(data))`. Some multipart parsers treat typed
parts differently from plain string fields. **Fix**: use plain `StringContent` per the guidelines.
Verify with an upload test against the live CMS afterwards.

### 5. [x] `Draft()` / `Trash()` query params — shared feature gap vs official SDK
The official SDK's `buildSearchParams` supports `draft` (draft/versions workflow) and `trash`
(Payload v3 soft delete). Neither port exposes them, which undercuts the otherwise-complete
versions API. **Fix**: add `Draft(bool value)` and `Trash(bool value)` to `QueryBuilder`,
serialized as `draft=true` / `trash=true`. TS backport: `draft({ value })` / `trash({ value })`.

### 6. [ ] `Populate()` semantics — shared; verified broken 2026-07-06, redesign deliberately LAST
Live-verified: the comma encoding `populate=a,b` is ignored by Payload (response byte-identical to
no populate); object notation `populate[<collection>][<field>]=true` works. Key semantics (never
matched any Payload version — the comma model is Strapi/Mongoose-shaped): `populate` does NOT choose
which relationships resolve (that is `depth`); it is a select mask applied to already-populated
docs, keyed by target collection slug (polymorphic-safe), overriding the collection's
`defaultPopulate`. Introduced in Payload v3.0 alongside `select`. Redesign:
`Populate(string collection, string[] fields)`. Correctness assertion: with `depth>=1`,
`Populate("users", ["name"])` yields author objects containing only `name`+`id`; with `depth=0` it
has no effect. **Sequenced last by user decision (2026-07-06)** — after §11 draft writes and the
empty-where guard — to build a solid mental model first. Backport §10.

### 7. [x] DateTime parse hardening — C#-only minor
`DocumentDTO.FromJson` uses bare `DateTime.TryParse` (culture-sensitive, converts to local time).
**Fix**: parse with `CultureInfo.InvariantCulture` + `DateTimeStyles.RoundtripKind`.

### 8. [x] Doc/spec drift
- Target framework: net8.0 only is correct — docs previously promised net6.0 and have been
  aligned to net8.0+ instead (user decision 2026-07-06).
- This file's Project Setup section references `Payload.CMS.csproj`; actual file is
  `PayloadCMS.DotNet.csproj`.
- PROJECT_GUIDELINES §6.1 (TS repo) lists `updateGlobal` as PATCH; both implementations and
  Payload's REST API use POST. The code is right; fix the table.
- README `RequestErrorDTO` section links to stale `#errorresultdto` anchor.
- Test count drift: keep the count in Implementation Status current.

### 9. [x] Draft writes — shared feature gap (found in integration pass 2026-07-06; C# done)
`Create`/`UpdateById`/`UpdateGlobal` now accept `QueryBuilder? query = null` (placed after `data`,
mirroring bulk `Update`), so `Draft(true)`/`Locale` work on writes. Three unit tests assert
`draft=true` reaches the URL. Source-breaking for positional `file` callers — use `file:` named
argument. TS backport pending — see `TYPESCRIPT_BACKPORT.md` §11 for the exact change table.

### Accepted (no action)
- Custom `Content-Type` set via `SetHeaders()` is dropped in C# (HttpContent owns the header) —
  edge case with no Payload-relevant consequence; TS would honor it. Documented divergence.
- `TryConvertInt` truncates `long`/`double` — mirrors loose JS number semantics; timestamps fit.
- **Empty-`where` bulk `Update`/`Delete`: no client-side guard** (decision 2026-07-07, after full
  design discussion — do not re-propose). The server 400 ("Missing 'where' query…") is
  authoritative and its message is fully surfaced via `PayloadError.Result`; consumers should
  render that (see CmsProject `PayloadErrorExtensions.ToDisplayMessage()`). A hard-coded guard
  would over-validate if Payload ever relaxes the rule — "Payload behavior always wins" (§2.1).
  This matches the official SDK exactly: its enforcement is TS-types only, zero runtime checks.
  A C# type-level equivalent (bulk-query subtype / type-state builder) was evaluated and rejected
  as class explosion contradicting §2.2 minimalism.

### Integration-lab checklist (CmsProject)
Exercises every risky finding: populate on a real relationship field · file upload · document fetch
on the configured DB adapter (numeric IDs) · `where` with a decimal value · request to a non-API
route (error path).
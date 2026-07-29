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
- `PopulateBuilder` — public fluent populate-mask builder (composes one `SelectBuilder` per collection slug) in `Public/Query/`, namespace `PayloadCMS.DotNet.Query`
- `QueryBuilder` — public fluent facade over `WhereBuilder`, `SelectBuilder`, and `JoinBuilder` in `Public/Query/`, namespace `PayloadCMS.DotNet.Query`
- `PayloadError` — exception class (extends Exception, has `StatusCode`, `Response`, `Body`, `ServerStack`, `Result`) in `Public/`, namespace `PayloadCMS.DotNet`
- `RequestErrorDTO` — sealed class in `Public/Models/Errors/`, exposes `Name`, `Message`, `Field`, `Json` (base shape only; `data` block accessible via `Json` for consumer-side mapping)
- `FileUpload` — public `IFileUpload` sealed record implementation
- `RequestConfig` — public `sealed record` in `PayloadCMS.DotNet.Config`; options object for `PayloadSDK.Request()`
- `PayloadSDK` — main client (all public methods + `Fetch`, `AppendQueryString`, `NormalizeUrl`) in namespace `PayloadCMS.DotNet`
- `ServiceCollectionExtensions.AddPayloadSDK()` — ASP.NET Core DI extension in `PayloadCMS.DotNet.Extensions`
- xUnit v3 test suite — 108 tests across `QueryStringEncoder`, `QueryBuilder`, `SelectBuilder`, `JoinBuilder`, `ApiKeyAuth`, `PayloadError`, `PayloadSDK`

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

**PARKED (2026-07-09) — Draft/Trash capability gating, revisit later, do not re-litigate blind:**
Item 5 is functionally done and at parity with the official SDK (top priority per user decision
2026-07-09: parity with `@shopnex/payload-sdk`'s REST flavor is the primary goal). Open design
discomfort, deliberately shelved: `Draft`/`Trash` sit unconditionally on `QueryBuilder` for every
collection, but they're opt-in Payload features (`versions.drafts`, `trash: true` per collection).
Live-verified 2026-07-09: sending `Draft(true)` against a collection with no drafts configured
silently no-ops on reads (identical response with/without the flag) AND, worse, on **writes** —
`?draft=true` PATCH against a non-versioned collection returns 200 and commits straight to the live
document, no error, no signal. A developer using `Draft(true)` specifically to stage a safe
non-public edit would silently publish it instead. This is a materially worse failure mode than
`Populate`/`Join`/`Locale` no-opping (those are visible in the response shape; this isn't).
Considered and rejected for now: client-side capability gating (mirroring which collections have
drafts/trash enabled) — same risk class as the empty-`where` guard rejection above, one level
worse: it requires mirroring *remote schema* the SDK has no way to query, which will drift the
moment a collection's CMS config changes without the client being updated. The official SDK has
the identical blind spot (flat options bag, zero capability checks) — so this is not a parity gap,
it's a structural limitation of any out-of-process REST client. Options on the table when revisited:
(1) doc-only — spell out the silent-live-write danger explicitly (cheapest, no staleness risk);
(2) a documented consumer-side pattern — check `doc.Json.ContainsKey("_status")` after a
`Draft`-flagged write to confirm versioning was actually active, analogous to CmsProject's own
`PayloadErrorExtensions` pattern; (3) an explicit opt-in capability declaration on the SDK — new
public surface, own staleness risk in the opposite direction, doesn't fit the one-builder-per-param
shape. Leaning (1) + (2), not (3), but undecided — loop back after the current lab testing pass.

**UPDATE (2026-07-29) — `trash: true` enabled on `posts`, real mechanics found, `DeleteById` fixed:**
The "opt-in feature" half of the note above is now partially resolved for `Trash` specifically —
`posts` has `trash: true`. Live-verified against Payload's compiled source: soft-delete ("trashing")
is NOT a `Trash(true)` action at all — it's a plain `update()` setting `data.deletedAt`. `Trash(true)`
only lifts a hidden `deletedAt IS NULL` filter that `find`/`update`/`delete` all apply by default;
it's inclusive (matches trashed + normal), not exclusive-to-trash, and adds no restriction of its
own. This exposed a real, separate gap: `DeleteById` had no `QueryBuilder` parameter at all, so a
trashed document couldn't be permanently deleted by ID — the server 404s on a plain `DELETE`
against an already-trashed doc, since the same hidden filter excludes it from being found. Fixed:
`DeleteById` now takes `QueryBuilder? query = null`, same shape as the §11 write methods. See
`TYPESCRIPT_BACKPORT.md` §12. The Draft/Trash capability-gating question above remains open and
unrelated to this fix — this was a concrete missing-parameter bug, not the parked design question.

**FULL RE-AUDIT (2026-07-29)** — the `DeleteById` gap prompted a complete method-by-method,
parameter-by-parameter re-verification against the literal source of `packages/sdk/src` in
`payloadcms/payload` (fetched raw, not summarized), after the user correctly pointed out the
original audit never did this — it checked method existence and response-unwrapping shape, never
a full parameter diff, which is exactly the class of gap that let `DeleteById` through undetected.
Findings:

- **Five more methods had zero query-parameter support at all** (same bug class as `DeleteById`):
  `FindVersionById`, `RestoreVersion`, `FindGlobal`, `FindGlobalVersionById`,
  `RestoreGlobalVersion`. **Fixed 2026-07-29** — all five now take `QueryBuilder? query = null`,
  same pattern as §11/§12. Five new unit tests. See `TYPESCRIPT_BACKPORT.md` §13 for the exact TS
  change table. `FindGlobal` was the most consequential of the five — one of the most-used
  methods, previously unable to express `depth`, `locale`, or `select` at all.
- **`QueryBuilder` was missing two official params** — `pagination?: boolean` and
  `autosave?: boolean`. **`Pagination(bool value)` added 2026-07-29** (see
  `TYPESCRIPT_BACKPORT.md` §15) — verified at the `buildSearchParams.ts` source level that it's
  wired identically to `draft`/`trash`, not Local-API-only despite the docs' REST example only
  showing `limit`/`page`. `autosave` deliberately **not** added — traced the same source and found
  the official SDK's own `buildSearchParams` never reads it at all, so it's inert in the reference
  implementation itself, not just under-documented. Live verification of `Pagination` against the
  real REST response (does `pagination=false` ignore `limit`, or only skip the count query?) is
  still pending — see the CmsProject lab plan.
- **`disableErrors` has no equivalent** on `findById`/`findVersionById`/`findGlobalVersionById` —
  official SDK can swallow a 404 and return `null`; ours always throws. Not a query-string param,
  so not a `QueryBuilder` fix — would need an overload or nullable-return design. Flagged as an
  open design question, not yet proposed as a concrete fix. See `TYPESCRIPT_BACKPORT.md` §14.

Everything else (`Find`, `FindById`, `Create`, `Delete`/`DeleteById`, `Update`/`UpdateById`,
`Count`, `UpdateGlobal`, all six auth methods) verified to already cover their full official
parameter set. `Logout`/`Unlock` confirmed to genuinely not exist in the official SDK — pre-existing,
documented C#-only additions, not a gap.

### 6. [x] `Populate()` semantics — DONE in C# 2026-07-09 (redesigned per user-approved design)
`Populate(string collection, string[] fields)` — a select mask keyed by collection slug, emitting
`populate[<collection>][<field>]=true`. Implemented as a dedicated **`PopulateBuilder`** in
`Public/Query/` (builder-family symmetry — user review feedback 2026-07-09: every query-param
family gets its own named builder; do not inline builder logic into the `QueryBuilder` facade).
`PopulateBuilder` composes one `SelectBuilder` per collection slug (get-or-create), reusing its
dot-notation expansion and deep merge. Repeated calls merge additively; empty slug is skipped
(JoinBuilder precedent); no exclusion mode (deferred until a real use case). Old comma-list
overload removed outright — it emitted a dead param. Five unit tests pin the wire shape. Live
narrowing verification in the lab (QueryPopulate page) pending the next package bump.
TS backport: §10 in `TYPESCRIPT_BACKPORT.md`. Original finding kept below for reference:
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

### Backlog
- **Full documentation reconciliation pass, per port** (added 2026-07-09). The C# repo's
  `PROJECT_GUIDELINES.md` has drifted from the code (stale §8/§9 status tables, file tree gaps
  beyond the query section, missing `Models/Errors` entries); the TS repo's copy and `README.md`s
  should be swept in the same pass. Verify every table/tree/checklist against the actual source
  on both sides. Do this as its own focused task, not piecemeal.

### Integration-lab checklist (CmsProject)
Exercises every risky finding: populate on a real relationship field · file upload · document fetch
on the configured DB adapter (numeric IDs) · `where` with a decimal value · request to a non-API
route (error path).
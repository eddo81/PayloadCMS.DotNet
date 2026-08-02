using PayloadCMS.DotNet.Query;

namespace PayloadCMS.DotNet.Extensions;

/// <summary>
/// Extension methods for <see cref="QueryBuilder"/> covering query parameters that require
/// matching server-side configuration to be meaningful (<c>draft</c>, <c>trash</c>,
/// <c>autosave</c>), plus the general-purpose custom parameter primitive they are built on.
/// <para>Requires an explicit <c>using PayloadCMS.DotNet.Extensions;</c> — deliberately not part
/// of <see cref="QueryBuilder"/>'s own members, so using them takes the same deliberate step on
/// the client as configuring the matching feature takes on the Payload CMS instance.</para>
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Registers an arbitrary custom query-string parameter — the extensibility primitive
    /// <see cref="Draft"/>, <see cref="Trash"/>, and <see cref="Autosave"/> are themselves built
    /// on. Intended for parameters introduced by Payload plugins or custom server-side
    /// configuration that this library has no first-class method for.
    /// <para>A custom parameter with a key matching a built-in one (including <c>draft</c>,
    /// <c>trash</c>, <c>autosave</c>, or any core parameter such as <c>where</c>/<c>select</c>)
    /// overwrites it — there is no collision guard.</para>
    /// </summary>
    /// <param name="builder">The query builder.</param>
    /// <param name="key">The query-string parameter name.</param>
    /// <param name="value">The value to serialize for this parameter.</param>
    /// <returns>The current builder for chaining.</returns>
    public static QueryBuilder AddCustomParam(this QueryBuilder builder, string key, object? value)
    {
        builder._customParams[key] = value;

        return builder;
    }

    /// <summary>
    /// Overlays the latest draft version of each returned document (<c>draft=true</c>).
    /// <para>This is NOT a visibility filter — a plain find already returns documents
    /// of every <c>_status</c>; filter on <c>_status</c> to control which documents
    /// come back. What <c>draft=true</c> changes is the content: published documents
    /// with a newer pending draft are returned with the draft's content instead.</para>
    /// <para>On writes, saves the change as a draft version without touching the
    /// published document. Requires drafts enabled on the collection/global.</para>
    /// </summary>
    /// <param name="builder">The query builder.</param>
    /// <param name="value"><c>true</c> to read draft overlays / write draft versions.</param>
    /// <returns>The current builder for chaining.</returns>
    public static QueryBuilder Draft(this QueryBuilder builder, bool value)
    {
        return builder.AddCustomParam("draft", value);
    }

    /// <summary>
    /// Includes soft-deleted (<c>trash</c>) documents in the query.
    /// <para>Only meaningful on collections with <c>trash</c> enabled (Payload v3).</para>
    /// </summary>
    /// <param name="builder">The query builder.</param>
    /// <param name="value"><c>true</c> to include soft-deleted documents.</param>
    /// <returns>The current builder for chaining.</returns>
    public static QueryBuilder Trash(this QueryBuilder builder, bool value)
    {
        return builder.AddCustomParam("trash", value);
    }

    /// <summary>
    /// Marks a write as an autosave (<c>autosave=true</c>).
    /// <para>Only meaningful on collections with <c>versions.drafts.autosave</c> configured —
    /// same capability-gating caveat as <see cref="Draft"/>/<see cref="Trash"/>.</para>
    /// </summary>
    /// <param name="builder">The query builder.</param>
    /// <param name="value"><c>true</c> to mark the write as an autosave.</param>
    /// <returns>The current builder for chaining.</returns>
    public static QueryBuilder Autosave(this QueryBuilder builder, bool value)
    {
        return builder.AddCustomParam("autosave", value);
    }
}

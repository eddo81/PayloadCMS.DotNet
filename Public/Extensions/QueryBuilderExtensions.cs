using PayloadCMS.DotNet.Query;

namespace PayloadCMS.DotNet.Extensions;

/// <summary>
/// Adds query parameters for features that must also be turned on in Payload: <c>draft</c>,
/// <c>trash</c> and <c>autosave</c>, plus a way to send any other query parameter by name.
/// <para>Available after adding <c>using PayloadCMS.DotNet.Extensions;</c>.</para>
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Adds a custom query parameter, for parameters this library has no method for, such as those
    /// added by Payload plugins.
    /// <para>Adding the same parameter again replaces its value. A parameter that another method
    /// also sets, such as <c>limit</c> or <c>where</c>, replaces that method's value.</para>
    /// </summary>
    /// <param name="builder">The query to add the parameter to.</param>
    /// <param name="key">The parameter's name.</param>
    /// <param name="value">The parameter's value.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public static QueryBuilder AddCustomParam(this QueryBuilder builder, string key, object? value)
    {
        builder._customParams[key] = value;

        return builder;
    }

    /// <summary>
    /// Works with draft versions instead of published ones.
    /// <para>When reading, each document is returned as its newest version, published or not. It
    /// does not change which documents are returned; filter on <c>_status</c> for that.</para>
    /// <para>When writing, the change is saved as a draft and the published document is left as
    /// it is.</para>
    /// <para>Drafts must be turned on for the collection or global. Without that, a write is saved
    /// straight to the published document, with no error.</para>
    /// </summary>
    /// <param name="builder">The query to add the parameter to.</param>
    /// <param name="value"><c>true</c> to work with drafts.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public static QueryBuilder Draft(this QueryBuilder builder, bool value)
    {
        return builder.AddCustomParam("draft", value);
    }

    /// <summary>
    /// Includes documents that have been moved to the trash, alongside the others.
    /// <para>Also needed to permanently delete a document that is already in the trash.</para>
    /// <para>Only has an effect on collections with trash turned on in Payload.</para>
    /// </summary>
    /// <param name="builder">The query to add the parameter to.</param>
    /// <param name="value"><c>true</c> to include documents in the trash.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public static QueryBuilder Trash(this QueryBuilder builder, bool value)
    {
        return builder.AddCustomParam("trash", value);
    }

    /// <summary>
    /// Saves a write as an autosave of the draft.
    /// <para>Only has an effect on collections with autosave turned on in Payload.</para>
    /// </summary>
    /// <param name="builder">The query to add the parameter to.</param>
    /// <param name="value"><c>true</c> to save the write as an autosave.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public static QueryBuilder Autosave(this QueryBuilder builder, bool value)
    {
        return builder.AddCustomParam("autosave", value);
    }
}

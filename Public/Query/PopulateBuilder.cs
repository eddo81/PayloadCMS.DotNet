namespace PayloadCMS.DotNet.Query;

/// <summary>
/// Fluent builder for Payload CMS <c>populate</c> query parameters.
///
/// <para><c>populate</c> is a select shape keyed by target collection slug
/// (e.g. <c>populate[users][name]=true</c>) that narrows what already-populated
/// documents contain. It does not choose which relationships resolve into
/// objects — that is <c>depth</c>'s job — and it overrides the target
/// collection's <c>defaultPopulate</c> config. Payload always includes <c>id</c>.</para>
///
/// <para>Each collection's mask is composed via a <see cref="SelectBuilder"/>,
/// so dot notation and additive deep-merging behave exactly like <c>select</c>.</para>
/// </summary>
public class PopulateBuilder
{
    private readonly Dictionary<string, SelectBuilder> _masks = new Dictionary<string, SelectBuilder>();

    /// <summary>
    /// Masks the fields returned on populated documents from a target <c>collection</c>.
    /// <para>Keyed by collection slug (not field name) so polymorphic relationships
    /// are masked consistently. Repeated calls for the same collection merge
    /// additively. Empty collection slugs are silently skipped.</para>
    /// </summary>
    /// <param name="collection">The target collection slug (e.g. <c>"users"</c>).</param>
    /// <param name="fields">Field names to include on populated documents.</param>
    /// <returns>The current builder for chaining.</returns>
    public PopulateBuilder Populate(string collection, string[] fields)
    {
        if (collection == "")
        {
            return this;
        }

        if (!_masks.ContainsKey(collection))
        {
            _masks[collection] = new SelectBuilder();
        }

        _masks[collection].Select(fields);

        return this;
    }

    /// <summary>
    /// Builds the final <c>populate</c> object keyed by collection slug.
    /// </summary>
    /// <returns>The populate map, or <c>null</c> if no masks were configured.</returns>
    public Dictionary<string, object?>? Build()
    {
        var result = new Dictionary<string, object?>();

        foreach (var (collection, selectBuilder) in _masks)
        {
            var mask = selectBuilder.Build();

            if (mask != null)
            {
                result[collection] = mask;
            }
        }

        if (result.Count == 0)
        {
            return null;
        }

        return result;
    }
}

namespace PayloadCMS.DotNet.Query;

/// <summary>
/// Builds the fields to return for related documents, collection by collection.
/// <para>Used through <see cref="QueryBuilder.Populate"/>.</para>
/// </summary>
public class PopulateBuilder
{
    private readonly Dictionary<string, SelectBuilder> _masks = new Dictionary<string, SelectBuilder>();

    /// <summary>
    /// Chooses which fields are returned for related documents from the given collection.
    /// <para>Only affects related documents that are returned in full, so it needs
    /// <see cref="QueryBuilder.Depth"/> set to 1 or more.</para>
    /// <para>Call it again for the same collection to add more fields. Use dot notation for nested
    /// fields, such as <c>"group.number"</c>.</para>
    /// <para>An empty collection name is ignored.</para>
    /// </summary>
    /// <param name="collection">The collection the related documents belong to, such as <c>"users"</c>.</param>
    /// <param name="fields">The fields to return for those documents.</param>
    /// <returns>The current <see cref="PopulateBuilder"/> instance.</returns>
    public PopulateBuilder Populate(string collection, string[] fields)
    {
        if (string.IsNullOrEmpty(collection))
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
    /// Builds the final <c>populate</c> object from all configured collections.
    /// </summary>
    /// <returns>The <c>populate</c> object, or <c>null</c> if no fields were configured.</returns>
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

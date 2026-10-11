namespace PayloadCMS.DotNet.Query;

/// <summary>
/// Fluent builder for Payload CMS REST API query parameters.
/// Use it to filter, sort, paginate, and control which fields and related documents are returned.
/// </summary>
public class QueryBuilder
{
    private int? _limit;
    private int? _page;
    private bool? _pagination;
    private string? _sort;
    private int? _depth;
    private string? _locale;
    private string? _fallbackLocale;
    private readonly PopulateBuilder _populateBuilder = new PopulateBuilder();
    private readonly WhereBuilder _whereBuilder = new WhereBuilder();
    private readonly SelectBuilder _selectBuilder = new SelectBuilder();
    private readonly JoinBuilder _joinBuilder = new JoinBuilder();
    internal readonly Dictionary<string, object?> _customParams = new();

    /// <summary>
    /// Limits the number of documents returned.
    /// </summary>
    /// <param name="value">Maximum document count.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Limit(int value)
    {
        _limit = value;

        return this;
    }

    /// <summary>
    /// Sets which page of results to return. The first page is 1.
    /// </summary>
    /// <param name="value">The page number.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Page(int value)
    {
        _page = value;

        return this;
    }

    /// <summary>
    /// Turns paging on or off. With paging off, every matching document is returned in one response.
    /// <para>Combine with <see cref="Limit"/> to still cap how many documents are returned.</para>
    /// </summary>
    /// <param name="value"><c>false</c> to return every match at once; <c>true</c> to return pages.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Pagination(bool value)
    {
        _pagination = value;

        return this;
    }

    /// <summary>
    /// Sorts results by the given field, in ascending order.
    /// <para>Call it again to add further sort fields; earlier calls take priority.</para>
    /// <para>An empty field name is ignored.</para>
    /// </summary>
    /// <param name="field">The field to sort by.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Sort(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return this;
        }

        if (_sort == null)
        {
            _sort = field;
        }
        else
        {
            _sort += $",{field}";
        }

        return this;
    }

    /// <summary>
    /// Sorts results by the given field, in descending order.
    /// <para>Combines with <see cref="Sort"/>: calls add sort fields in order, earlier calls taking
    /// priority.</para>
    /// <para>An empty field name is ignored.</para>
    /// </summary>
    /// <param name="field">The field to sort by.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder SortByDescending(string field)
    {
        if (!string.IsNullOrEmpty(field) && !field.StartsWith('-'))
        {
            field = $"-{field}";
        }

        return Sort(field);
    }

    /// <summary>
    /// Controls whether related documents are returned in full or as IDs only.
    /// <para>A value of 0 returns related documents as IDs only; 1 includes the directly related
    /// documents; 2 also includes their related documents, and so on.</para>
    /// </summary>
    /// <param name="value">How many levels deep to return related documents in full.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Depth(int value)
    {
        _depth = value;

        return this;
    }

    /// <summary>
    /// Sets the language that translated fields are returned in.
    /// <para>An empty value is ignored.</para>
    /// </summary>
    /// <param name="value">A locale code, such as <c>en</c> or <c>sv</c>.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Locale(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        _locale = value;

        return this;
    }

    /// <summary>
    /// Sets the language to fall back to when a translated field has no value in the requested
    /// language.
    /// <para>An empty value is ignored.</para>
    /// </summary>
    /// <param name="value">A locale code, such as <c>en</c>.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder FallbackLocale(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        _fallbackLocale = value;

        return this;
    }

    /// <summary>
    /// Returns only the given fields of each document.
    /// <para>Call it again to add more fields. Use dot notation for nested fields, such as
    /// <c>"group.number"</c>.</para>
    /// <para>Empty field names, and names with an empty part such as <c>"group..number"</c>, are ignored.</para>
    /// </summary>
    /// <param name="fields">The fields to include.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Select(string[] fields)
    {
        _selectBuilder.Select(fields);

        return this;
    }

    /// <summary>
    /// Leaves the given fields out of each document.
    /// <para>Call it again to leave out more fields. Use dot notation for nested fields, such as
    /// <c>"group.number"</c>.</para>
    /// <para>Empty field names, and names with an empty part such as <c>"group..number"</c>, are ignored.</para>
    /// </summary>
    /// <param name="fields">The fields to leave out.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Exclude(string[] fields)
    {
        _selectBuilder.Exclude(fields);

        return this;
    }

    /// <summary>
    /// Chooses which fields are returned for related documents from the given collection.
    /// <para>Only affects related documents that are returned in full, so it needs
    /// <see cref="Depth"/> set to 1 or more.</para>
    /// <para>Call it again for the same collection to add more fields. Use dot notation for nested
    /// fields, such as <c>"group.number"</c>.</para>
    /// <para>An empty collection name is ignored.</para>
    /// </summary>
    /// <param name="collection">The collection the related documents belong to, such as <c>"users"</c>.</param>
    /// <param name="fields">The fields to return for those documents.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Populate(string collection, string[] fields)
    {
        _populateBuilder.Populate(collection, fields);

        return this;
    }

    /// <summary>
    /// Adds filter conditions to the query.
    /// <para>Repeated calls add to the same set of conditions.</para>
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> instance used to define the conditions.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Where(Action<WhereBuilder> callback)
    {
        callback(_whereBuilder);

        return this;
    }

    /// <summary>
    /// Configures the documents returned in join fields: lists of documents that point back to this
    /// one, such as a post's comments.
    /// <para>Each join field can be limited, paged, sorted, filtered or turned off on its own.</para>
    /// </summary>
    /// <param name="callback">Receives a <see cref="JoinBuilder"/> instance used to configure the join fields.</param>
    /// <returns>The current <see cref="QueryBuilder"/> instance.</returns>
    public QueryBuilder Join(Action<JoinBuilder> callback)
    {
        callback(_joinBuilder);

        return this;
    }

    /// <summary>
    /// Builds the final query parameters object from all configured fields.
    /// </summary>
    /// <returns>The query parameters, keyed by parameter name.</returns>
    public Dictionary<string, object?> Build()
    {
        var where = _whereBuilder.Build();
        var select = _selectBuilder.Build();
        var populate = _populateBuilder.Build();
        var joins = _joinBuilder.Build();
        var result = new Dictionary<string, object?>();

        if (_limit != null)
        {
            result["limit"] = _limit;
        }

        if (_page != null)
        {
            result["page"] = _page;
        }

        if (_pagination != null)
        {
            result["pagination"] = _pagination;
        }

        if (_sort != null)
        {
            result["sort"] = _sort;
        }

        if (_depth != null)
        {
            result["depth"] = _depth;
        }

        if (_locale != null)
        {
            result["locale"] = _locale;
        }

        if (_fallbackLocale != null)
        {
            result["fallback-locale"] = _fallbackLocale;
        }

        if (select != null)
        {
            result["select"] = select;
        }

        if (populate != null)
        {
            result["populate"] = populate;
        }

        if (where != null)
        {
            result["where"] = where;
        }

        if (joins != null)
        {
            result["joins"] = joins;
        }

        foreach (var customParam in _customParams)
        {
            result[customParam.Key] = customParam.Value;
        }

        return result;
    }
}

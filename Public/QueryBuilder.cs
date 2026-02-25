using Payload.CMS.Public.Enums;

namespace Payload.CMS.Public;

/// <summary>
/// Fluent builder for Payload CMS REST API query parameters.
/// <para>Delegates filtering to <see cref="WhereBuilder"/> and
/// join configuration to <see cref="JoinBuilder"/>.</para>
/// </summary>
public class QueryBuilder
{
    private int? _limit;
    private int? _page;
    private string? _sort;
    private int? _depth;
    private string? _locale;
    private string? _fallbackLocale;
    private string? _select;
    private string? _populate;
    private readonly WhereBuilder _whereBuilder = new WhereBuilder();
    private readonly JoinBuilder _joinBuilder = new JoinBuilder();

    /// <summary>
    /// Limits the number of documents returned.
    /// </summary>
    /// <param name="value">Maximum document count.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Limit(int value)
    {
        _limit = value;

        return this;
    }

    /// <summary>
    /// Sets the page of results to retrieve (1-based).
    /// </summary>
    /// <param name="value">The page number.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Page(int value)
    {
        _page = value;

        return this;
    }

    /// <summary>
    /// Sorts results ascending by the given field.
    /// <para>Can be called multiple times for multi-field sorts.</para>
    /// </summary>
    /// <param name="field">The field name to sort by.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Sort(string field)
    {
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
    /// Sorts results descending by the given field.
    /// <para>Automatically prefixes the field with <c>-</c> if needed.</para>
    /// </summary>
    /// <param name="field">The field name to sort by.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder SortByDescending(string field)
    {
        var _field = field.StartsWith('-') ? field : $"-{field}";

        return Sort(_field);
    }

    /// <summary>
    /// Sets the population <c>depth</c> for related documents.
    /// </summary>
    /// <param name="value">Depth level (0 = none, 1 = direct, etc.).</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Depth(int value)
    {
        _depth = value;

        return this;
    }

    /// <summary>
    /// Sets the <c>locale</c> for querying localized fields.
    /// </summary>
    /// <param name="value">A locale string (e.g. <c>en</c>, <c>sv</c>).</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Locale(string value)
    {
        _locale = value;

        return this;
    }

    /// <summary>
    /// Sets a <c>fallback locale</c> when localized values are missing.
    /// </summary>
    /// <param name="value">A fallback locale string (e.g. <c>en</c>).</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder FallbackLocale(string value)
    {
        _fallbackLocale = value;

        return this;
    }

    /// <summary>
    /// Specifies which fields to include in the result.
    /// <para>Supports dot notation for nested selections
    /// (e.g. <c>title</c>, <c>author.name</c>).</para>
    /// <para>Can be called multiple times to accumulate fields.</para>
    /// </summary>
    /// <param name="fields">Field names to include.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Select(string[] fields)
    {
        if (_select == null)
        {
            _select = string.Join(',', fields);
        }
        else
        {
            _select += $",{string.Join(',', fields)}";
        }

        return this;
    }

    /// <summary>
    /// Flags top-level <c>relationship</c> fields for population.
    /// </summary>
    /// <param name="fields">Relationship field names to populate.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Populate(string[] fields)
    {
        _populate = string.Join(',', fields);

        return this;
    }

    /// <summary>
    /// Adds a field comparison to the <c>where</c> clause.
    /// <para>Delegates to the internal <see cref="WhereBuilder"/>.</para>
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Where(string field, Operator op, object? value)
    {
        _whereBuilder.Where(field, op, value);

        return this;
    }

    /// <summary>
    /// Adds a nested <c>AND</c> group of <c>where</c> conditions.
    /// <para>Delegates to a fresh <see cref="WhereBuilder"/> via callback.</para>
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> for nested conditions.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder And(Action<WhereBuilder> callback)
    {
        _whereBuilder.And(callback);

        return this;
    }

    /// <summary>
    /// Adds a nested <c>OR</c> group of <c>where</c> conditions.
    /// <para>Delegates to a fresh <see cref="WhereBuilder"/> via callback.</para>
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> for nested conditions.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Or(Action<WhereBuilder> callback)
    {
        _whereBuilder.Or(callback);

        return this;
    }

    /// <summary>
    /// Configures <c>Join Field</c> population via callback.
    /// <para>Delegates to the internal <see cref="JoinBuilder"/> for
    /// filtering, sorting, and limiting joined data.</para>
    /// </summary>
    /// <param name="callback">Receives the <see cref="JoinBuilder"/> instance.</param>
    /// <returns>The current builder for chaining.</returns>
    public QueryBuilder Join(Action<JoinBuilder> callback)
    {
        callback(_joinBuilder);

        return this;
    }

    /// <summary>
    /// Builds the final query parameters object.
    /// <para>Serializes all configured options into a plain
    /// object for query string encoding.</para>
    /// </summary>
    /// <returns>Query parameters ready for serialization.</returns>
    public Dictionary<string, object?> Build()
    {
        var where = _whereBuilder.Build();
        var result = new Dictionary<string, object?>();

        if (_limit != null)
        {
            result["limit"] = _limit;
        }

        if (_page != null)
        {
            result["page"] = _page;
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

        if (_select != null)
        {
            result["select"] = _select;
        }

        if (_populate != null)
        {
            result["populate"] = _populate;
        }

        if (where != null)
        {
            result["where"] = where;
        }

        if (_joinBuilder.IsDisabled)
        {
            result["joins"] = false;
        }
        else
        {
            var joins = _joinBuilder.Build();

            if (joins != null)
            {
                result["joins"] = joins;
            }
        }

        return result;
    }
}

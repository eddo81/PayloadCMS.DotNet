using Payload.CMS.Internal;
using Payload.CMS.Public.Enums;

namespace Payload.CMS.Public;

/// <summary>
/// Collects and composes <c>Join Field</c> query operations.
/// <para>Scoped to the <c>joins</c> query parameter and invoked
/// via <see cref="QueryBuilder.Join"/>.</para>
/// </summary>
public class JoinBuilder
{
    private readonly List<JoinClause> _clauses = new();
    private readonly Dictionary<string, WhereBuilder> _whereBuilders = new();
    private bool _disabled = false;

    /// <summary>
    /// Finds or creates a <see cref="JoinClause"/> for the given join field.
    /// <para>If <c>on</c> is an empty string, returns <c>null</c> and the
    /// caller should skip the operation.</para>
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name (e.g. "relatedPosts").</param>
    /// <returns>The clause instance, or <c>null</c> if <c>on</c> is empty.</returns>
    private JoinClause? _getOrCreateClause(string on)
    {
        if (on == "")
        {
            return null;
        }

        var clause = _clauses.FirstOrDefault(clause => clause.On == on);

        if (clause == null)
        {
            clause = new JoinClause(on);
            _clauses.Add(clause);
        }

        return clause;
    }

    /// <summary>
    /// Returns the <see cref="WhereBuilder"/> for the given join field.
    /// <para>Creates and caches a new instance on first access.</para>
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <returns>The cached or newly created builder.</returns>
    private WhereBuilder _getOrCreateWhereBuilder(string on)
    {
        _whereBuilders.TryGetValue(on, out WhereBuilder? builder);

        if (builder == null)
        {
            builder = new WhereBuilder();
            _whereBuilders[on] = builder;
        }

        return builder;
    }

    /// <summary>
    /// Limits the number of joined documents returned.
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="value">Maximum document count (default 10).</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Limit(string on, int value)
    {
        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Limit = value;
        }

        return this;
    }

    /// <summary>
    /// Sets the page of joined documents to retrieve (1-based).
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="value">The page number.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Page(string on, int value)
    {
        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Page = value;
        }

        return this;
    }

    /// <summary>
    /// Sorts joined documents ascending by the given field.
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="field">The field name to sort by.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Sort(string on, string field)
    {
        if (field == "")
        {
            return this;
        }

        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Sort = field;
        }

        return this;
    }

    /// <summary>
    /// Sorts joined documents descending by the given field.
    /// <para>Automatically prefixes the field with <c>-</c> if needed.</para>
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="field">The field name to sort by.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder SortByDescending(string on, string field)
    {
        var _field = field.StartsWith('-') ? field : $"-{field}";

        return Sort(on, _field);
    }

    /// <summary>
    /// Toggles the count of joined documents in the response.
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="value">Whether to include the count. Defaults to <c>true</c>.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Count(string on, bool value = true)
    {
        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Count = value;
        }

        return this;
    }

    /// <summary>
    /// Adds a <c>where</c> condition scoped to a <c>Join Field</c>.
    /// <para>Multiple calls for the same join accumulate via
    /// an internal <see cref="WhereBuilder"/> cache.</para>
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="field">The field to compare.</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Where(string on, string field, Operator op, object? value)
    {
        var builder = _getOrCreateWhereBuilder(on);

        builder.Where(field, op, value);

        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Where = builder.Build();
        }

        return this;
    }

    /// <summary>
    /// Adds a nested <c>AND</c> group scoped to a <c>Join Field</c>.
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> for nested conditions.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder And(string on, Action<WhereBuilder> callback)
    {
        var builder = _getOrCreateWhereBuilder(on);

        builder.And(callback);

        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Where = builder.Build();
        }

        return this;
    }

    /// <summary>
    /// Adds a nested <c>OR</c> group scoped to a <c>Join Field</c>.
    /// </summary>
    /// <param name="on">The <c>Join Field</c> name.</param>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> for nested conditions.</param>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Or(string on, Action<WhereBuilder> callback)
    {
        var builder = _getOrCreateWhereBuilder(on);

        builder.Or(callback);

        var clause = _getOrCreateClause(on);

        if (clause != null)
        {
            clause.Where = builder.Build();
        }

        return this;
    }

    /// <summary>
    /// Whether all joins have been explicitly disabled.
    /// <para>When <c>true</c>, the caller should set <c>joins=false</c> in the
    /// query parameters instead of calling <see cref="Build"/>.</para>
    /// </summary>
    public bool IsDisabled
    {
        get { return _disabled; }
    }

    /// <summary>
    /// Disables all <c>Join Fields</c> for the query.
    /// <para>Sets <c>joins=false</c> in the query string, overriding
    /// any previously configured join clauses.</para>
    /// </summary>
    /// <returns>The current builder for chaining.</returns>
    public JoinBuilder Disable()
    {
        _disabled = true;

        return this;
    }

    /// <summary>
    /// Builds the <c>joins</c> query parameter object.
    /// </summary>
    /// <returns>The joins object, or <c>null</c> if no clauses have been added.</returns>
    public Dictionary<string, object?>? Build()
    {
        if (_clauses.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, object?>();

        foreach (var clause in _clauses)
        {
            foreach (var kvp in clause.Build())
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }
}

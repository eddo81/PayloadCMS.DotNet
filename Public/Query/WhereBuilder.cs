using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Enums;

namespace PayloadCMS.DotNet.Query;

/// <summary>
/// Builds filter conditions for a query, including nested <c>and</c> and <c>or</c> groups.
/// </summary>
public class WhereBuilder
{
    private readonly List<WhereClause> _clauses = new();
    private readonly List<WhereBuilder> _and = new();
    private readonly List<WhereBuilder> _or = new();

    /// <summary>
    /// Finds or creates the <see cref="WhereClause"/> for the given field.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <returns>The clause instance.</returns>
    private WhereClause GetOrCreateClause(string field)
    {
        var clause = _clauses.FirstOrDefault(clause => clause.Field == field);

        if (clause == null)
        {
            clause = new WhereClause(field);
            _clauses.Add(clause);
        }

        return clause;
    }

    /// <summary>
    /// Adds a comparison condition for a field.
    /// <para>
    /// Multiple comparison operators can be used for the same field. If the same operator
    /// is added more than once, the most recently added value is used.
    /// </para>
    /// </summary>
    /// <param name="field">The field to filter.</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The value to compare against: a string, number, boolean, date, list or object.
    /// Throws if the value can't be sent as JSON, such as <c>NaN</c> or an object that references itself.
    /// Later changes to the value are ignored.</param>
    /// <returns>The current <see cref="WhereBuilder"/> instance.</returns>
    public WhereBuilder Field(string field, Operator op, object? value)
    {
        var clause = GetOrCreateClause(field);
        
        clause.Set(op, value.ToNode());

        return this;
    }

    /// <summary>
    /// Adds a group of conditions that must all match.
    /// <para>
    /// Use this to combine conditions that need to be evaluated together. The group can
    /// contain additional <c>and</c> and <c>or</c> groups.
    /// </para>
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> instance used to define the conditions in the group.</param>
    /// <returns>The current <see cref="WhereBuilder"/> instance.</returns>
    public WhereBuilder And(Action<WhereBuilder> callback)
    {
        var builder = new WhereBuilder();

        callback(builder);

        _and.Add(builder);

        return this;
    }

    /// <summary>
    /// Adds an alternative condition, where at least one alternative must match.
    /// <para>
    /// Each call adds one alternative. Conditions inside the same callback must all match
    /// for that alternative to match.
    /// An alternative can contain additional <c>and</c> and <c>or</c> groups.
    /// </para>
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> instance used to define the conditions of the alternative.</param>
    /// <returns>The current <see cref="WhereBuilder"/> instance.</returns>
    public WhereBuilder Or(Action<WhereBuilder> callback)
    {
        var builder = new WhereBuilder();

        callback(builder);

        _or.Add(builder);

        return this;
    }

    /// <summary>
    /// Builds the final <c>where</c> object from all conditions and groups.
    /// </summary>
    /// <returns>The where object, or <c>null</c> if no conditions or groups were configured.</returns>
    internal JsonObject? Build()
    {
        if (_clauses.Count == 0 && _and.Count == 0 && _or.Count == 0)
        {
            return null;
        }

        var result = new JsonObject();

        foreach (var clause in _clauses)
        {
            foreach (var entry in clause.Build())
            {
                // A node can belong to one tree only, so it is attached as a copy.
                result[entry.Key] = entry.Value?.DeepClone();
            }
        }

        if (_and.Count > 0)
        {
            var levels = new JsonArray();

            foreach (var level in _and)
            {
                levels.Add(level.Build() ?? new JsonObject());
            }

            result["and"] = levels;
        }

        if (_or.Count > 0)
        {
            var levels = new JsonArray();

            foreach (var level in _or)
            {
                levels.Add(level.Build() ?? new JsonObject());
            }

            result["or"] = levels;
        }

        return result;
    }
}

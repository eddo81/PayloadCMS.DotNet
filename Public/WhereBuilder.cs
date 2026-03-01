using PayloadCMS.DotNet.Internal;
using PayloadCMS.DotNet.Internal.Contracts;
using PayloadCMS.DotNet.Enums;

namespace PayloadCMS.DotNet;

/// <summary>
/// Fluent builder for nested <c>where</c>/<c>and</c>/<c>or</c> clauses.
/// </summary>
public class WhereBuilder
{
    private readonly List<IClause> _clauses = new();

    /// <summary>
    /// Adds a field comparison clause.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder for chaining.</returns>
    public WhereBuilder Where(string field, Operator op, object? value)
    {
        _clauses.Add(new WhereClause(field, op, value));

        return this;
    }

    /// <summary>
    /// Adds a nested <c>AND</c> group of clauses.
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> for composing nested conditions.</param>
    /// <returns>The current builder for chaining.</returns>
    public WhereBuilder And(Action<WhereBuilder> callback)
    {
        var builder = new WhereBuilder();

        callback(builder);

        _clauses.Add(new AndClause(builder._clauses));

        return this;
    }

    /// <summary>
    /// Adds a nested <c>OR</c> group of clauses.
    /// </summary>
    /// <param name="callback">Receives a <see cref="WhereBuilder"/> for composing nested conditions.</param>
    /// <returns>The current builder for chaining.</returns>
    public WhereBuilder Or(Action<WhereBuilder> callback)
    {
        var builder = new WhereBuilder();

        callback(builder);

        _clauses.Add(new OrClause(builder._clauses));

        return this;
    }

    /// <summary>
    /// Builds the <c>where</c> clause object.
    /// </summary>
    /// <returns>The clause object, or <c>null</c> if no clauses have been added.</returns>
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

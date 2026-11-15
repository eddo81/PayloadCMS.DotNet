using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Enums;

namespace PayloadCMS.DotNet.Internal;

/// <summary>
/// The operators applied to one field of a <c>where</c> level.
/// </summary>
internal class WhereClause
{
    /// <summary>The field the operators apply to.</summary>
    public readonly string Field;

    private readonly Dictionary<string, JsonNode?> _operators = new();

    public WhereClause(string field)
    {
        Field = field;
    }

    /// <summary>
    /// Sets the value compared against for an operator.
    /// <para>Setting an operator that is already set replaces its value.</para>
    /// </summary>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The value to compare against.</param>
    public void Set(Operator op, JsonNode? value)
    {
        _operators[op.ToStringValue()] = value;
    }

    /// <summary>
    /// Serializes the clause into a <c>field[operator]=value</c> structure.
    /// </summary>
    /// <returns>The field with each of its operators.</returns>
    public JsonObject Build()
    {
        var inner = new JsonObject();

        foreach (var (op, value) in _operators)
        {
            // A node can belong to one tree only, so it is attached as a copy.
            inner[op] = value?.DeepClone();
        }

        var result = new JsonObject
        {
            [Field] = inner
        };

        return result;
    }
}

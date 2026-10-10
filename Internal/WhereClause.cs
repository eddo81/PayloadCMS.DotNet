using PayloadCMS.DotNet.Internal.Contracts;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Enums;

namespace PayloadCMS.DotNet.Internal;

/// <summary>
/// The operators applied to one field of a <c>where</c> level.
/// </summary>
internal class WhereClause : IClause
{
    /// <summary>The field the operators apply to.</summary>
    public readonly string Field;

    private readonly Dictionary<string, object?> _operators = new();

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
    public void Set(Operator op, object? value)
    {
        _operators[op.ToStringValue()] = value;
    }

    /// <summary>
    /// Serializes the clause into a <c>field[operator]=value</c> structure.
    /// </summary>
    /// <returns>The field with each of its operators.</returns>
    public Dictionary<string, object?> Build()
    {
        var inner = new Dictionary<string, object?>(_operators);

        var result = new Dictionary<string, object?>
        {
            [Field] = inner
        };

        return result;
    }
}

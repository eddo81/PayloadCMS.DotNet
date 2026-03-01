using PayloadCMS.DotNet.Internal.Contracts;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Enums;

namespace PayloadCMS.DotNet.Internal;

/// <summary>
/// Represents a single field comparison condition.
/// </summary>
internal class WhereClause : IClause
{
    private readonly string _field;
    private readonly Operator _operator;
    private readonly object? _value;

    public WhereClause(string field, Operator op, object? value)
    {
        _field = field;
        _operator = op;
        _value = value;
    }

    /// <summary>
    /// Serializes the clause into a <c>field[operator]=value</c> structure.
    /// </summary>
    /// <returns>The serialized field comparison structure.</returns>
    public Dictionary<string, object?> Build()
    {
        var inner = new Dictionary<string, object?>
        {
            [_operator.ToStringValue()] = _value
        };

        var result = new Dictionary<string, object?>
        {
            [_field] = inner
        };

        return result;
    }
}

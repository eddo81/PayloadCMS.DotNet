using Payload.CMS.Internal.Contracts;

namespace Payload.CMS.Internal;

/// <summary>
/// Represents a logical <c>OR</c> grouping of <see cref="IClause"/> instances.
/// </summary>
internal class OrClause : IClause
{
    private readonly List<IClause> _clauses;

    public OrClause(List<IClause> clauses)
    {
        _clauses = clauses;
    }

    /// <summary>
    /// Serializes the clause into an <c>or</c> array structure.
    /// </summary>
    /// <returns>The serialized <c>or</c> clause array.</returns>
    public Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>();

        result["or"] = _clauses
            .Select(clause => clause.Build())
            .ToList();

        return result;
    }
}

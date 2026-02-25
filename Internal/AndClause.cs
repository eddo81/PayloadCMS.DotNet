using Payload.CMS.Internal.Contracts;

namespace Payload.CMS.Internal;

/// <summary>
/// Represents a logical <c>AND</c> grouping of <see cref="IClause"/> instances.
/// </summary>
internal class AndClause : IClause
{
    private readonly List<IClause> _clauses;
    public AndClause(List<IClause> clauses) {
        _clauses = clauses;
    }

    /// <summary>
    /// Serializes the clause into an <c>and</c> array structure.
    /// </summary>
    /// <returns>The serialized <c>and</c> clause array.</returns>
    public Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>();

        result["and"] = _clauses
            .Select(clause => clause.Build())
            .ToList();

        return result;
    }
}

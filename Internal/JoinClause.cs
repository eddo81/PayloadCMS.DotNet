using PayloadCMS.DotNet.Internal.Contracts;

namespace PayloadCMS.DotNet.Internal;

/// <summary>
/// Accumulates join-scoped operations for a single <c>Join Field</c>.
/// <para>Used internally by <see cref="PayloadCMS.DotNet.Query.JoinBuilder"/> to collect
/// <c>limit</c>, <c>page</c>, <c>sort</c>, <c>count</c> and <c>where</c> per join target, or to
/// switch that target off entirely.</para>
/// </summary>
internal class JoinClause : IClause
{
    /// <summary>The <c>Join Field</c> name this clause targets.</summary>
    public readonly string On;
    public int? Limit;
    public int? Page;
    public string? Sort;
    public bool? Count;
    public Dictionary<string, object?>? Where;

    /// <summary>Whether this join field is switched off for the query.</summary>
    public bool Disabled;

    public JoinClause(string on)
    {
        On = on;
    }

    /// <summary>
    /// Collects the options configured for this join field.
    /// </summary>
    /// <returns>The options object that sits under the join field's name.</returns>
    private Dictionary<string, object?> Join()
    {
        var join = new Dictionary<string, object?>();

        if (Limit != null) 
        {
            join["limit"] = Limit;
        }

        if (Page != null)
        {
            join["page"] = Page;
        }

        if (Sort != null)
        {
            join["sort"] = Sort;
        }

        if (Count != null)
        {
            join["count"] = Count;
        }

        if (Where != null) 
        {
            join["where"] = Where;
        }

        return join;
    }

    /// <summary>
    /// Serializes the clause into a Payload-compatible join structure.
    /// </summary>
    /// <returns>A nested object keyed by the join field name.</returns>
    public Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>
        {
            [On] = Disabled ? false : Join()
        };

        return result;
    }
}

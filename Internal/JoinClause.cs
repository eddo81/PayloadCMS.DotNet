using PayloadCMS.DotNet.Internal.Contracts;

namespace PayloadCMS.DotNet.Internal;

/// <summary>
/// Accumulates join-scoped operations for a single <c>Join Field</c>.
/// <para>Used internally by <see cref="PayloadCMS.DotNet.JoinBuilder"/> to collect <c>limit</c>,
/// <c>sort</c>, <c>where</c>, and <c>count</c> per join target.</para>
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

    public JoinClause(string on)
    {
        On = on;
    }

    /// <summary>
    /// Serializes the clause into a Payload-compatible join structure.
    /// </summary>
    /// <returns>A nested object keyed by the join field name.</returns>
    public Dictionary<string, object?> Build()
    {
        var inner = new Dictionary<string, object?>();

        if (Limit != null) 
        {
            inner["limit"] = Limit;
        }

        if (Page != null)
        {
            inner["page"] = Page;
        }

        if (Sort != null)
        {
            inner["sort"] = Sort;
        }

        if (Count != null)
        {
            inner["count"] = Count;
        }

        if (Where != null) 
        {
            inner["where"] = Where;
        }

        var result = new Dictionary<string, object?>
        {
            [On] = inner
        };

        return result;
    }
}

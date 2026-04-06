using PayloadCMS.DotNet.Internal.Contracts;

namespace PayloadCMS.DotNet.Internal;

/// <summary>
/// Represents a single field path in a <c>select</c> clause,
/// targeting either inclusion (<c>true</c>) or exclusion (<c>false</c>).
/// <para>Used internally by <see cref="PayloadCMS.DotNet.Query.SelectBuilder"/>.</para>
/// </summary>
internal class SelectClause : IClause
{
    private readonly string[] _segments;
    private readonly bool _value;

    public SelectClause(string[] segments, bool value)
    {
        _segments = segments;
        _value = value;
    }

    /// <summary>
    /// Builds the nested dictionary structure for this field path.
    /// <para>e.g. segments <c>["group", "number"]</c> with <c>value = true</c>
    /// produces <c>{ "group": { "number": true } }</c>.</para>
    /// </summary>
    /// <returns>The nested field structure for this clause.</returns>
    public Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?> { 
          [_segments[_segments.Length - 1]] = _value 
        };

        for (int i = _segments.Length - 2; i >= 0; i--)
        {
            result = new Dictionary<string, object?> { 
              [_segments[i]] = result 
            };
        }

        return result;
    }
}

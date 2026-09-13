using PayloadCMS.DotNet.Internal;

namespace PayloadCMS.DotNet.Query;

/// <summary>
/// Fluent builder for Payload CMS <c>select</c> query parameters.
///
/// <para>Composes a set of <see cref="SelectClause"/> entries into a
/// nested field-inclusion/exclusion tree that the
/// <c>QueryStringEncoder</c> serializes to bracket notation
/// (e.g. <c>select[group][number]=true</c>).</para>
///
/// <para>Use dot notation to target nested fields:
/// <c>"group.number"</c> → <c>select[group][number]=true</c>.</para>
/// </summary>
public class SelectBuilder
{
    private readonly List<SelectClause> _clauses = new();

    /// <summary>
    /// Marks fields for inclusion in the response.
    /// <para>Use dot notation for nested paths (e.g. <c>"group.number"</c>).</para>
    /// <para>Fields with an empty name or an empty path segment (e.g. <c>""</c>,
    /// <c>"group..number"</c>, <c>".number"</c>) are ignored.</para>
    /// </summary>
    /// <param name="fields">Field names to include.</param>
    /// <returns>The current builder for chaining.</returns>
    public SelectBuilder Select(string[] fields)
    {
        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field))
            {
                continue;
            }

            var segments = field.Split('.');

            if (HasEmptySegment(segments))
            {
                continue;
            }

            _clauses.Add(new SelectClause(segments, true));
        }

        return this;
    }

    /// <summary>
    /// Marks fields for exclusion from the response.
    /// <para>Use dot notation for nested paths (e.g. <c>"group.number"</c>).</para>
    /// <para>Fields with an empty name or an empty path segment (e.g. <c>""</c>,
    /// <c>"group..number"</c>, <c>".number"</c>) are ignored.</para>
    /// </summary>
    /// <param name="fields">Field names to exclude.</param>
    /// <returns>The current builder for chaining.</returns>
    public SelectBuilder Exclude(string[] fields)
    {
        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field))
            {
                continue;
            }

            var segments = field.Split('.');

            if (HasEmptySegment(segments))
            {
                continue;
            }

            _clauses.Add(new SelectClause(segments, false));
        }

        return this;
    }

    /// <summary>
    /// Builds the final <c>select</c> object by deep-merging all clauses.
    /// </summary>
    /// <returns>The merged field map, or <c>null</c> if no fields were configured.</returns>
    public Dictionary<string, object?>? Build()
    {
        if (_clauses.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, object?>();

        foreach (var clause in _clauses)
        {
            MergeSelectClauses(result, clause.Build());
        }

        return result;
    }

    private static bool HasEmptySegment(string[] segments)
    {
        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return true;
            }
        }

        return false;
    }

    private static void MergeSelectClauses(Dictionary<string, object?> target, Dictionary<string, object?> source)
    {
        foreach (var (key, value) in source)
        {
            if (target.ContainsKey(key) && target[key] is Dictionary<string, object?> existingNested && value is Dictionary<string, object?> sourceNested)
            {
                MergeSelectClauses(existingNested, sourceNested);
            }
            else
            {
                target[key] = value;
            }
        }
    }
}

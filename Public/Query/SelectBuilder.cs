using PayloadCMS.DotNet.Internal;

namespace PayloadCMS.DotNet.Query;

/// <summary>
/// Builds the list of fields to return with, or leave out of, each document.
/// <para>Used through <see cref="QueryBuilder.Select"/> and <see cref="QueryBuilder.Exclude"/>.</para>
/// </summary>
public class SelectBuilder
{
    private readonly List<SelectClause> _clauses = new();

    /// <summary>
    /// Determines whether any part of a dot-separated field name is empty.
    /// </summary>
    /// <param name="segments">The parts of the field name, split on <c>.</c>.</param>
    /// <returns><c>true</c> if any part is empty, such as in <c>"group..number"</c> or <c>".number"</c>; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Merges one field's entry into the combined <c>select</c> object.
    /// <para>Where both hold nested fields under the same name, the nested fields are merged.
    /// Otherwise the incoming value replaces the existing one, so a later call wins.</para>
    /// </summary>
    /// <param name="target">The combined object being built, changed in place.</param>
    /// <param name="source">The entry to merge in.</param>
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

    /// <summary>
    /// Returns only the given fields of each document.
    /// <para>Call it again to add more fields. Use dot notation for nested fields, such as
    /// <c>"group.number"</c>.</para>
    /// <para>Empty field names, and names with an empty part such as <c>"group..number"</c>, are ignored.</para>
    /// </summary>
    /// <param name="fields">The fields to include.</param>
    /// <returns>The current <see cref="SelectBuilder"/> instance.</returns>
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
    /// Leaves the given fields out of each document.
    /// <para>Call it again to leave out more fields. Use dot notation for nested fields, such as
    /// <c>"group.number"</c>.</para>
    /// <para>Empty field names, and names with an empty part such as <c>"group..number"</c>, are ignored.</para>
    /// </summary>
    /// <param name="fields">The fields to leave out.</param>
    /// <returns>The current <see cref="SelectBuilder"/> instance.</returns>
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
    /// Builds the final <c>select</c> object from all configured fields.
    /// </summary>
    /// <returns>The <c>select</c> object, or <c>null</c> if no fields were configured.</returns>
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
}

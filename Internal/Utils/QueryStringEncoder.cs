namespace PayloadCMS.DotNet.Internal.Utils;
/// <summary>
/// Serializes nested objects into Payload CMS query strings.
/// <para>Preserves Payload's bracketed syntax (e.g. <c>where[title][equals]=foo</c>)
/// while URL-encoding values. Square brackets <c>[]</c> and commas <c>,</c> are
/// left unescaped by default as they carry semantic meaning.</para>
/// </summary>
internal class QueryStringEncoder
{
    private readonly bool _addQueryPrefix;
    private readonly bool _strictEncoding;
    private string _prefix
    {
        get { return _addQueryPrefix ? "?" : ""; }
    }

    /// <summary>
    /// Creates a new <see cref="QueryStringEncoder"/>.
    /// </summary>
    /// <param name="addQueryPrefix">Prefix output with <c>?</c>. Defaults to <c>true</c>.</param>
    /// <param name="strictEncoding">Keep brackets and commas percent-encoded. Defaults to <c>false</c>.</param>
    public QueryStringEncoder(bool? addQueryPrefix = null, bool? strictEncoding = null)
    {
        _addQueryPrefix = addQueryPrefix ?? true;
        _strictEncoding = strictEncoding ?? false;
    }

    /// <summary>
    /// Converts an object into a Payload-compatible query string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The query string (prefixed with <c>?</c>), or empty string.</returns>
    public string Stringify(Dictionary<string, object?> obj)
    {
        var result = Serialize(obj, parentKey: "") ?? "";

        if (string.IsNullOrEmpty(result))
        {
            return "";
        }

        return $"{_prefix}{result}";
    }

    /// <summary>
    /// Encodes a string for safe query string inclusion.
    /// <para>Preserves <c>[]</c> and <c>,</c> which carry semantic meaning in Payload CMS query syntax.</para>
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <returns>The encoded string.</returns>
    private string SafeEncode(string value)
    {
        var encoded = Uri.EscapeDataString(value);

        if (_strictEncoding)
        {
            return encoded;
        }

        return encoded
            .Replace("%5B", "[")
            .Replace("%5D", "]")
            .Replace("%2C", ",");
    }

    /// <summary>
    /// Determines whether a value is a serializable primitive.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><c>true</c> if serializable as a terminal node.</returns>
    private bool IsPrimitive(object? value)
    {
        return value is string or int or long or double or float or decimal or bool or DateTime;
    }

    /// <summary>
    /// Determines whether a value is a plain dictionary object.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><c>true</c> if the value is a nested object.</returns>
    private bool IsPlainObject(object? value)
    {
        return value is Dictionary<string, object?>;
    }

    /// <summary>
    /// Recursively serializes an object into query string segments.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="parentKey">The accumulated key path (e.g. <c>where[title]</c>).</param>
    /// <returns>A query string fragment, or <c>null</c> if empty.</returns>
    private string? Serialize(Dictionary<string, object?> obj, string parentKey)
    {
        var segments = new List<string>();

        foreach (var (key, value) in obj)
        {
            // Skip null entries.
            if (value is null)
            {
                continue;
            }

            // Build the current key path, preserving bracket notation.
            var encodedKey = string.IsNullOrEmpty(parentKey)
                ? SafeEncode(key)
                : $"{parentKey}[{SafeEncode(key)}]";

            // Handle primitive values first — these are terminal nodes in the structure.
            if (IsPrimitive(value))
            {
                var encoded = SerializePrimitive(encodedKey, value);
                if (encoded != null)
                {
                    segments.Add(encoded);
                }
                continue;
            }

            // Handle arrays recursively. Uses IList to support List<T> of any element type.
            if (value is System.Collections.IList arr)
            {
                SerializeArray(arr, encodedKey, segments);
                continue;
            }

            // Recursively serialize nested objects into query segments.
            if (IsPlainObject(value))
            {
                var nested = Serialize((Dictionary<string, object?>)value, encodedKey);
                if (nested != null)
                {
                    segments.Add(nested);
                }
                continue;
            }

            // Unsupported types are skipped implicitly.
        }

        var joined = string.Join("&", segments);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    /// <summary>
    /// Serializes an array using index-based notation.
    /// </summary>
    /// <param name="arr">The array to serialize.</param>
    /// <param name="parentKey">The current key path (e.g. <c>where[tags]</c>).</param>
    /// <param name="segments">The accumulated query segments to append to.</param>
    private void SerializeArray(System.Collections.IList arr, string parentKey, List<string> segments)
    {
        for (int i = 0; i < arr.Count; i++)
        {
            var value = arr[i];
            var elementKey = $"{parentKey}[{i}]";

            // Skip null entries.
            if (value is null)
            {
                continue;
            }

            // Handle primitive values first — these are terminal nodes in the structure.
            if (IsPrimitive(value))
            {
                var encoded = SerializePrimitive(elementKey, value);
                if (encoded != null)
                {
                    segments.Add(encoded);
                }
                continue;
            }

            // Handle nested arrays recursively.
            if (value is System.Collections.IList nestedArr)
            {
                SerializeArray(nestedArr, elementKey, segments);
                continue;
            }

            // Recursively serialize nested objects into query segments.
            if (IsPlainObject(value))
            {
                var nested = Serialize((Dictionary<string, object?>)value, elementKey);
                if (nested != null)
                {
                    segments.Add(nested);
                }
                continue;
            }

            // Unsupported types are skipped implicitly.
        }
    }

    /// <summary>
    /// Serializes a primitive into a <c>key=value</c> pair.
    /// </summary>
    /// <param name="key">The full key path (e.g. <c>where[title][equals]</c>).</param>
    /// <param name="value">The primitive value to encode.</param>
    /// <returns>A <c>key=value</c> string, or <c>null</c> if unsupported.</returns>
    private string? SerializePrimitive(string key, object? value)
    {
        if (value is DateTime dt)
        {
            return $"{key}={SafeEncode(dt.ToString("O"))}";
        }

        // Serialize bool as lowercase "true"/"false" — C# ToString() gives "True"/"False".
        if (value is bool b)
        {
            return $"{key}={SafeEncode(b ? "true" : "false")}";
        }

        return $"{key}={SafeEncode(value!.ToString()!)}";
    }
}

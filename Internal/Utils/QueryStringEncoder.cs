using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    /// <param name="strictEncoding">Percent-encode brackets and commas. Defaults to <c>false</c>, which leaves [, ] and , literal.</param>
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
    public string Stringify(JsonObject obj)
    {
        var result = Serialize(obj, parentKey: "") ?? "";

        if (string.IsNullOrEmpty(result))
        {
            return "";
        }

        return $"{_prefix}{result}";
    }

    /// <summary>
    /// Appends a bracketed key segment to a key path.
    /// </summary>
    /// <param name="parentKey">The accumulated key path, already encoded.</param>
    /// <param name="segment">The segment to append, already encoded.</param>
    /// <returns>The extended key path.</returns>
    private string Bracket(string parentKey, string segment)
    {
        var openBracket = SafeEncode("[");
        var closeBracket = SafeEncode("]");

        return $"{parentKey}{openBracket}{segment}{closeBracket}";
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
    /// Recursively serializes an object into query string segments.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="parentKey">The accumulated key path (e.g. <c>where[title]</c>).</param>
    /// <returns>A query string fragment, or <c>null</c> if empty.</returns>
    private string? Serialize(JsonObject obj, string parentKey)
    {
        var segments = new List<string>();

        foreach (var (key, value) in obj)
        {
            // Build the current key path, preserving bracket notation.
            var encodedKey = string.IsNullOrEmpty(parentKey)
                ? SafeEncode(key)
                : Bracket(parentKey, SafeEncode(key));

            // A null keeps its key and renders as an empty value. Dropping it would remove the
            // filter and return more documents than the caller asked for.
            if (value is null)
            {
                segments.Add($"{encodedKey}=");
                continue;
            }

            // Handle primitive values first — these are terminal nodes in the structure.
            if (value is JsonValue primitive)
            {
                segments.Add(SerializePrimitive(encodedKey, primitive));
                continue;
            }

            // Handle arrays recursively.
            if (value is JsonArray arr)
            {
                SerializeArray(arr, encodedKey, segments);
                continue;
            }

            // Recursively serialize nested objects into query segments.
            if (value is JsonObject nestedObj)
            {
                var nested = Serialize(nestedObj, encodedKey);
                if (nested != null)
                {
                    segments.Add(nested);
                }
            }
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
    private void SerializeArray(JsonArray arr, string parentKey, List<string> segments)
    {
        for (int i = 0; i < arr.Count; i++)
        {
            var value = arr[i];
            var elementKey = Bracket(parentKey, i.ToString(CultureInfo.InvariantCulture));

            // A null keeps its key and renders as an empty value.
            if (value is null)
            {
                segments.Add($"{elementKey}=");
                continue;
            }

            // Handle primitive values first — these are terminal nodes in the structure.
            if (value is JsonValue primitive)
            {
                segments.Add(SerializePrimitive(elementKey, primitive));
                continue;
            }

            // Handle nested arrays recursively.
            if (value is JsonArray nestedArr)
            {
                SerializeArray(nestedArr, elementKey, segments);
                continue;
            }

            // Recursively serialize nested objects into query segments.
            if (value is JsonObject nestedObj)
            {
                var nested = Serialize(nestedObj, elementKey);
                if (nested != null)
                {
                    segments.Add(nested);
                }
            }
        }
    }

    /// <summary>
    /// Serializes a primitive into a <c>key=value</c> pair.
    /// </summary>
    /// <param name="key">The full key path (e.g. <c>where[title][equals]</c>).</param>
    /// <param name="value">The primitive value to encode.</param>
    /// <returns>A <c>key=value</c> string.</returns>
    private string SerializePrimitive(string key, JsonValue value)
    {
        // A string is written as its value. A number or boolean is written as its JSON literal,
        // which is culture-invariant and lowercase.
        var text = value.ToJsonString();

        if (value.GetValueKind() == JsonValueKind.String)
        {
            text = value.GetValue<string>();
        }

        return $"{key}={SafeEncode(text)}";
    }
}

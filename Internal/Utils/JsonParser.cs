using System.Text;
using System.Text.Json;

namespace PayloadCMS.DotNet.Internal.Utils;

/// <summary>
/// Handles JSON serialization and deserialization at the HTTP boundary.
/// <para>Bridges the gap between <see cref="System.Text.Json"/>'s deferred
/// <see cref="JsonElement"/> representation and the native CLR types used
/// throughout the library (<c>Dictionary</c>, <c>List</c>, <c>string</c>,
/// <c>int</c>, <c>bool</c>, <c>null</c>).</para>
/// </summary>
internal static class JsonParser
{
    // -------------------------------------------------------------------------
    // Serialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Serializes a dictionary into an <c>application/json</c> <see cref="StringContent"/>.
    /// </summary>
    /// <param name="data">The data to serialize.</param>
    /// <returns>A <see cref="StringContent"/> with UTF-8 encoded JSON body.</returns>
    internal static StringContent Serialize(Dictionary<string, object?> data)
    {
        return new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
    }

    // -------------------------------------------------------------------------
    // Deserialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a JSON string into a <c>Dictionary&lt;string, object?&gt;</c>.
    /// </summary>
    /// <param name="text">The JSON string to parse.</param>
    /// <returns>The parsed dictionary, or <c>null</c> if the root is not a JSON object.</returns>
    /// <exception cref="JsonException">If the JSON is malformed.</exception>
    internal static Dictionary<string, object?>? Parse(string text)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(text);

        return ConvertElement(element) as Dictionary<string, object?>;
    }

    internal static object? ConvertElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertElement(property.Value);
            }

            return dict;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var list = new List<object?>();

            foreach (var item in element.EnumerateArray())
            {
                list.Add(ConvertElement(item));
            }

            return list;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt32(out int intValue))
            {
                return intValue;
            }

            if (element.TryGetInt64(out long longValue))
            {
                return longValue;
            }

            return element.GetDouble();
        }

        if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Number conversion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attempts to convert a JSON number value to <c>int</c>.
    /// <para>Necessary because <see cref="System.Text.Json"/> may produce
    /// <c>int</c>, <c>long</c>, or <c>double</c> for the same JSON number
    /// depending on its magnitude.</para>
    /// </summary>
    /// <param name="value">The raw value from the deserialized dictionary.</param>
    /// <returns>The integer value, or <c>null</c> if conversion is not possible.</returns>
    internal static int? TryConvertInt(object? value)
    {
        if (value is int integerValue)
        {
            return integerValue;
        }

        if (value is long longValue)
        {
            return (int)longValue;
        }

        if (value is double doubleValue)
        {
            return (int)doubleValue;
        }

        return null;
    }
}

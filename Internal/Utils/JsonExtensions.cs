using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PayloadCMS.DotNet.Internal.Utils;

/// <summary>
/// Handles JSON serialization and deserialization at the HTTP boundary.
/// <para>Values are written with <see cref="Options"/>. Parsed JSON is returned as a JSON tree,
/// and converted to native CLR values (<c>Dictionary</c>, <c>List</c>, <c>string</c>, numbers,
/// <c>bool</c>, <c>null</c>) where a public property exposes it.</para>
/// </summary>
internal static class JsonExtensions
{
    /// <summary>
    /// The serializer options for every value the SDK sends.
    /// <para>Dates are written as a UTC instant with millisecond precision ending in <c>Z</c>, and
    /// a <see cref="BigInteger"/> as its digits.</para>
    /// </summary>
    internal static readonly JsonSerializerOptions Options = CreateOptions();

    // -------------------------------------------------------------------------
    // Serialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a value into a JSON tree with <see cref="Options"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The JSON tree, or <c>null</c> if <paramref name="value"/> is <c>null</c>.</returns>
    /// <exception cref="ArgumentException">If a value is <c>NaN</c> or infinite.</exception>
    /// <exception cref="JsonException">If a value contains itself.</exception>
    internal static JsonNode? ToNode(this object? value)
    {
        return JsonSerializer.SerializeToNode(value, Options);
    }

    /// <summary>
    /// Serializes a dictionary into a JSON string.
    /// </summary>
    /// <param name="data">The data to serialize.</param>
    /// <returns>The JSON text.</returns>
    /// <exception cref="ArgumentException">If a value is <c>NaN</c> or infinite.</exception>
    /// <exception cref="JsonException">If a value contains itself.</exception>
    internal static string ToJsonString(this Dictionary<string, object?> data)
    {
        return JsonSerializer.Serialize(data, Options);
    }

    /// <summary>
    /// Serializes a dictionary into an <c>application/json</c> <see cref="StringContent"/>.
    /// </summary>
    /// <param name="data">The data to serialize.</param>
    /// <returns>A <see cref="StringContent"/> with UTF-8 encoded JSON body.</returns>
    /// <exception cref="ArgumentException">If a value is <c>NaN</c> or infinite.</exception>
    /// <exception cref="JsonException">If a value contains itself.</exception>
    internal static StringContent ToJsonContent(this Dictionary<string, object?> data)
    {
        return new StringContent(data.ToJsonString(), Encoding.UTF8, "application/json");
    }

    // -------------------------------------------------------------------------
    // Deserialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a JSON string into a <see cref="JsonObject"/>.
    /// <para>A key that appears more than once keeps its last value.</para>
    /// </summary>
    /// <param name="text">The JSON string to parse.</param>
    /// <returns>The parsed object, or <c>null</c> if the root is not a JSON object.</returns>
    /// <exception cref="JsonException">If the JSON is malformed.</exception>
    internal static JsonObject? ParseJson(this string text)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(text);

        return FromElement(element) as JsonObject;
    }

    /// <summary>
    /// Copies a <see cref="JsonElement"/> into a JSON tree.
    /// </summary>
    /// <param name="element">The element to copy.</param>
    /// <returns>The tree, or <c>null</c> for a JSON <c>null</c>.</returns>
    private static JsonNode? FromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var obj = new JsonObject();

            foreach (var property in element.EnumerateObject())
            {
                // Assigning replaces an earlier value, so a repeated key keeps its last value.
                obj[property.Name] = FromElement(property.Value);
            }

            return obj;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var array = new JsonArray();

            foreach (var item in element.EnumerateArray())
            {
                array.Add(FromElement(item));
            }

            return array;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return JsonValue.Create(element);
    }

    /// <summary>
    /// Converts a JSON tree into native CLR values.
    /// </summary>
    /// <param name="node">The tree to convert.</param>
    /// <returns>A <c>Dictionary</c>, <c>List</c>, <c>string</c>, number (<c>int</c>, <c>long</c> or
    /// <c>double</c>, the first that fits), <c>bool</c> or <c>null</c>.</returns>
    internal static object? ToClr(this JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var (key, child) in obj)
            {
                dict[key] = child.ToClr();
            }

            return dict;
        }

        if (node is JsonArray array)
        {
            var list = new List<object?>();

            foreach (var item in array)
            {
                list.Add(item.ToClr());
            }

            return list;
        }

        if (node is not JsonValue value)
        {
            return null;
        }

        var kind = value.GetValueKind();

        if (kind == JsonValueKind.String)
        {
            return value.GetValue<string>();
        }

        if (kind == JsonValueKind.Number)
        {
            if (value.TryGetValue(out int intValue))
            {
                return intValue;
            }

            if (value.TryGetValue(out long longValue))
            {
                return longValue;
            }

            return value.GetValue<double>();
        }

        if (kind == JsonValueKind.True || kind == JsonValueKind.False)
        {
            return value.GetValue<bool>();
        }

        return null;
    }

    /// <summary>
    /// Converts a JSON object into a <c>Dictionary&lt;string, object?&gt;</c> of native CLR values.
    /// </summary>
    /// <param name="node">The object to convert.</param>
    /// <returns>The dictionary; see <see cref="ToClr(JsonNode?)"/> for the value types.</returns>
    internal static Dictionary<string, object?> ToDictionary(this JsonObject node)
    {
        return (Dictionary<string, object?>)node.ToClr()!;
    }

    // -------------------------------------------------------------------------
    // Value conversion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attempts to read a JSON number as an <c>int</c>.
    /// <para>A fraction is truncated, and a whole number outside the <c>int</c> range wraps.</para>
    /// </summary>
    /// <param name="value">The node to read.</param>
    /// <returns>The integer value, or <c>null</c> if the node is not a number.</returns>
    internal static int? TryConvertInt(this JsonNode? value)
    {
        if (value is not JsonValue number || number.GetValueKind() != JsonValueKind.Number)
        {
            return null;
        }

        if (number.TryGetValue(out int integerValue))
        {
            return integerValue;
        }

        if (number.TryGetValue(out long longValue))
        {
            return (int)longValue;
        }

        return (int)number.GetValue<double>();
    }

    /// <summary>
    /// Attempts to read a JSON number as a <c>long</c>.
    /// <para>A fraction is truncated.</para>
    /// </summary>
    /// <param name="value">The node to read.</param>
    /// <returns>The integer value, or <c>null</c> if the node is not a number.</returns>
    internal static long? TryConvertLong(this JsonNode? value)
    {
        if (value is not JsonValue number || number.GetValueKind() != JsonValueKind.Number)
        {
            return null;
        }

        if (number.TryGetValue(out long longValue))
        {
            return longValue;
        }

        return (long)number.GetValue<double>();
    }

    /// <summary>
    /// Attempts to read a JSON string.
    /// </summary>
    /// <param name="value">The node to read.</param>
    /// <returns>The string, or <c>null</c> if the node is not a string.</returns>
    internal static string? TryConvertString(this JsonNode? value)
    {
        if (value is not JsonValue text || text.GetValueKind() != JsonValueKind.String)
        {
            return null;
        }

        return text.GetValue<string>();
    }

    /// <summary>
    /// Attempts to read a JSON boolean.
    /// </summary>
    /// <param name="value">The node to read.</param>
    /// <returns>The boolean, or <c>null</c> if the node is not <c>true</c> or <c>false</c>.</returns>
    internal static bool? TryConvertBool(this JsonNode? value)
    {
        if (value is not JsonValue flag)
        {
            return null;
        }

        var kind = flag.GetValueKind();

        if (kind != JsonValueKind.True && kind != JsonValueKind.False)
        {
            return null;
        }

        return flag.GetValue<bool>();
    }

    // -------------------------------------------------------------------------
    // Options
    // -------------------------------------------------------------------------

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new DateTimeJsonConverter());
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        options.Converters.Add(new BigIntegerJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);

        return options;
    }

    private sealed class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(DateSerializer.Serialize(value));
        }
    }

    private sealed class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTimeOffset();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(DateSerializer.Serialize(value));
        }
    }

    private sealed class BigIntegerJsonConverter : JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);

            return BigInteger.Parse(document.RootElement.GetRawText(), CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}

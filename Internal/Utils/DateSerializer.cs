using System.Globalization;

namespace PayloadCMS.DotNet.Internal.Utils;

/// <summary>
/// Serializes date values for inclusion in a query string.
/// <para>A value carrying a time is emitted as a UTC instant with millisecond precision. A
/// calendar date is emitted as <c>yyyy-MM-dd</c>.</para>
/// </summary>
internal static class DateSerializer
{
    private const string UtcIso8601Format = "yyyy-MM-ddTHH:mm:ss.fff'Z'";

    private const string DateOnlyFormat = "yyyy-MM-dd";

    /// <summary>
    /// Determines whether a value is a date type this class can serialize.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><c>true</c> if <see cref="Serialize(object)"/> accepts the value.</returns>
    internal static bool IsDateType(object? value)
    {
        return value is DateTime || value is DateTimeOffset || value is DateOnly;
    }

    /// <summary>
    /// Serializes a boxed date value by dispatching to the overload for its runtime type.
    /// </summary>
    /// <param name="value">The value to serialize. Must satisfy <see cref="IsDateType"/>.</param>
    /// <returns>An ISO 8601 string — an instant ending in <c>Z</c>, or a <c>yyyy-MM-dd</c> date.</returns>
    /// <exception cref="ArgumentException">If <paramref name="value"/> is not a date type.</exception>
    internal static string Serialize(object? value)
    {
        if (value is DateTime dt)
        {
            return Serialize(dt);
        }
        else if (value is DateTimeOffset offset)
        {
            return Serialize(offset);
        }
        else if (value is DateOnly d)
        {
            return Serialize(d);
        }

        throw new ArgumentException($"Unsupported type {value?.GetType().FullName ?? "null"} for date serialization.");
    }

    /// <summary>
    /// Serializes a <see cref="DateTime"/> as a UTC ISO 8601 instant.
    /// <para>A value whose <see cref="DateTimeKind"/> is <c>Local</c> is converted to UTC. A value
    /// with no kind is taken to already be UTC.</para>
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>An ISO 8601 string ending in <c>Z</c>.</returns>
    internal static string Serialize(DateTime value)
    {
        var universalValue = value;

        // A value carrying no timezone is read as the server's own local time, and Payload
        // compares dates at millisecond precision — so the offset is resolved here and the
        // fractional part is cut to three digits.
        if (value.Kind == DateTimeKind.Local)
        {
            universalValue = value.ToUniversalTime();
        }

        return universalValue.ToString(UtcIso8601Format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Serializes a <see cref="DateTimeOffset"/> as a UTC ISO 8601 instant.
    /// <para>The offset is resolved, so the result denotes the same instant the caller supplied. A
    /// zero offset is written as <c>Z</c> rather than <c>+00:00</c>.</para>
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>An ISO 8601 string ending in <c>Z</c>.</returns>
    internal static string Serialize(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString(UtcIso8601Format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Serializes a <see cref="DateOnly"/> as an ISO 8601 calendar date.
    /// <para>No time or timezone is written; a server reading the value resolves it to midnight
    /// UTC.</para>
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A <c>yyyy-MM-dd</c> string.</returns>
    internal static string Serialize(DateOnly value)
    {
        return value.ToString(DateOnlyFormat, CultureInfo.InvariantCulture);
    }
}

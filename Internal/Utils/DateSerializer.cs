using System.Globalization;

namespace PayloadCMS.DotNet.Internal.Utils;

/// <summary>
/// Serializes date values for inclusion in a query string.
/// <para>Every value is emitted as a UTC instant with millisecond precision.</para>
/// </summary>
internal static class DateSerializer
{
    private const string UtcIso8601Format = "yyyy-MM-ddTHH:mm:ss.fff'Z'";

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
}

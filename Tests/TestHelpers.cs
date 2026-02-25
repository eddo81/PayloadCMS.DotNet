namespace Payload.CMS.Tests;

internal static class TestHelpers
{
    /// <summary>
    /// Sorts query string segments alphabetically so tests are
    /// order-independent. Mirrors the TS <c>Normalize</c> helper.
    /// </summary>
    internal static string Normalize(string qs)
    {
        var withoutPrefix = qs.StartsWith('?') ? qs.Substring(1) : qs;
        var segments = withoutPrefix.Split('&').Where(segment => segment.Length > 0).OrderBy(segment => segment);
        var joined = string.Join("&", segments);

        return qs.StartsWith('?') ? $"?{joined}" : joined;
    }
}

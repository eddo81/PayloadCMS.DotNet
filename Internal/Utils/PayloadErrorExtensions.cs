using System.Text.Json;

namespace PayloadCMS.DotNet.Internal.Utils;

/// <summary>
/// Helpers for interpreting the response body carried by a <see cref="PayloadError"/>.
/// </summary>
internal static class PayloadErrorExtensions
{
    /// <summary>
    /// Reads the bulk operation response body this error carries, if it carries one.
    /// </summary>
    /// <param name="error">The error to inspect.</param>
    /// <returns>The parsed response body, or <c>null</c> when it was not a bulk response.</returns>
    internal static Dictionary<string, object?>? AsBulkOperationResult(this PayloadError error)
    {
        if (error.StatusCode != 400 || error.Body == null)
        {
            return null;
        }

        Dictionary<string, object?>? json;

        try
        {
            json = JsonParser.Parse(error.Body);
        }
        catch (JsonException)
        {
            return null;
        }

        // A bulk response reports the documents it wrote alongside the ones it could not. Every
        // other rejection Payload sends carries "errors" without "docs".
        if (json == null || !json.ContainsKey("docs") || !json.ContainsKey("errors"))
        {
            return null;
        }

        return json;
    }
}

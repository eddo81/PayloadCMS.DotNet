using Payload.CMS.Internal.Contracts;

namespace Payload.CMS.Public.Config;

/// <summary>
/// <see cref="IAuthCredential"/> for Payload CMS <c>API Key</c> authentication.
/// <para>Sets the <c>Authorization</c> header to:
/// <c>{collectionSlug} API-Key {apiKey}</c></para>
/// </summary>
/// <seealso href="https://payloadcms.com/docs/authentication/api-keys"/>
public class ApiKeyAuth : IAuthCredential
{
    private readonly string _collectionSlug;
    private readonly string _apiKey;

    /// <param name="collectionSlug">The slug of the auth-enabled collection (e.g. <c>users</c>).</param>
    /// <param name="apiKey">The API key to authenticate with.</param>
    public ApiKeyAuth(string collectionSlug, string apiKey)
    {
        _collectionSlug = collectionSlug;
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public void Apply(Dictionary<string, string> headers)
    {
        headers["Authorization"] = $"{this._collectionSlug} API-Key {this._apiKey}";
    }
}

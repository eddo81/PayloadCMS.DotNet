using Payload.CMS.Internal.Contracts;

namespace Payload.CMS.Public.Config;

/// <summary>
/// <see cref="IAuthCredential"/> for Payload CMS <c>JWT</c> authentication.
/// <para>Sets the <c>Authorization</c> header to: <c>Bearer {token}</c></para>
/// </summary>
/// <seealso href="https://payloadcms.com/docs/authentication/token-data"/>
public class JwtAuth : IAuthCredential
{
    private readonly string _token;

    /// <param name="token">The JWT bearer token.</param>
    public JwtAuth(string token)
    {
        _token = token;
    }

    /// <inheritdoc/>
    public void Apply(Dictionary<string, string> headers)
    {
        headers["Authorization"] = $"Bearer {this._token}";
    }
}

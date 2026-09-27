namespace PayloadCMS.DotNet.Config;

/// <summary>
/// SDK-wide settings for <see cref="PayloadCMS.DotNet.PayloadSDK"/>.
/// </summary>
public sealed record PayloadSDKConfig
{
    /// <summary>
    /// Percent-encode <c>[</c>, <c>]</c> and <c>,</c> in query strings. Defaults to <c>true</c>; set to <c>false</c> to leave them literal.
    /// </summary>
    public bool StrictEncoding { get; init; } = true;
}

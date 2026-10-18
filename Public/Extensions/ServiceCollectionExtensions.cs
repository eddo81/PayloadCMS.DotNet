using Microsoft.Extensions.DependencyInjection;
using PayloadCMS.DotNet.Config;

namespace PayloadCMS.DotNet.Extensions;

/// <summary>
/// Extension methods for registering <see cref="PayloadSDK"/> with an ASP.NET Core DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PayloadSDK"/> as a scoped service in the DI container.
    /// <para>Each scope gets its own instance, so a credential set with <see cref="PayloadSDK.SetJwtAuth"/>
    /// or <see cref="PayloadSDK.SetApiKeyAuth"/> applies only to that scope.</para>
    /// </summary>
    /// <param name="services">The service collection to add the SDK to.</param>
    /// <param name="baseUrl">The base URL of the Payload CMS instance (e.g. <c>https://cms.example.com</c>).</param>
    /// <param name="config">Settings applied to every request the registered SDK makes. Defaults are used when omitted.</param>
    /// <param name="httpClient">
    /// Optional <see cref="HttpClient"/> instance shared by every registered instance.
    /// The caller is responsible for its lifetime and disposal. If omitted, a shared default client is used instead.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPayloadSDK(this IServiceCollection services, string baseUrl, PayloadSDKConfig? config = null, HttpClient? httpClient = null)
    {
        services.AddScoped(provider => new PayloadSDK(baseUrl, config, httpClient));

        return services;
    }
}

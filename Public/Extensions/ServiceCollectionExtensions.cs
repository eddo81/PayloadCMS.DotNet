using Microsoft.Extensions.DependencyInjection;

namespace PayloadCMS.DotNet.Extensions;

/// <summary>
/// Extension methods for registering <see cref="PayloadSDK"/> with an ASP.NET Core DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PayloadSDK"/> as a singleton in the DI container,
    /// backed by a named <see cref="System.Net.Http.HttpClient"/> managed by <see cref="System.Net.Http.IHttpClientFactory"/>.
    /// </summary>
    /// <param name="services">The service collection to add the SDK to.</param>
    /// <param name="baseUrl">The base URL of the Payload CMS instance (e.g. <c>https://cms.example.com</c>).</param>
    /// <param name="configureClient">Optional delegate to configure the underlying <see cref="System.Net.Http.HttpClient"/> (e.g. timeouts, default headers).</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPayloadSDK(this IServiceCollection services, string baseUrl, Action<System.Net.Http.HttpClient>? configureClient = null) {
        services.AddHttpClient(nameof(PayloadSDK), httpClient =>
        {
            configureClient?.Invoke(httpClient);
        });

        services.AddSingleton(provider =>
        {
            var factory = provider.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            var httpClient = factory.CreateClient(nameof(PayloadSDK));

            return new PayloadSDK(httpClient, baseUrl);
        });

        return services;
    }
}

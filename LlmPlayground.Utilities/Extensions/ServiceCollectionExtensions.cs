using LlmPlayground.Utilities.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LlmPlayground.Utilities.Extensions;

/// <summary>
/// Extension methods for registering utility services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the LlmPlayground utilities to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmPlaygroundUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IRequestValidator, RequestValidator>();

        return services;
    }
}


using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LlmPlayground.Services.Extensions;

/// <summary>
/// Extension methods for configuring LlmPlayground services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all LlmPlayground services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmPlaygroundServices(this IServiceCollection services)
    {
        services.AddLlmService();
        services.AddPrologService();
        services.AddPromptLabService();
        return services;
    }

    /// <summary>
    /// Adds the LLM service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmService(this IServiceCollection services)
    {
        services.AddSingleton<ILlmService, LlmService>();
        return services;
    }

    /// <summary>
    /// Adds the Prolog service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPrologService(this IServiceCollection services)
    {
        services.AddSingleton<IPrologService, PrologService>();
        return services;
    }

    /// <summary>
    /// Adds the PromptLab service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPromptLabService(this IServiceCollection services)
    {
        services.AddSingleton<IPromptLabService, PromptLabService>();
        return services;
    }
}


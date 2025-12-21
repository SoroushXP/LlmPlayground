using LlmPlayground.Utilities.Logging;
using LlmPlayground.Utilities.Logging.Sinks;
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

    /// <summary>
    /// Adds the centralized logging service with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmPlaygroundLogging(this IServiceCollection services)
    {
        return services.AddLlmPlaygroundLogging(LoggingConfiguration.Default);
    }

    /// <summary>
    /// Adds the centralized logging service with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The logging configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmPlaygroundLogging(
        this IServiceCollection services,
        LoggingConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(configuration);
        services.AddSingleton<ILoggerService>(sp =>
        {
            var config = sp.GetRequiredService<LoggingConfiguration>();
            return new LoggerBuilder()
                .WithConfiguration(config)
                .Build();
        });

        return services;
    }

    /// <summary>
    /// Adds the centralized logging service with builder configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the logger builder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmPlaygroundLogging(
        this IServiceCollection services,
        Action<LoggerBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton<ILoggerService>(sp =>
        {
            var builder = new LoggerBuilder();
            configure(builder);
            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Adds all LlmPlayground utilities including logging with console interception.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logDirectory">Optional log directory for file logging.</param>
    /// <param name="interceptConsole">Whether to intercept console output.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmPlaygroundFullUtilities(
        this IServiceCollection services,
        string? logDirectory = null,
        bool interceptConsole = true)
    {
        services.AddLlmPlaygroundUtilities();
        
        services.AddLlmPlaygroundLogging(builder =>
        {
            builder.WithMinimumLevel(LogLevel.Debug)
                   .WithConsoleSink(LogLevel.Information, useColors: true);

            if (!string.IsNullOrEmpty(logDirectory))
            {
                builder.WithFileSink(
                    directory: logDirectory,
                    minimumLevel: LogLevel.Debug,
                    rollingPolicy: RollingPolicy.Daily,
                    retainDays: 7);
            }

            if (interceptConsole)
            {
                builder.WithConsoleInterception();
            }
        });

        return services;
    }
}

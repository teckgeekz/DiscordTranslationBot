﻿using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Providers.Translation;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extension methods for a service collection.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds translation providers.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>Service collection.</returns>
    internal static IServiceCollection AddTranslationProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Set up configuration.
        var section = configuration.GetSection(TranslationProvidersOptions.SectionName);

        services.AddOptionsWithFluentValidation<TranslationProvidersOptions, TranslationProvidersOptionsValidator>(
            section);

        // Register translation providers. They are prioritized in the order added.
        var options = section.Get<TranslationProvidersOptions>();

        if (options?.AzureTranslator.Enabled == true)
        {
            services.AddSingleton<TranslationProviderBase, AzureTranslatorProvider>();
        }

        if (options?.LibreTranslate.Enabled == true)
        {
            services.AddSingleton<TranslationProviderBase, LibreTranslateProvider>();
        }

        // Configure named HttpClient for translation providers.
        services.AddHttpClient(TranslationProviderBase.ClientName)
            .AddTransientHttpErrorPolicy(
                b => b.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 2)));

        return services;
    }
}

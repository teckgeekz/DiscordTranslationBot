using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Telemetry;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using DiscordEventListener = DiscordTranslationBot.Discord.DiscordEventListener;

var builder = WebApplication.CreateSlimBuilder(args);

// Logging.
builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss.fff ");

// Telemetry.
builder.AddTelemetry();

// Configuration.
builder
    .Services
    .AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetRequiredSection(DiscordOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Main services.
builder
    .Services
    .AddTranslationProviders(builder.Configuration)
    .AddSingleton<IDiscordClient>(
        _ => new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                                 | GatewayIntents.GuildMessages
                                 | GatewayIntents.GuildMessageReactions
                                 | GatewayIntents.MessageContent,
                MessageCacheSize = 100,
                UseInteractionSnowflakeDate = false
            }))
    .AddSingleton<DiscordEventListener>()
    .AddHostedService<Worker>();

// Mediator.
builder
    .Services
    .AddMediator(o => o.NotificationPublisherType = typeof(TaskWhenAllPublisher))
    .AddSingleton(typeof(IPipelineBehavior<,>), typeof(MessageValidationBehavior<,>))
    .AddSingleton(typeof(IPipelineBehavior<,>), typeof(MessageElapsedTimeLoggingBehavior<,>));

// Health checks.
builder
    .Services
    .AddRateLimiting()
    .AddHealthChecks()
    .AddCheck<DiscordClientHealthCheck>(DiscordClientHealthCheck.HealthCheckName);

var app = builder.Build();

app.UseTelemetry();
app.UseRateLimiter();

app
    .MapHealthChecks(
        "/_health",
        new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse })
    .RequireRateLimiting(RateLimitingExtensions.HealthCheckRateLimiterPolicyName);

await app.RunAsync();

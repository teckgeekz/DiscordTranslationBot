﻿using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Countries.Utilities;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Commands.Translation;

public sealed class TranslateByCountryFlagEmojiReaction : ICommand
{
    [Required]
    public required ICountry Country { get; init; }

    /// <summary>
    /// The user message.
    /// </summary>
    [Required]
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    [Required]
    public required ReactionInfo ReactionInfo { get; init; }
}

/// <summary>
/// Handler for translating by a flag emoji reaction.
/// </summary>
public sealed partial class TranslateByCountryFlagEmojiReactionHandler
    : ICommandHandler<TranslateByCountryFlagEmojiReaction>,
        INotificationHandler<ReactionAddedEvent>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateByCountryFlagEmojiReactionHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateByCountryFlagEmojiReactionHandler(
        IDiscordClient client,
        IEnumerable<TranslationProviderBase> translationProviders,
        IMediator mediator,
        ILogger<TranslateByCountryFlagEmojiReactionHandler> logger)
    {
        _client = client;
        _translationProviders = translationProviders.ToList();
        _mediator = mediator;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates any message that got a country flag emoji reaction on it.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask<Unit> Handle(
        TranslateByCountryFlagEmojiReaction command,
        CancellationToken cancellationToken)
    {
        if (command.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await command.Message.RemoveReactionAsync(
                command.ReactionInfo.Emote,
                command.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(command.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await command.Message.RemoveReactionAsync(
                command.ReactionInfo.Emote,
                command.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        string? providerName = null;
        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviders)
        {
            try
            {
                providerName = translationProvider.ProviderName;

                translationResult = await translationProvider.TranslateByCountryAsync(
                    command.Country,
                    sanitizedMessage,
                    cancellationToken);

                break;
            }
            catch (LanguageNotSupportedForCountryException ex)
            {
                // Send message if this is the last available translation provider.
                if (translationProvider == _translationProviders[^1])
                {
                    _log.LanguageNotSupportedForCountry(ex, translationProvider.GetType(), command.Country.Name);

                    await _mediator.Send(
                        new SendTempReply
                        {
                            Text = ex.Message,
                            ReactionInfo = command.ReactionInfo,
                            SourceMessage = command.Message
                        },
                        cancellationToken);

                    return Unit.Value;
                }
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        if (translationResult is null)
        {
            await command.Message.RemoveReactionAsync(
                command.ReactionInfo.Emote,
                command.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            await _mediator.Send(
                new SendTempReply
                {
                    Text = "Couldn't detect the source language to translate from or the result is the same.",
                    ReactionInfo = command.ReactionInfo,
                    SourceMessage = command.Message
                },
                cancellationToken);

            return Unit.Value;
        }

        // Send the reply message.
        var replyText = !string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode)
            ? $"""
               Translated message from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)} to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
               {Format.BlockQuote(translationResult.TranslatedText)}
               """
            : $"""
               Translated message to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
               {Format.BlockQuote(translationResult.TranslatedText)}
               """;

        return await _mediator.Send(
            new SendTempReply
            {
                Text = replyText,
                ReactionInfo = command.ReactionInfo,
                SourceMessage = command.Message,
                DeletionDelay = TimeSpan.FromSeconds(20)
            },
            cancellationToken);
    }

    public async ValueTask Handle(ReactionAddedEvent notification, CancellationToken cancellationToken)
    {
        if (!CountryUtility.TryGetCountryByEmoji(notification.ReactionInfo.Emote.Name, out var country))
        {
            return;
        }

        await _mediator.Send(
            new TranslateByCountryFlagEmojiReaction
            {
                Country = country,
                Message = notification.Message,
                ReactionInfo = notification.ReactionInfo
            },
            cancellationToken);
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceMessage();

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Target language code not supported. Provider {providerType} doesn't support the language code or the country {countryName} has no mapping for the language code.")]
        public partial void LanguageNotSupportedForCountry(Exception ex, Type providerType, string countryName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}

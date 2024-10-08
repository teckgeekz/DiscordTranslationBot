﻿using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Countries;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using Mediator;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateByCountryFlagEmojiReactionHandlerTests
{
    private const string Content = """
                                   👍 test<:disdainsam:630009232128868353> _test_*test*
                                   > test
                                   __test__
                                   """;

    private const string ExpectedSanitizedMessage = """
                                                    test testtest
                                                    test
                                                    test
                                                    """;

    private const ulong BotUserId = 1UL;
    private const ulong MessageUserId = 2UL;

    private readonly IMediator _mediator;
    private readonly IUserMessage _message;
    private readonly TranslateByCountryFlagEmojiReactionHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public TranslateByCountryFlagEmojiReactionHandlerTests()
    {
        _translationProvider = Substitute.For<TranslationProviderBase>();
        _translationProvider.ProviderName.Returns("Test Provider");

        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Id.Returns(BotUserId);

        _message = Substitute.For<IUserMessage>();
        _message.Id.Returns(1UL);
        _message.Author.Id.Returns(MessageUserId);
        _message.Content.Returns(Content);

        _message
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        var channel = Substitute.For<IMessageChannel, IGuildChannel>();
        channel.EnterTypingState().ReturnsForAnyArgs(Substitute.For<IDisposable>());
        _message.Channel.Returns(channel);
        ((IGuildChannel)channel).Guild.Id.Returns(1UL);

        _mediator = Substitute.For<IMediator>();

        _sut = new TranslateByCountryFlagEmojiReactionHandler(
            client,
            [_translationProvider],
            _mediator,
            new LoggerFake<TranslateByCountryFlagEmojiReactionHandler>());
    }

    [Fact]
    public async Task Handle_ReactionAddedEvent_Returns_GetCountryByEmojiFalse()
    {
        // Arrange
        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Channel = Substitute.For<IMessageChannel>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.Airplane.Name)
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<TranslateByCountryFlagEmojiReaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReactionAddedEvent_SendsCommand_GetCountryByEmojiTrue()
    {
        // Arrange
        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Channel = Substitute.For<IMessageChannel>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(CountryConstants.SupportedCountries[0].EmojiUnicode)
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<TranslateByCountryFlagEmojiReaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = CountryConstants.SupportedCountries[0],
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _translationProvider
            .DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Success()
    {
        // Arrange
        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = "translated_text"
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = CountryConstants.SupportedCountries[0],
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<SendTempReply>(x => x.Text.Contains("Translated message from", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = CountryConstants.SupportedCountries[0],
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _translationProvider
            .DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_NoTranslationResult()
    {
        // Arrange
        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TranslationResult)null!);

        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = CountryConstants.SupportedCountries[0],
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task
        Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_WhenUnsupportedCountryExceptionIsThrown_ForLastTranslationProvider()
    {
        // Arrange
        const string exMessage = "exception message";

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .ThrowsAsync(new LanguageNotSupportedForCountryException(exMessage));

        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = CountryConstants.SupportedCountries[0],
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator.Received(1).Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = ExpectedSanitizedMessage
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = CountryConstants.SupportedCountries[0],
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<SendTempReply>(
                    x => x.Text == "Couldn't detect the source language to translate from or the result is the same."),
                Arg.Any<CancellationToken>());
    }
}

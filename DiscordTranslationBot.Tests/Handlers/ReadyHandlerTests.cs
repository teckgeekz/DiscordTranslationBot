﻿using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;
using Mediator;
using Moq;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class ReadyHandlerTests
{
    private readonly Mock<IMediator> _mediator;
    private readonly ReadyHandler _sut;

    public ReadyHandlerTests()
    {
        _mediator = new Mock<IMediator>(MockBehavior.Strict);
        _sut = new ReadyHandler(_mediator.Object);
    }

    [Fact]
    public async Task Handle_ReadyNotification_Success()
    {
        // Arrange
        var notification = new ReadyNotification();

        _mediator
            .Setup(x => x.Send(It.IsAny<RegisterSlashCommands>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediator.Verify(
            x => x.Send(It.IsAny<RegisterSlashCommands>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}

using Discord;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Tests.Handlers;

[TestClass]
public sealed class RedirectLogMessageToLoggerHandlerTests
{
    private readonly LoggerFake<RedirectLogMessageToLoggerHandler> _logger;
    private readonly RedirectLogMessageToLoggerHandler _sut;

    public RedirectLogMessageToLoggerHandlerTests()
    {
        _logger = new LoggerFake<RedirectLogMessageToLoggerHandler>(true);
        _sut = new RedirectLogMessageToLoggerHandler(_logger);
    }

    [DataTestMethod]
    [DataRow(LogSeverity.Debug, LogLevel.Trace)]
    [DataRow(LogSeverity.Verbose, LogLevel.Debug)]
    [DataRow(LogSeverity.Info, LogLevel.Information)]
    [DataRow(LogSeverity.Warning, LogLevel.Warning)]
    [DataRow(LogSeverity.Error, LogLevel.Error)]
    [DataRow(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_LogNotification_Success(LogSeverity severity, LogLevel expectedLevel)
    {
        // Arrange
        var request = new LogNotification
        {
            LogMessage = new LogMessage(severity, "source1", "message1", new InvalidOperationException("test"))
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        var entry = _logger.Entries.First();
        entry.LogLevel.Should().Be(expectedLevel);
        entry.Message.Should().Be($"Discord {request.LogMessage.Source}: {request.LogMessage.Message}");
        entry.Exception.Should().Be(request.LogMessage.Exception);
    }
}

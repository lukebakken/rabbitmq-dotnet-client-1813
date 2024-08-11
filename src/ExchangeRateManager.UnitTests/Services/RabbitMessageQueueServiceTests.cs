using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Services;
using ExchangeRateManager.Tests.UnitTests.Base;
using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using System.Text;

namespace ExchangeRateManager.Tests.UnitTests.Services;

/// <summary>
/// Tests for the RabbitMQ message queue service.
/// </summary>
public class RabbitMessageQueueServiceTests : TestBase
{
    private readonly RabbitMessageQueueService _service;
    private readonly Mock<IAsyncConnectionFactory> _factoryMock = new();
    private readonly Mock<IConnection> _connectionMock = new();
    private readonly Mock<IModel> _channelMock = new();

    public RabbitMessageQueueServiceTests() : base()
    {
        _connectionMock
            .Setup(x => x.CreateModel())
            .Returns(_channelMock.Object);

        _factoryMock
            .Setup(x => x.CreateConnection())
            .Returns(_connectionMock.Object);

        _service = new RabbitMessageQueueService(_factoryMock.Object);
    }

    [Fact]
    public void SendMessage_RunsBasicPublish()
    {
        var expectedPayload = KeyValuePair.Create("crash", "test");
        byte[] actualMessage = [];
        KeyValuePair<string, string> actualText;

        _channelMock
            .Setup(x => x.BasicPublish(string.Empty, MessageQueues.NewForexRate, false, null, It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback((string _, string _, bool _, IBasicProperties _, ReadOnlyMemory<byte> message) => actualMessage = message.ToArray());

        _service.SendMessage(MessageQueues.NewForexRate, expectedPayload);

        // Assert
        _channelMock
            .Verify(x => x.BasicPublish(string.Empty, MessageQueues.NewForexRate, false, null, It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);

        actualMessage.Should().NotBeEmpty();
        actualText = Encoding.UTF8.GetString(actualMessage).FromJson<KeyValuePair<string, string>>();
        actualText.Should().BeEquivalentTo(expectedPayload);
    }
}

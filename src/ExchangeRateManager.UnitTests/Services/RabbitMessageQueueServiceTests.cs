using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Services;
using ExchangeRateManager.Tests.UnitTests.Base;
using NSubstitute;
using RabbitMQ.Client;
using Shouldly;
using System.Text;
using System.Threading.Channels;

namespace ExchangeRateManager.Tests.UnitTests.Services;

/// <summary>
/// Tests for the RabbitMQ message queue service.
/// </summary>
public class RabbitMessageQueueServiceTests : TestBase
{
    private readonly RabbitMessageQueueService _service;
    private readonly IConnectionFactory _factory = Substitute.For<IConnectionFactory>();
    private readonly IConnection _connection = Substitute.For<IConnection>();
    private readonly IChannel _channel = Substitute.For<IChannel>();


    public RabbitMessageQueueServiceTests() : base()
    {
        _connection
            .CreateChannelAsync()
            .Returns(_channel);

        _factory
            .CreateConnectionAsync()
            .Returns(_connection);

        _service = new RabbitMessageQueueService(_factory);
    }

    interface IProperties : IReadOnlyBasicProperties, IAmqpHeader { }

    [Fact]
    public async Task SendMessage_RunsBasicPublish()
    {
        var expectedPayload = KeyValuePair.Create("crash", "test");
        byte[] actualMessage = [];
        KeyValuePair<string, string> actualText;

        byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes("Hello, world!");

        _channel
            .When(x => x.BasicPublishAsync(
                string.Empty, MessageQueues.NewForexRate, false,
                Arg.Any<BasicProperties>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => actualMessage = ((ReadOnlyMemory<byte>)callInfo[4]).ToArray());

        await _service.SendMessage(MessageQueues.NewForexRate, expectedPayload);

        // Assert
        await _channel
            .Received(1)
            .BasicPublishAsync(
                string.Empty, MessageQueues.NewForexRate, false,
                Arg.Any<BasicProperties>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());

        actualMessage.ShouldNotBeEmpty();
        actualText = Encoding.UTF8.GetString(actualMessage).FromJson<KeyValuePair<string, string>>();
        actualText.ShouldBeEquivalentTo(expectedPayload);
    }
}

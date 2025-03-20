using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using ExchangeRateManager.Common.Constants;
using System.Diagnostics;

namespace ExchangeRateManagerApp.MessageQueueListener
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@rabbitmq:5672/")
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                MessageQueues.NewForexRate, false, false, false);

            Console.CancelKeyPress += delegate
            {
                Console.WriteLine(" [-] Bye! <3");
                if (Debugger.IsAttached)
                {
                    Environment.Exit(1);
                }
            };

            Console.WriteLine(" [*] Waiting for messages. Hit 'Ctrl+C' to terminate.");
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] Received {message}");

                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(MessageQueues.NewForexRate, true, consumer);
            while (true)
            {
                await Task.Delay(1000);
            }
        }
    }
}

using ExchangeRateManager.Common.Constants;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;

namespace ExchangeRateManagerApp.MessageQueueListener
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@rabbitmq:5672/")
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: MessageQueues.NewForexRate,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            Console.CancelKeyPress += delegate
            {
                Console.WriteLine(" [-] Bye! <3");
                if (Debugger.IsAttached)
                {
                    Environment.Exit(1);
                }
            };

            Console.WriteLine(" [*] Waiting for messages. Hit 'Ctrl+C' or any key to terminate.");
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] Received {message}");
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queue: MessageQueues.NewForexRate, autoAck: true, consumer: consumer);

            Console.ReadLine();
        }
    }
}

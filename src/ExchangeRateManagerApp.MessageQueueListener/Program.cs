using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using ExchangeRateManager.Common.Constants;
using System.Diagnostics;

namespace ExchangeRateManagerApp.MessageQueueListener
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@rabbitmq:5672/")
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: MessageQueues.NewForexRate,
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

            Console.WriteLine(" [*] Waiting for messages. Hit 'Ctrl+C' to terminate.");
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] Received {message}");
                };

            channel.BasicConsume(queue: MessageQueues.NewForexRate, autoAck: true, consumer: consumer);
            while (true)
            {
                Task.Delay(1000);
            }
        }
    }
}

using System.Text;
using RabbitMQ.Client;

namespace sender
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "user",
                Password = "password"
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "direct_logs", type: ExchangeType.Direct);

            string routing;
            string message;

            do
            {
                Console.WriteLine("Please set the routing:");
                routing = Console.ReadLine() ?? "Default";

                Console.WriteLine("Enter your message (Enter 'End' to close the program):");
                message = Console.ReadLine() ?? "Hello";

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "direct_logs",
                                     routingKey: routing,
                                     basicProperties: null,
                                     body: body);
                Console.WriteLine($"Sent ''{message}' with ;{routing}' routing");
                Console.WriteLine("----------------------------------------------------------");
            } while (message != "End");
        }
    }
}
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

            var severities = new List<string> { "erro", "warning", "info", "debug" };
            Random rnd = new Random();

            var severity = severities.ElementAt(rnd.Next(0, 3));
            var message = "Hello";
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "direct_logs",
                                 routingKey: severity, // it means that thisexchange will push messages only to the queues that want message from this exchange with this routing
                                 basicProperties: null,
                                 body: body);
            Console.WriteLine($" [x] Sent '{severity}':'{message}'");

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
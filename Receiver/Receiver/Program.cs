using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Receiver
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
            
            //var queueName = channel.QueueDeclare().QueueName;
            var queueName = "testName";
            channel.QueueDeclare(queue: queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            Console.WriteLine("Please enter interested routings(Enter 'End' to finish)");
            var routings = new HashSet<string>();
            while (true)
            {
                var routing = Console.ReadLine();
                if (routing == "End")
                {
                    break;
                }
                routings.Add(routing);
            }

            foreach (var routing in routings)
            {
                channel.QueueBind(queue: queueName,
                                  exchange: "direct_logs",
                                  routingKey: routing); 
            }

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            Console.WriteLine("Waiting for messages...");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                Console.WriteLine($"Received '{routingKey}':'{message}'");
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
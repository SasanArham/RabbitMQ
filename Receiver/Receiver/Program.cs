using System.Drawing;
using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static System.Net.Mime.MediaTypeNames;

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

            var queueName = string.Empty;
            var qArguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "nacked_not_queud_exchange" } ,
                { "x-max-priority", 5 } // while its possible t use both negative and positive numbers up to 255 its highly recommended to use numbers between 0 to 5 
            };
            channel.QueueDeclare(queue: queueName,
                                 durable: false,
                                 exclusive: true,
                                 autoDelete: true,
                                 arguments: qArguments);

            //Note that not all property combination make sense in practice. For example, auto-delete and exclusive queues should be server-named.
            //Such queues are supposed to be used for client-specific or connection (session)-specific data.
            //When auto-delete or exclusive queues use well - known(static) names,
            //in case of client disconnection and immediate reconnection there will be a natural race condition between RabbitMQ nodes that will delete
            //such queues and recovering clients that will try to re-declare them.This can result in client - side connection recovery failure or exceptions,
            //and create unnecessary confusion or affect application availability.
            //https://www.rabbitmq.com/queues.html




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
                if (message == "Nack")
                {
                    channel.BasicNack(ea.DeliveryTag, false
                        , false); // If I set this to true and there is no other handeling consumer it will cause infinte reququing loop
                }
                else
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
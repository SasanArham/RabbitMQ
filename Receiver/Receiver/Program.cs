using System.Drawing;
using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

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

            var queueName = "stream_queue2";


            //Streams are implemented as an immutable append-only disk log.
            //This means that the log will grow indefinitely until the disk runs out.
            //To avoid this undesirable scenario it is possible to set a retention configuration per stream which will discard the oldest data
            //in the log based on total log data size and/or age.
            //There are two parameters that control the retention of a stream.
            //These can be combined. These are either set at declaration time using a queue argument or as a policy which can be dynamically updated.
            var qArguments = new Dictionary<string, object>
            {
                { "x-queue-type", "stream"} ,
                { "x-max-age", "7D"} , //valid units: Y, M, D, h, m, s
                { "x-max-length-bytes", 100000000} 
            };
            channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: qArguments);

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
            channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);
            Console.WriteLine("Waiting for messages...");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                

                if (message == "Nack")
                {
                    channel.BasicNack(ea.DeliveryTag, false
                        , false); // If I set this to true and there is no other handeling consumer it will cause infinte reququing loop
                    Console.WriteLine($"Received But not acknoledged '{routingKey}':'{message}'");
                }
                else
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    Console.WriteLine($"Received '{routingKey}':'{message}'");
                }
            };

            var consumerArgs = new Dictionary<string, object>
            {
                //{ "x-stream-offset", "first" } // It means whenever the app starts consuming it will start reading from first message of stream
                //{ "x-stream-offset", 3 } // It means whenever the app starts consuming it will start reading from fourth (index starts from 0) message of stream
                { "x-stream-offset", "last" } // It means whenever the app starts consuming it will start reading from last message of stream
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer
                                 ,arguments: consumerArgs);

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Mdameer.RabbitMQ.Configuration;
using Orchard;
using Orchard.Logging;
using Orchard.MessageBus.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mdameer.RabbitMQ.Services
{
    public class RabbitMQMessageBusBroker : Component, IMessageBroker
    {

        private readonly IRabbitMQConnectionProvider _rabbitMQConnectionProvider;
        private readonly IHostNameProvider _hostNameProvider;
        private readonly IConnection _connection;
        public const string ConnectionStringKey = "Mdameer.RabbitMQ";
        public const string ExchangeName = "OrchardExchange";
        private readonly string _connectionString;

        private ConcurrentDictionary<string, ConcurrentBag<Action<string, string>>> _handlers = new ConcurrentDictionary<string, ConcurrentBag<Action<string, string>>>();

        public RabbitMQMessageBusBroker(IRabbitMQConnectionProvider rabbitMQConnectionProvider, IHostNameProvider hostNameProvider)
        {
            _rabbitMQConnectionProvider = rabbitMQConnectionProvider;
            _hostNameProvider = hostNameProvider;
            _connectionString = _rabbitMQConnectionProvider.GetConnectionString(ConnectionStringKey);
            _connection = _rabbitMQConnectionProvider.GetConnection(_connectionString);
        }

        public IModel Model
        {
            get
            {
                return _connection.CreateModel();
            }
        }

        public void Subscribe(string channel, Action<string, string> handler)
        {
            if (_connection == null)
            {
                return;
            }

            try
            {
                var channelHandlers = _handlers.GetOrAdd(channel, c =>
                {
                    return new ConcurrentBag<Action<string, string>>();
                });

                channelHandlers.Add(handler);

                Model.ExchangeDeclare(ExchangeName, ExchangeType.Direct, false, true);
                var queueName = Model.QueueDeclare().QueueName;
                Model.QueueBind(queueName, ExchangeName, channel);

                var consumer = new EventingBasicConsumer(Model);
                consumer.Received += (model, ea) =>
                {
                    var body = Encoding.UTF8.GetString(ea.Body);
                    // the message contains the publisher before the first '/'
                    var messageTokens = body.ToString().Split('/');
                    var publisher = messageTokens.FirstOrDefault();
                    var message = messageTokens.Skip(1).FirstOrDefault();

                    if (String.IsNullOrWhiteSpace(publisher))
                    {
                        return;
                    }

                    // ignore self sent messages
                    if (_hostNameProvider.GetHostName().Equals(publisher, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    Logger.Debug("Processing {0}", message);
                    handler(ea.RoutingKey, message);
                };

                Model.BasicConsume(queueName, true, consumer);
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error occurred while subscribing to " + channel);
            }
        }

        public void Publish(string channel, string message)
        {
            if (_connection == null)
            {
                return;
            }

            Logger.Debug("Publishing {0}", message);
            Model.BasicPublish(ExchangeName, channel, null, Encoding.UTF8.GetBytes(_hostNameProvider.GetHostName() + "/" + message));
        }
    }
}
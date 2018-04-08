using System;
using System.Collections.Concurrent;
using System.Configuration;
using Orchard.Environment.Configuration;
using Orchard.Localization;
using Orchard.Logging;
using RabbitMQ.Client;

namespace Mdameer.RabbitMQ.Configuration
{

    public class RabbitMQConnectionProvider : IRabbitMQConnectionProvider
    {
        private static ConcurrentDictionary<string, Lazy<IConnection>> _connectionMultiplexers = new ConcurrentDictionary<string, Lazy<IConnection>>();
        private readonly ShellSettings _shellSettings;

        public RabbitMQConnectionProvider(ShellSettings shellSettings)
        {
            _shellSettings = shellSettings;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }

        public ILogger Logger { get; set; }

        public string GetConnectionString(string key)
        {
            var _tenantSettingsKey = _shellSettings.Name + ":" + key;

            var connectionStringSettings = ConfigurationManager.ConnectionStrings[_tenantSettingsKey] ?? ConfigurationManager.ConnectionStrings[key];

            if (connectionStringSettings == null)
            {
                return null;
            }

            return connectionStringSettings.ConnectionString;
        }

        public IConnection GetConnection(string connectionString)
        {

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            // when using ConcurrentDictionary, multiple threads can create the value
            // at the same time, so we need to pass a Lazy so that it's only 
            // the object which is added that will create a ConnectionMultiplexer,
            // even when a delegate is passed

            return _connectionMultiplexers.GetOrAdd(connectionString,
                new Lazy<IConnection>(() =>
                {
                    Logger.Debug("Creating a new cache client for: {0}", connectionString);
                    var factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
                    return factory.CreateConnection();
                })).Value;
        }
    }
}

using Orchard;
using RabbitMQ.Client;

namespace Mdameer.RabbitMQ.Configuration
{

    public interface IRabbitMQConnectionProvider : ISingletonDependency
    {
        IConnection GetConnection(string connectionString);
        string GetConnectionString(string service);
    }

}
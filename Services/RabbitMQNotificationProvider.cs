using System.Collections.Generic;
using Mdameer.RabbitMQ.Configuration;
using Orchard.Localization;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;

namespace Mdameer.RabbitMQ.Services
{
    public class RabbitMQNotificationProvider : INotificationProvider
    {
        private readonly IRabbitMQConnectionProvider _rabbitMQConnectionProvider;

        public RabbitMQNotificationProvider(IRabbitMQConnectionProvider rabbitMQConnectionProvider)
        {
            _rabbitMQConnectionProvider = rabbitMQConnectionProvider;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications()
        {
            //verify if there is a connection string set in the web.config
            if (_rabbitMQConnectionProvider.GetConnectionString(RabbitMQMessageBusBroker.ConnectionStringKey) == null)
            {
                yield return new NotifyEntry { Message = T("You need to configure RabbitMQ MessageBus connection string."), Type = NotifyType.Warning };
            }
        }
    }
}

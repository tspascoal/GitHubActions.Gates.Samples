using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace GitHubActions.Gates.Framework.Clients
{
    internal class ServiceBusClient : IServiceBusClient
    {
        readonly IConfiguration _config;
        readonly Azure.Messaging.ServiceBus.ServiceBusClient? _client;

        public ServiceBusClient(IConfiguration configuration)
        {
            _config = configuration;
        }

        internal ServiceBusClient(IConfiguration configuration, Azure.Messaging.ServiceBus.ServiceBusClient sbClient) : this(configuration) 
        {
            _client = sbClient;
        }

        /// <summary>
        /// Sends a msg to a service bus queue.
        /// 
        /// Optionally the msg can be queued for receivel
        /// </summary>
        /// <param name="QueueName"></param>
        /// <param name="Message"></param>
        /// <param name="schedule"></param>
        /// <returns></returns>
        public async Task SendMessage(string QueueName, object Message, DateTime? schedule = null)
        {
            // Use injected client?
            Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient = _client ?? CreateServiceBusClientFactory();


            // create a Queue client            
            var sender = serviceBusClient.CreateSender(QueueName);

            var msg = new ServiceBusMessage
            {
                ContentType = "application/json",
                Body = new BinaryData(Message)
            };

            if (schedule != null)
            {
                msg.ScheduledEnqueueTime = schedule.Value;
            }
            await sender.SendMessageAsync(msg);
        }

        internal virtual Azure.Messaging.ServiceBus.ServiceBusClient CreateServiceBusClientFactory()
        {
            var parameterName = $"{Config.SERVICEBUSCONNECTIONNAME}:fullyQualifiedNamespace";
            var serviceBusNamespace = _config[parameterName];

            if (serviceBusNamespace != null)
            {
                var managedIdentityCredential = new DefaultAzureCredential();

                return new(serviceBusNamespace, managedIdentityCredential);
            }
            else
            {
                var serviceBusConnectionString = _config[Config.SERVICEBUSCONNECTIONNAME];
                if (string.IsNullOrEmpty(serviceBusConnectionString))
                {
                    throw new Exception($"Missing both {Config.SERVICEBUSCONNECTIONNAME} and {Config.SERVICEBUSCONNECTIONNAME}__fullyQualifiedNamespace configurations.");
                }
                return new(serviceBusConnectionString);
            }
        }
    }
}

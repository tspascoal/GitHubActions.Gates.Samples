using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace GitHubActions.Gates.Framework
{
    public static class Config
    {
        // If you change this values this will have impact on the app settings so it will break deployed functions.
        // Make sure the .bicep files are in synch with those as well
        public const string SERVICEBUSCONNECTIONNAME = "SERVICEBUS_CONNECTION";
        public const string WEBHOOKSECRETNAME = "GHAPP_WEBHOOKSECRET";
        public const string GHAPPPEMCERTIFICATENAME = "GHAPP_PEMCERTIFICATE";
        public const string GHAPPID = "GHAPP_ID";
        public static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
        }
    }
}

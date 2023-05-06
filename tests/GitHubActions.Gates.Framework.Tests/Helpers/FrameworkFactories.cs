using Microsoft.Extensions.Configuration;
using Moq;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    internal static class FrameworkFactories
    {
        internal static Mock<IConfiguration> CreateServiceBusConfigMock(string? connectionString = null, string? fullyQualifiedNamespace = null)
        {
            var config = new Mock<IConfiguration>();

            if (connectionString != null)
            {
                config.SetupGet(c => c[Config.SERVICEBUSCONNECTIONNAME]).Returns(connectionString);
            }

            if (fullyQualifiedNamespace != null)
            {
                config.SetupGet(c => c[$"{Config.SERVICEBUSCONNECTIONNAME}:fullyQualifiedNamespace"]).Returns(fullyQualifiedNamespace);
            }
            return config;
        }
    }
}

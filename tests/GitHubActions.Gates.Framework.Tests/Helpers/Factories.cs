using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit.Sdk;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    internal static class Factories
    {
        private static readonly string pemPrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIICWwIBAAKBgHns9VQsMjbvByTvWYYrhh5hH4SZJXe5QtP9kTZAwvPtUZ0qfmWg
tjT6yAzf4tm6+t8q44a/1KEG3UkCkWkN/eMLGTAdOBjJbs4J5D9SrCiRPtFt4PIy
ABO1WkL+657t6zccDcV5do6RpSgGo3BsGX7SL5W/Ln3XEtLkMBU2o/KLAgMBAAEC
gYAGaikeHJUnvrupfc4/+No9ju6Rk10dR2n0rUqOiCm5m6rHkgzEXAg6EVelFcJh
oizAwQjndgBB2JKb3L65wDqzPHN9S8/vaZWpadtuTyRMGsFxwDexr9sMmdM9Ixn3
Ch9LmN0U6pv9E0gslv1wa3gEXSoxIR1OxZ3qVPTxGXPDwQJBANAHHNux9L3fBLa4
7ofa2jLUknnmOuzmbrDT6/hqqzaToCVWg6rc4GHoM3IUY/QUOZC42/tyzoBRn9LW
wiI4Gs0CQQCWCtFgnaj7JdZtib58OGpbCIAqhF/9ePFHD5+c2Bs1YwtenBlwr649
moPJ3X8JgpwvKHLWQx4xlKfdukkZ//K3AkEAl0a026Z7XZ/SY7Xz7+NcjV477l1Y
OHIRyJEzpgCb5SJRcRWKxjO9EDW1Q55EWXhjrDRh9Ga2eGXjHYWCwzOdeQJAMA/d
C+bU10ZCjqn944qqvuhVLcljei1AlHOzvCkZZhuI69By8b7EwKT7LDGQXPqCjzSU
vH+Zb2Zf802V1wc+twJAOXddfD8kwveNpOLlR3nyEWSTYdC0kstOoI62kqeYHcPz
rC9V/EWOvIlFkDZ/FjONYvHBbfzbl0wFLPoZ5rFTaw==
-----END RSA PRIVATE KEY-----";

        internal static Mock<IConfigurationRoot?> CreateConfigMock()
        {
            var config = new Mock<IConfigurationRoot?>();
            config.SetupGet(c => c![Config.GHAPPPEMCERTIFICATENAME])
                  .Returns(pemPrivateKey);
            config.SetupGet(c => c![Config.GHAPPID])
                .Returns("123");

            return config;
        }

        internal static Mock<ILogger> CreateLoggerMock(bool callBase = false)
        {
            return new Mock<ILogger>() { CallBase = callBase };
        }

        internal static HttpResponseMessage CreateHttpResponseFactory(HttpStatusCode statusCode = HttpStatusCode.OK,
                                                      string? content = null,
                                                      IEnumerable<(string Name, string Value)>? headers = null)
        {
            var httpResponse = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                httpResponse.Content = new StringContent(content!);
            }
            if (headers != null && headers.Any())
            {
                foreach (var (name, value) in headers)
                {
                    httpResponse.Headers.Add(name, value);
                }
            }
            return httpResponse;
        }

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

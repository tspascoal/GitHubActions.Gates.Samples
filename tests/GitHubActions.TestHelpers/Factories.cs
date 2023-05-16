using GitHubActions.Gates.Framework;
using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Octokit;
using System.Net;

namespace GitHubActions.TestHelpers
{
    public class Factories
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

        public static Mock<IConfiguration?> CreateConfigMock()
        {
            var config = new Mock<IConfiguration?>();
            config.SetupGet(c => c![Config.GHAPPPEMCERTIFICATENAME])
                  .Returns(pemPrivateKey);
            config.SetupGet(c => c![Config.GHAPPID])
                .Returns("123");

            return config;
        }
        public static DeploymentProtectionRuleWebHook BuildWebHook()
        {
            // TODO: add a more representative webhook
            return new DeploymentProtectionRuleWebHook { environment = "production" };
        }
        public static Mock<ILogger> CreateLoggerMock(bool callBase = false)
        {
            return new Mock<ILogger>() { CallBase = callBase };
        }

        public static HttpResponseMessage CreateHttpResponseFactory(HttpStatusCode statusCode = HttpStatusCode.OK,
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

        public static (Mock<GitHubAppClient>, Mock<IGitHubClient>) CreateGitHubClientForGraphl(Mock<ILogger> log, string graphQLQuery, object graphQLVars, string graphQLResponse)
        {
            var config = Factories.CreateConfigMock();
            var octoClientMock = new Mock<IGitHubClient>();
            var gitHubAppClientMock = new Mock<GitHubAppClient>(octoClientMock.Object, 0L, log.Object, config.Object) { CallBase = true };

            var responseGraphQL = OctokitResponseFactory.CreateApiResponse<string>(HttpStatusCode.OK, graphQLResponse);

            var graphQLRequestBody = new { query = graphQLQuery, variables = graphQLVars };

            octoClientMock
                .Setup(o => o.Connection.Post<string>(
                            It.IsAny<Uri>(),
                            It.Is<object>(body => JsonConvert.SerializeObject(body) == JsonConvert.SerializeObject(graphQLRequestBody)), // body
                            It.IsAny<string>(),  // accepts
                            It.IsAny<string>(),  // contentType
                            It.IsAny<IDictionary<string, string>>(),  // parameters
                            It.IsAny<CancellationToken>()))
                    .Returns(responseGraphQL);

            return (gitHubAppClientMock, octoClientMock);
        }
    }
}
using System.Net;
using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;
using Newtonsoft.Json;
using GitHubActions.Gates.Framework.Exceptions;

namespace GitHubActions.Gates.Framework.Tests
{
    public class GitHubAppClientTests
    {
        [Fact]
        public async Task SetApprovalDecision_ShouldCallOctoKit()
        {
            var callbackUrl = "https://api.github.com/repos/octo/gates/actions/runs/4493385896/deployment_protection_rule";
            var expectedState = "approved";
            var expectedEnvironment = "production";
            var expectedComment = "Deployment approved";

            var expectedPayload = new
            {
                state = expectedState,
                environment_name = expectedEnvironment,
                comment = expectedComment
            };

            Mock<ILogger> log = new();
            var config = Factories.CreateConfigMock();

            var octoClientMock = new Mock<IGitHubClient>();

            octoClientMock
                .Setup(o => o.Connection.Post(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HttpStatusCode.OK);

            var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

            // get installation token will not be called since we are injecting an existing connection.

            await gitHubAppClient.SetApprovalDecision(callbackUrl, expectedState, expectedEnvironment, expectedComment);

            octoClientMock.Verify(
                o => o.Connection.Post(
                    new Uri(callbackUrl),
                    It.Is<object>(payload => JsonConvert.SerializeObject(payload) == JsonConvert.SerializeObject(expectedPayload)),
                    "application/vnd.github+json",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReportUpdate_ShouldCallOctoKit()
        {
            var callbackUrl = "https://api.github.com/repos/octo/gates/actions/runs/4493385896/deployment_protection_rule";
            var expectedEnvironment = "production";
            var expectedComment = "I'm delaying it";

            var expectedPayload = new
            {
                environment_name = expectedEnvironment,
                comment = expectedComment
            };

            Mock<ILogger> log = new();
            var config = Factories.CreateConfigMock();

            var octoClientMock = new Mock<IGitHubClient>();

            octoClientMock
                .Setup(o => o.Connection.Post(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HttpStatusCode.OK);

            var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

            // get installation token will not be called since we are injecting an existing connection.

            await gitHubAppClient.ReportUpdate(callbackUrl, expectedEnvironment, expectedComment);

            octoClientMock.Verify(
                o => o.Connection.Post(
                    new Uri(callbackUrl),
                    It.Is<object>(payload => JsonConvert.SerializeObject(payload) == JsonConvert.SerializeObject(expectedPayload)),
                    "application/vnd.github+json",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetInstallationToken_CalledTwice_CallsGitHubOnlyOnce()
        {
            Mock<ILogger> log = new();
            var expectedAccessToken = new AccessToken("ghs_16C7e42F292c6912E7710c838347Ae178B4a", new DateTimeOffset());
            var expectedAccessToken2 = new AccessToken("dummy2", new DateTimeOffset());

            var config = Factories.CreateConfigMock();

            var octoClientMock = new Mock<IGitHubClient>();

            octoClientMock
                .SetupSequence(o => o.GitHubApps.CreateInstallationToken(It.IsAny<long>()))
                .ReturnsAsync(expectedAccessToken)
                .ReturnsAsync(expectedAccessToken2); // This one should never be called

            var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

            var installToken1 = await gitHubAppClient.GetInstallationToken();
            var installToken2 = await gitHubAppClient.GetInstallationToken();

            Assert.Equal("ghs_16C7e42F292c6912E7710c838347Ae178B4a", installToken1);
            Assert.Equal("ghs_16C7e42F292c6912E7710c838347Ae178B4a", installToken2);

            octoClientMock.Verify(o => o.GitHubApps.CreateInstallationToken(It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public async Task GraphQLAsync_ReturnsData()
        {
            Mock<ILogger> log = new();
            var octoClientMock = new Mock<IGitHubClient>();

            var graphQLUrl = "https://api.github.com/graphql";
            var graphQLQuery = "query { viewer { login  } }";
            var expectedGraphQLResponse = "{\"data\": { \"viewer\": { \"login\": \"mona\"}}}";

            var graphQLRequestBody = new { query = graphQLQuery, variables = new { } };

            var responseGraphQL = OctokitResponseFactory.CreateApiResponse<string>(HttpStatusCode.OK, expectedGraphQLResponse);

            var config = Factories.CreateConfigMock();
            var gitHubAppClientMock = new Mock<GitHubAppClient>(octoClientMock.Object, 0L, log.Object, config.Object) { CallBase = true };

            ///////////////////////            
            octoClientMock
                .Setup(o => o.Connection.Post<string>(
                    It.IsAny<Uri>(),
                        It.IsAny<object>(),  // body
                        It.IsAny<string>(),  // accepts
                        It.IsAny<string>(),  // contentType
                        It.IsAny<IDictionary<string, string>>(),  // parameters
                        It.IsAny<CancellationToken>()))
                .Returns(responseGraphQL);

            dynamic? graphQLResponse = await gitHubAppClientMock.Object.GraphQLAsync(graphQLQuery, new { });

            octoClientMock.Verify(
                o => o.Connection.Post<object>(
                        new Uri(graphQLUrl),
                        It.Is<object>(payload => JsonConvert.SerializeObject(payload) == JsonConvert.SerializeObject(graphQLRequestBody)),
                        "application/vnd.github+json",
                        "application/json",
                        It.IsAny<IDictionary<string, string>>(),  // parameters
                        It.IsAny<CancellationToken>()),
                    Times.Once);

            Assert.NotNull(graphQLResponse);
            Assert.Equal("mona", (string)graphQLResponse!.data.viewer.login);
        }

        [Fact]
        public async Task GraphQLAsync_ThrowsErrorException()
        {
            Mock<ILogger> log = new();
            var octoClientMock = new Mock<IGitHubClient>();

            var graphQLUrl = "https://api.github.com/graphql";
            var graphQLQuery = "query { viewer { logi  } }";
            var expectedGraphQLResponse = "{\"errors\": [{\"path\": [\"query\",\"viewer\",\"logi\"],\"extensions\": {\"code\": \"undefinedField\",\"typeName\": \"User\",\"fieldName\": \"logi\"},\"locations\": [{\"line\": 8,\"column\": 5}],\"message\": \"Field 'logi' doesn't exist on type 'User'\"}]\r\n}";

            var graphQLRequestBody = new { query = graphQLQuery, variables = new { } };

            var responseGraphQL = OctokitResponseFactory.CreateApiResponse<string>(HttpStatusCode.OK, expectedGraphQLResponse);

            var config = Factories.CreateConfigMock();
            var gitHubAppClientMock = new Mock<GitHubAppClient>(octoClientMock.Object, 0L, log.Object, config.Object) { CallBase = true };

            ///////////////////////            
            octoClientMock
                .Setup(o => o.Connection.Post<string>(
                    It.IsAny<Uri>(),
                        It.IsAny<object>(),  // body
                        It.IsAny<string>(),  // accepts
                        It.IsAny<string>(),  // contentType
                        It.IsAny<IDictionary<string, string>>(),  // parameters
                        It.IsAny<CancellationToken>()))
                .Returns(responseGraphQL);

            var exception = await Assert.ThrowsAsync<GraphQLException>(async () => await gitHubAppClientMock.Object.GraphQLAsync(graphQLQuery, new { }));

            octoClientMock.Verify(
                o => o.Connection.Post<object>(
                        new Uri(graphQLUrl),
                        It.Is<object>(payload => JsonConvert.SerializeObject(payload) == JsonConvert.SerializeObject(graphQLRequestBody)),
                        "application/vnd.github+json",
                        "application/json",
                        It.IsAny<IDictionary<string, string>>(),  // parameters
                        It.IsAny<CancellationToken>()),
                    Times.Once);

            Assert.IsType<GraphQLException>(exception);
            Assert.Equal("API call failed with errors", exception.Message);
            Assert.Single(exception.Errors);
            Assert.Equal(new List<string> { "Field 'logi' doesn't exist on type 'User'" }, exception.Errors);
        }
    }
}
using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Exceptions;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using GitHubActions.Gates.Framework.Tests.Helpers;
using GitHubActions.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Octokit;
using System.Net;
using System.Text;

namespace GitHubActions.Gates.Framework.Tests
{
    public partial class ProcessingHandlerTests
    {
        static readonly DeploymentProtectionRuleWebHook webHookPayload = new()
        {
            deployment_callback_url = "https://api.github.com/repos/octo/gates/actions/runs/4493385896/deployment_protection_rule",
            environment = "production",
            installation = new() { id = 0 },
            repository = new() { full_name = "mona/lisa" , name = "lisa", owner = new() { login = "mona" } }
        };

        private static RepositoryContent CreateRepositoryContentFactory(string path, string content)
        {
            return new(Path.GetFileName(path),
                       path,
                       "dummysha",
                       323,
                       ContentType.File,
                       "https://github.com/mona/lisa/down",
                       "", "",
                       "https://github.com/mona/lisa/down",
                       "base64",
                       Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                       "", "");
        }

        public class AddComment
        {
            [Fact]
            public async Task ShouldCallGitHubClientWithExpectedParameters()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var handler = new TestableGate(client.Object, log.Object, webHookPayload);
                var comment = "Deployment updated";

                await handler.AddComment(comment);

                client.Verify(c => c.ReportUpdate(webHookPayload.deployment_callback_url, webHookPayload.environment, comment), Times.Once);
            }
        }

        public class Approve
        {
            [Fact]
            public async Task ShouldCallGitHubClientWithExpectedParameters()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var handler = new TestableGate(client.Object, log.Object, webHookPayload);
                var comment = "Deployment Approved";

                await handler.Approve(comment);

                client.Verify(c => c.Approve(webHookPayload.deployment_callback_url, webHookPayload.environment, comment), Times.Once);
            }

            [Fact]
            public async Task WithSchedule_ShouldEnqueue_NotCallGitHub()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                //var handler = new GateHelper(client.Object, log.Object, webHookPayload);
                var comment = "Deployment Approved";

                var processingHandlerMock = new Mock<TestableGate>(client.Object, log.Object, webHookPayload);

                var expectedQueueDate = new DateTime(2028, 1, 1);

                processingHandlerMock
                    .Protected()
                    .Setup("EnQueueProcessing", ItExpr.IsAny<DateTime>());

                processingHandlerMock.Setup(p => p.Approve(comment, expectedQueueDate)).CallBase();

                await processingHandlerMock.Object.Approve(comment, expectedQueueDate);

                processingHandlerMock
                    .Protected()
                    .Verify("EnQueueProcessing", Times.Once(), expectedQueueDate);

                client.Verify(c => c.Approve(webHookPayload.deployment_callback_url, webHookPayload.environment, comment), Times.Never);
                client.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task WithSchedule_ShouldNotEnqueueAndCallGitHub()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var comment = "Deployment Approved";

                var processingHandlerMock = new Mock<TestableGate>(client.Object, log.Object, webHookPayload);

                var expectedQueueDate = new DateTime(2028, 1, 1);

                processingHandlerMock
                    .Protected()
                    .Setup("EnQueueProcessing", ItExpr.IsAny<DateTime>());

                processingHandlerMock.Setup(p => p.Approve(comment, null)).CallBase();

                await processingHandlerMock.Object.Approve(comment);

                processingHandlerMock
                    .Protected()
                    .Verify("EnQueueProcessing", Times.Never(), expectedQueueDate);

                client.Verify(c => c.Approve(webHookPayload.deployment_callback_url, webHookPayload.environment, comment), Times.Once());
                client.VerifyNoOtherCalls();
            }
        }

        public class Reject
        {
            [Fact]
            public async Task ShouldCallGitHubClientWithExpectedParameters()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var handler = new TestableGate(client.Object, log.Object, webHookPayload);
                var comment = "Deployment rejected";

                await handler.Reject(comment);

                client.Verify(c => c.Reject(webHookPayload.deployment_callback_url, webHookPayload.environment, comment), Times.Once);
            }
        }
        public class LoadConfiguration
        {
            [Fact]
            public async Task ShouldLoadFileIfExists()
            {
                const string expectedRepoOwner = "mona";
                const string expectedRepoName = "lisa";
                RepositoryContent expectedFile = CreateRepositoryContentFactory(TestableGate.ConfigFilePath, @"Rules:
- Environment: test");

                var log = Factories.CreateLoggerMock();

                var config = new Mock<IConfiguration?>();

                var octoClientMock = new Mock<IGitHubClient>();

                octoClientMock
                    .Setup(o => o.Repository.Content.GetAllContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new[] { expectedFile });

                var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

                var handler = new TestableGate(gitHubAppClient, log.Object, webHookPayload);

                await handler.LoadConfiguration(expectedRepoOwner, expectedRepoName);

                octoClientMock.Verify(o =>
                    o.Repository.Content.GetAllContents(
                        expectedRepoOwner,
                        expectedRepoName,
                        TestableGate.ConfigFilePath)
                    , Times.Once);
            }

            [Fact]
            public async Task InvalidYAMLConfigFile_Rejects()
            {
                const string expectedRepoOwner = "mona";
                const string expectedRepoName = "lisa";
                RepositoryContent expectedFile = CreateRepositoryContentFactory(TestableGate.ConfigFilePath, "invalid");
                var log = Factories.CreateLoggerMock();

                var config = new Mock<IConfiguration?>();

                var octoClientMock = new Mock<IGitHubClient>();

                octoClientMock
                    .Setup(o => o.Repository.Content.GetAllContents(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(new[] { expectedFile });

                var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

                var handler = new TestableGate(gitHubAppClient, log.Object, webHookPayload);

                var exception = await Assert
                    .ThrowsAsync<RejectException>(async () => await handler.LoadConfiguration(expectedRepoOwner, expectedRepoName));

                Assert.Equal("Sorry I'm rejecting this. The gateHelperProcessing.yml file doesn't seem to be valid. Check if the YAML file is valid and it respect the configuration format. Error: Exception during deserialization", exception.Message);

                octoClientMock.Verify(o =>
                    o.Repository.Content.GetAllContents(
                        expectedRepoOwner,
                        expectedRepoName,
                        TestableGate.ConfigFilePath)
                    , Times.Once);
            }

            [Fact]
            public async Task ConfigFileNoPermission_Rejects()
            {
                const string expectedRepoOwner = "mona";
                const string expectedRepoName = "lisa";
                var log = Factories.CreateLoggerMock();

                var expectedExceptionMessage = "Sorry I'm rejecting this. I can't proceed, couldn't retrieve the config file gateHelperProcessing.yml. Error: Resource not accessible by integration";

                var config = new Mock<IConfiguration?>();

                var octoClientMock = new Mock<IGitHubClient>();

                var responseBody = "{\"message\":\"Resource not accessible by integration\",\"documentation_url\":\"https://docs.github.com/rest/reference/repos#get-repository-content\"}";
                var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, responseBody);
                var forbiddenException = new ForbiddenException(response);

                octoClientMock
                    .Setup(o => o.Repository.Content.GetAllContents(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Throws(forbiddenException);

                var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

                var handler = new TestableGate(gitHubAppClient, log.Object, webHookPayload);

                var exception = await Assert
                    .ThrowsAsync<RejectException>(async () => await handler.LoadConfiguration(expectedRepoOwner, expectedRepoName));

                Assert.Equal(expectedExceptionMessage, exception.Message);

                octoClientMock.Verify(o =>
                    o.Repository.Content.GetAllContents(
                        expectedRepoOwner,
                        expectedRepoName,
                        TestableGate.ConfigFilePath)
                    , Times.Once);
            }

            [Fact]
            public async Task ConfigFileDoesNotExist_Rejects()
            {
                const string expectedRepoOwner = "mona";
                const string expectedRepoName = "lisa";
                var log = Factories.CreateLoggerMock();

                var expectedExceptionMessage = "Sorry I'm rejecting this. I can't proceed, couldn't retrieve the config file gateHelperProcessing.yml. Error: Not Found";

                var config = new Mock<IConfiguration?>();

                var octoClientMock = new Mock<IGitHubClient>();

                var responseBody = "{\"message\":\"Not Found\",\"documentation_url\":\"https://docs.github.com/rest/reference/repos#get-repository-content\"}";
                var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.NotFound, responseBody);
                var notFoundException = new NotFoundException(response);

                octoClientMock
                    .Setup(o => o.Repository.Content.GetAllContents(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Throws(notFoundException);

                var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

                var handler = new TestableGate(gitHubAppClient, log.Object, webHookPayload);

                var exception = await Assert
                    .ThrowsAsync<RejectException>(async () => await handler.LoadConfiguration(expectedRepoOwner, expectedRepoName));

                Assert.Equal(expectedExceptionMessage, exception.Message);

                octoClientMock.Verify(o =>
                    o.Repository.Content.GetAllContents(
                        expectedRepoOwner,
                        expectedRepoName,
                        TestableGate.ConfigFilePath)
                    , Times.Once);
            }

            [Fact]
            public async Task WithSemanticErrors_RejectsWithFormattedErrorsMardown()
            {
                const string expectedRepoOwner = "mona";
                const string expectedRepoName = "lisa";
                RepositoryContent expectedFile = CreateRepositoryContentFactory(TestableGate.ConfigFilePath, "Rules:");
                var log = Factories.CreateLoggerMock();

                var expectedExceptionMessage = "Config file [gateHelperProcessing.yml](https://github.com/mona/lisa/down) is not valid:\n- Rules Cannot Be Empty\n- Rules Is required\n\n";

                var config = new Mock<IConfiguration?>();

                var octoClientMock = new Mock<IGitHubClient>();

                octoClientMock
                    .Setup(o => o.Repository.Content.GetAllContents(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(new[] { expectedFile });

                var gitHubAppClient = new GitHubAppClient(octoClientMock.Object, 0, log.Object, config.Object);

                var handler = new TestableGate(gitHubAppClient, log.Object, webHookPayload);

                var exception = await Assert
                    .ThrowsAsync<RejectException>(async () => await handler.LoadConfiguration(expectedRepoOwner, expectedRepoName));

                Assert.Equal(expectedExceptionMessage, exception.Message);

                octoClientMock.Verify(o =>
                    o.Repository.Content.GetAllContents(
                        expectedRepoOwner,
                        expectedRepoName,
                        TestableGate.ConfigFilePath)
                    , Times.Once);
            }
        }

        public class ProcessProcessing
        {
            [Fact]
            public async Task PreviousOutcome_IsApproved()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var expectedComment = "Deployment Approved";

                var message = new EventMessage()
                {
                    Outcome = new()
                    {
                        Comment = expectedComment,
                        State = OutcomeState.Approved
                    },
                    WebHookPayload = webHookPayload
                };

                var processingHandlerMock = new Mock<TestableGate>(client.Object, log.Object, webHookPayload);
                processingHandlerMock
                        .Setup(f => f.Approve(It.IsAny<string?>(), It.IsAny<DateTime?>()))
                        .Verifiable();

                processingHandlerMock
                    .Protected()
                    .Setup("ProcessProcessing", ItExpr.IsNull<EventMessage>(), ItExpr.IsNull<ILogger>())
                    .CallBase();

                await processingHandlerMock.Object.CallBaseProcessProcessingForTests(message, log.Object);

                processingHandlerMock
                    .Verify(m => m.Approve(expectedComment, null), Times.Once());

                client.VerifyNoOtherCalls();
            }


            [Fact]
            public async Task PreviousOutcome_IsApprovedQueued()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var expectedComment = "Deployment Approved";
                var expectedQueuedDate = new DateTime(2030, 1, 1);

                var message = new EventMessage()
                {
                    Outcome = new()
                    {
                        Comment = expectedComment,
                        State = OutcomeState.Approved,
                        Schedule = expectedQueuedDate
                    },
                    WebHookPayload = webHookPayload
                };

                var processingHandlerMock = new Mock<TestableGate>(client.Object, log.Object, webHookPayload);
                processingHandlerMock
                        .Setup(f => f.Approve(It.IsAny<string?>(), It.IsAny<DateTime?>()))
                        .Verifiable();

                processingHandlerMock
                    .Protected()
                    .Setup("ProcessProcessing", ItExpr.IsNull<EventMessage>(), ItExpr.IsNull<ILogger>())
                    .CallBase();

                await processingHandlerMock.Object.CallBaseProcessProcessingForTests(message, log.Object);

                processingHandlerMock
                    .Verify(m => m.Approve(expectedComment, expectedQueuedDate), Times.Once());

                client.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task PreviousOutcome_IsRejected()
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var expectedComment = "not today";

                var message = new EventMessage()
                {
                    Outcome = new()
                    {
                        Comment = expectedComment,
                        State = OutcomeState.Rejected
                    },
                    WebHookPayload = webHookPayload
                };

                var processingHandlerMock = new Mock<TestableGate>(client.Object, log.Object, webHookPayload);
                processingHandlerMock
                        .Setup(f => f.Reject(It.IsAny<string?>()))
                        .Verifiable();

                processingHandlerMock
                    .Protected()
                    .Setup("ProcessProcessing", ItExpr.IsNull<EventMessage>(), ItExpr.IsNull<ILogger>())
                    .CallBase();

                await processingHandlerMock.Object.CallBaseProcessProcessingForTests(message, log.Object);

                processingHandlerMock
                    .Verify(m => m.Reject(expectedComment), Times.Once());

                client.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task RateLimited_Handled()
            {
                var expectedQueueDate = new DateTime(2030, 4, 6, 8, 4, 31, DateTimeKind.Utc);
                var headers = new Dictionary<string, string>
                {
                    {"X-RateLimit-Limit", "100"},
                    {"X-RateLimit-Remaining", "42"},
                    {"X-RateLimit-Reset", "1901693071"},
                    { "X-Ratelimit-Resource", "core" }
                };

                var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
                var rateExceededException = new RateLimitExceededException(response);

                await RateLimitTests(rateExceededException, expectedQueueDate, "Handling rate limit inner RateLimitExceededException for core");
            }

            [Fact]
            public async Task AbuseRateLimited_Handled()
            {
                var expectedQueueDate = new DateTime(2030, 4, 6, 8, 4, 31, DateTimeKind.Utc);
                var headers = new Dictionary<string, string>
                {
                    {"X-RateLimit-Limit", "100"},
                    {"X-RateLimit-Remaining", "42"},
                    {"X-RateLimit-Reset", "1901693071"}
                };

                var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
                var rateExceededException = new AbuseException(response);

                await RateLimitTests(rateExceededException, expectedQueueDate, "Handling rate limit inner AbuseException for ");
            }

            [Fact]
            public async Task SecondaryRateLimited_Handled()
            {
                var expectedQueueDate = new DateTime(2030, 4, 6, 8, 4, 31, DateTimeKind.Utc);
                var headers = new Dictionary<string, string>
                {
                    {"X-RateLimit-Limit", "100"},
                    {"X-RateLimit-Remaining", "42"},
                    {"X-RateLimit-Reset", "1901693071"}
                };

                var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
                var rateExceededException = new SecondaryRateLimitExceededException(response);

                await RateLimitTests(rateExceededException, expectedQueueDate, "Handling rate limit inner SecondaryRateLimitExceededException for ");
            }

            [Fact]
            public static async Task Reject_RateLimitHandled()
            {
                var clientMock = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock();
                var message = new EventMessage() { WebHookPayload = webHookPayload };

                var processingHandlerMock = new Mock<TestableGateDynamicProcess>(clientMock.Object, log.Object, webHookPayload) { CallBase = true };

                processingHandlerMock
                    .Protected()
                    .SetupSequence<Task>("EnQueueProcessing", ItExpr.IsAny<DateTime>())
                    .Returns(Task.CompletedTask);

                var expectedQueueDate = new DateTime(2030, 4, 6, 8, 4, 31, DateTimeKind.Utc);
                var headers = new Dictionary<string, string>
                {
                    {"X-RateLimit-Limit", "100"},
                    {"X-RateLimit-Remaining", "42"},
                    {"X-RateLimit-Reset", "1901693071"}
                };

                var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
                var rateExceededException = new SecondaryRateLimitExceededException(response);

                clientMock
                    .Setup(c => c.Reject(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string?>()))
                    .Throws(rateExceededException);

                processingHandlerMock.Object.ProcessBody = (async () => await processingHandlerMock.Object.Reject("im just a unit test"));
                await processingHandlerMock.Object.CallBaseProcessProcessingForTests(message, log.Object);

                processingHandlerMock
                    .Protected()
                    .Verify("EnQueueProcessing", Times.Once(), expectedQueueDate);
            }

            private static async Task RateLimitTests(Exception throwException, DateTime expectedQueueDate, string expectedLogMessage)
            {
                var client = new Mock<IGitHubAppClient>();
                var log = Factories.CreateLoggerMock(true);
                var message = new EventMessage() { WebHookPayload = webHookPayload };

                var processingHandlerMock = new Mock<TestableGateDynamicProcess>(client.Object, log.Object, webHookPayload) { CallBase = true };

                processingHandlerMock
                    .Protected()
                    .SetupSequence<Task>("EnQueueProcessing", ItExpr.IsAny<DateTime>())
                    .Returns(Task.CompletedTask);

                var octoClientMock = new Mock<IGitHubClient>();

                octoClientMock
                    .Setup(o => o.User.Current())
                    .Throws(throwException);

                processingHandlerMock.Object.ProcessBody = (async () => await octoClientMock.Object.User.Current());

                await processingHandlerMock.Object.CallBaseProcessProcessingForTests(message, log.Object);

                processingHandlerMock
                    .Protected()
                    .Verify("EnQueueProcessing", Times.Once(), expectedQueueDate);

                log
                  .Verify(x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => string.Equals(expectedLogMessage, o.ToString(), StringComparison.InvariantCulture)),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once());

                client.VerifyNoOtherCalls();
            }
        }
    }

    public class GetRunID
    {
        [Fact]
        public void ReturnsRunID_WhenGivenValidCallbackUrl()
        {

            var callbackUrl = "https://api.github.com/repos/mona/lisa/actions/runs/4493385896/deployment_protection_rule";
            var expectedRunID = "4493385896";

            var result = TestableGate.GetRunID(callbackUrl);

            Assert.Equal(expectedRunID, result);
        }

        [Fact]
        public void ThrowsException_WhenGivenInvalidCallbackUrl()
        {
            var callbackUrl = "invalid url";

            Assert.Throws<UriFormatException>(() => TestableGate.GetRunID(callbackUrl));
        }

        [Fact]
        public void ThrowsException_WhenGivenNullCallbackUrl()
        {
            string? callbackUrl = null;

            Assert.Throws<ArgumentNullException>(() => TestableGate.GetRunID(callbackUrl));
        }

        [Fact]
        public void ThrowsException_WhenGivenEmptyCallbackUrl()
        {

            var callbackUrl = "";


            Assert.Throws<ArgumentException>(() => TestableGate.GetRunID(callbackUrl));
        }

        [Fact]
        public void ThrowsException_WhenGivenWhitespaceCallbackUrl()
        {

            var callbackUrl = "   ";


            Assert.Throws<ArgumentException>(() => TestableGate.GetRunID(callbackUrl));
        }
    }
}
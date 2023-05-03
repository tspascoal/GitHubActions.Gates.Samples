using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Tests.Helpers;
using Moq.Protected;
using Moq;
using System.Net;
using Microsoft.Extensions.Logging;
using Octokit.Internal;
using System.ComponentModel.DataAnnotations;
using Octokit;
using GitHubActions.Gates.Framework.Exceptions;

namespace GitHubActions.Gates.Framework.Tests
{
    public partial class GitHubRetryHandlerTests
    {
        public class ExecuteWithRetryAsync
        {
            [Fact]
            public async Task OneRetry()
            {
                var responses = new List<HttpResponseMessage>()
                {
                    Factories.CreateHttpResponseFactory(HttpStatusCode.InternalServerError, "{}"),
                    Factories.CreateHttpResponseFactory(HttpStatusCode.OK)
                };

                await TestRetriesHelper(responses);
            }

            [Fact]
            public async Task ExceedsRetriesAndReturnsFailedRequest()
            {
                var internalServerErrorExpectedResponse = Factories.CreateHttpResponseFactory(HttpStatusCode.InternalServerError, "{}");

                var responses = new List<HttpResponseMessage>()
                {
                    internalServerErrorExpectedResponse,
                    internalServerErrorExpectedResponse,
                    internalServerErrorExpectedResponse
                };

                await TestRetriesHelper(responses);
            }

            [Fact]
            public async Task NoRetry()
            {
                var responses = new List<HttpResponseMessage>()
                {
                    Factories.CreateHttpResponseFactory(HttpStatusCode.OK)
                };

                await TestRetriesHelper(responses);
            }

            private async Task TestRetriesHelper(IList<HttpResponseMessage> responses)
            {
                var log = Factories.CreateLoggerMock();

                var handler = HttpMessageHandlerFactory.CreateDefault();
                var retryHandler = new GitHubRetryHandler(handler, log.Object);

                var testUrl = "http://test.com/dummy";
                var handlerMock = new Mock<HttpMessageHandler>();
                var sequence = handlerMock
                    .Protected()
                    .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                                                      ItExpr.IsAny<HttpRequestMessage>(),
                                                      ItExpr.IsAny<CancellationToken>());

                var expectedNumberCalls = responses.Count;
                foreach (var r in responses)
                {
                    sequence.ReturnsAsync(r);
                }

                var httpClient = new HttpClient(handlerMock.Object);
                var response = await retryHandler.ExecuteWithRetryAsync(async () =>
                {
                    return await httpClient.GetAsync(testUrl);
                }, expectedNumberCalls - 1, 0);

                Assert.Equal(responses.Last().StatusCode, response.StatusCode);
                handlerMock.Protected()
                    .Verify<Task<HttpResponseMessage>>("SendAsync",
                                                       Times.Exactly(expectedNumberCalls),
                                                       ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri == testUrl),
                                                       ItExpr.IsAny<CancellationToken>());
            }
        }

        public class SendCoreAsync
        {
            /// <summary>
            /// Validates if the code that transforms responses into GitHub exception 
            /// is doing it's thing.
            /// 
            /// Since we are relying on calling private octokit.net members by reflection we
            /// might as well test it for future breaking changes.
            /// </summary>
            /// <returns></returns>
            [Fact]
            public async Task Throws_NotFoundException()
            {
                var log = Factories.CreateLoggerMock();

                var handler = HttpMessageHandlerFactory.CreateDefault();
                var retryHandlerMock = new Mock<GitHubRetryHandlerTestable>(handler, log.Object) { CallBase = true };

                var notFoundResponse = Factories.CreateHttpResponseFactory(HttpStatusCode.NotFound);

                retryHandlerMock
                    .Setup(r => r.SendCoreAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(notFoundResponse);

                var dummy = new HttpRequestMessage();

                await Assert.ThrowsAsync<NotFoundException>(async () =>
                {
                    // The request doesn't matter. We are mocking the response anyway
                    await retryHandlerMock.Object.SendAsync(dummy, CancellationToken.None);
                });
            }
        }
    }
}

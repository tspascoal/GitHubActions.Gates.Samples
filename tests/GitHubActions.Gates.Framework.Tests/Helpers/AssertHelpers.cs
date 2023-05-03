using Moq.Protected;
using Moq;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    internal static class AssertHelpers
    {
        internal static void AssertGetInstallationTokenResponse(Mock<HttpMessageHandler> handlerMock)
        {
            handlerMock.Protected()
                .Verify<Task<HttpResponseMessage>>("SendAsync",
                                       Times.Once(),
                                       ItExpr.Is<HttpRequestMessage>(r => r != null && r.RequestUri!.AbsoluteUri
                                                                          == "https://api.github.com/app/installations/0/access_tokens"),
                                       ItExpr.IsAny<CancellationToken>());
        }
    }
}

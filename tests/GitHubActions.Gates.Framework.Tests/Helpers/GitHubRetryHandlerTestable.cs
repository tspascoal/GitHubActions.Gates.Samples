using GitHubActions.Gates.Framework.Clients;
using Microsoft.Extensions.Logging;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    internal class GitHubRetryHandlerTestable : GitHubRetryHandler
    {
        public GitHubRetryHandlerTestable(HttpMessageHandler innerHandler, ILogger logger) : base(innerHandler, logger)
        {
        }
        public new async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

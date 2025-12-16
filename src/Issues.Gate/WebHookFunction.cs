using GitHubActions.Gates.Framework.FunctionHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Issues.Gate
{
    public class WebHookFunction : WebHookHandler
    {
        private readonly ILogger<WebHookFunction> _logger;

        public WebHookFunction(ILogger<WebHookFunction> logger)
        {
            _logger = logger;
        }

        [Function("IssuesGate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            return await ProcessWebHook(req, _logger, Constants.ProcessQueueName);
        }
    }
}
using GitHubActions.Gates.Framework.FunctionHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Issues.Gate
{
    public class WebHookFunction : WebHookHandler
    {
        [FunctionName("IssuesGate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await ProcessWebHook(req, log, Constants.ProcessQueueName);
        }
    }
}
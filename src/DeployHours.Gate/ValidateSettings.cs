using GitHubActions.Gates.Framework.FunctionHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DeployHours.Gate
{
    public class ValidateSettings : ValidationHandler
    {
        [FunctionName("ValidateSettings")]
        public async static Task<ContentResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await Validate(req, log);
        }
    }
}

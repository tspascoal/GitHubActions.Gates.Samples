using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GitHubActions.Gates.Framework.FunctionHandlers;

namespace Issues.Gate
{
    public class ValidateSettings: ValidationHandler
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

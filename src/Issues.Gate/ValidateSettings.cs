using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GitHubActions.Gates.Framework.FunctionHandlers;

namespace Issues.Gate
{
    public class ValidateSettings : ValidationHandler
    {
        private readonly ILogger<ValidateSettings> _logger;

        public ValidateSettings(ILogger<ValidateSettings> logger)
        {
            _logger = logger;
        }

        [Function("ValidateSettings")]
        public async Task<ContentResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            return await Validate(req, _logger);
        }
    }
}

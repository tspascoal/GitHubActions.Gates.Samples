using GitHubActions.Gates.Framework.FunctionHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DeployHours.Gate
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

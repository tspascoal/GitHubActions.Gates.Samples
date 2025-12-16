using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace GitHubActions.Gates.Framework.FunctionHandlers
{
    public class WebHookHandler
    {
        // Removes carriage return and newline characters to prevent log forging
        private static string SanitizeForLog(string input)
        {
            return input?.Replace("\r", "").Replace("\n", "");
        }
        protected const string DeploymentProtectionRuleEventName = "deployment_protection_rule";
        protected virtual async Task<IActionResult> ProcessWebHook(HttpRequest req, ILogger log, string ProcessingQueueName)
        {
            var ghEvent = req.Headers["X-GitHub-Event"].FirstOrDefault();
            var id = req.Headers["X-GitHub-Delivery"].FirstOrDefault();

            log.LogInformation($"EventReceiver Begin: [{SanitizeForLog(ghEvent)}] {SanitizeForLog(id)}");

            // No need to waste resources something is definitely missing
            if (string.IsNullOrWhiteSpace(ghEvent))
            {
                log.LogError("Missing Event");
                return new BadRequestObjectResult("Missing Event");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            IConfiguration config = Config.GetConfig();

            try
            {
                ValidateSignature.ValidateSignatureIfConfigured(config, log, req, requestBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed HMAC validation.");
                return new BadRequestObjectResult(ex.Message);
            }
            try
            {
                switch (ghEvent)
                {
                    case DeploymentProtectionRuleEventName:
                        return await ProcessDeploymentProtectionRuleEvent(ProcessingQueueName, ghEvent, id, requestBody, config, log);
                    case "installation_repositories":
                        return ProcessInstallationRepositoriesEvent(ghEvent, requestBody, log);
                    case "installation":
                        return ProcessInstallationEvent(requestBody, log);
                    default:
                        return new OkObjectResult("Ignored");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Unhandled exception while processing event {ghEvent} {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }
        private static ActionResult ProcessInstallationEvent(string requestBody, ILogger log)
        {
            var data = JsonConvert.DeserializeObject<dynamic>(requestBody);

            if (data == null)
                return new OkResult();

            string action = data.action;
            string id = data.installation.id;

            log.LogInformation($"Installation:{action} {id}");

            return new OkResult();
        }

        private static ActionResult ProcessInstallationRepositoriesEvent(string? ghEvent, string requestBody, ILogger log)
        {
            var data = JsonConvert.DeserializeObject<dynamic>(requestBody);

            if (data == null)
                return new OkResult();

            string action = data.action;
            string id = data.installation.id;

            dynamic listRepos;
            if (action == "added")
            {
                listRepos = data.repositories_added;
            }
            else if (action == "removed")
            {
                listRepos = data.repositories_removed;
            }
            else
            {
                log.LogInformation($"{action} ignored for {ghEvent}");
                return new OkObjectResult("Ignored");
            }

            var repos = new StringBuilder();
            foreach (var repoDefinition in listRepos)
            {
                if (repos.Length != 0)
                    repos.Append(',');
                repos.Append((string)repoDefinition.full_name);
            }
            log.LogInformation($"Install Repos {id} {action}: {repos}");

            return new OkResult();
        }
        private static async Task<ActionResult> ProcessDeploymentProtectionRuleEvent(string processingQueueName, string? ghEvent, string? id, string requestBody, IConfiguration config, ILogger log)
        {
            var data = JsonConvert.DeserializeObject<DeploymentProtectionRuleWebHook>(requestBody);

            if (data != null)
            {
                var payload = new EventMessage
                {
                    WebHookPayload = data,
                    Id = id
                };

                if (ghEvent == DeploymentProtectionRuleEventName && data.action == "requested")
                {
                    var sbClient = new ServiceBusClient(config);
                    await sbClient.SendMessage(processingQueueName, payload);

                    log.LogInformation($"{ghEvent} processed. Message enqueued");
                }
            }
            return new OkResult();
        }
    }
}

using GitHubActions.Gates.Framework;
using GitHubActions.Gates.Framework.FunctionHandlers;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Issues.Gate.Models;
using Issues.Gate.Rules;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Issues.Gate
{
    public class ProcessFunction : ProcessingHandler<IssuesConfiguration, IssueGateRule>
    {
        private readonly ILogger<ProcessFunction> _logger;

        public ProcessFunction(ILogger<ProcessFunction> logger) : base("Issues Gate", Constants.ProcessQueueName, ".github/issues-gate.yml")
        {
            _logger = logger;
        }

        [Function("IssuesProcess")]
        public async Task Run([ServiceBusTrigger(Constants.ProcessQueueName, Connection = Config.SERVICEBUSCONNECTIONNAME)] EventMessage message)
        {
            await ProcessProcessing(message, _logger);
        }

        /// <summary>
        /// Evalutes the rules
        /// 
        /// If an exception is thrown it will be rejected. Otherwise it will be approved
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected override async Task Process(DeploymentProtectionRuleWebHook payload)
        {
            var rules = new IssueGateRulesEvaluator(GitHubClient, Log, GateConfiguration);

            // Evaluate the rule. If it fails with an exception the base handler will take care rejecting the gate
            var comment = await rules.ValidateRules(payload.environment, GetRepository(), GetRunID());

            await Approve(comment); // If we reached this point and no exception has been thrown, then we will approve it
        }
    }
}

using System.Threading.Tasks;
using GitHubActions.Gates.Framework;
using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.FunctionHandlers;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Issues.Gate.Models;
using Issues.Gate.Rules;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Issues.Gate
{
    public class ProcessFunction : ProcessingHandler<IssuesConfiguration, IssueGateRule>
    {
        public ProcessFunction() : base("Issues Gate", Constants.ProcessQueueName, ".github/issues-gate.yml") { }

        [FunctionName("IssuesProcess")]
        public async Task Run([ServiceBusTrigger(Constants.ProcessQueueName, Connection = Config.SERVICEBUSCONNECTIONNAME)] EventMessage message, ILogger log)
        {
            await ProcessProcessing(message, log);
        }

        /// <summary>
        /// Evalutes the rules
        /// 
        /// If an exception is thrown it will be rejected. Otherwise it will be approved
        /// 
        /// </summary>
        /// <param name="webhook"></param>
        /// <returns></returns>
        protected override async Task Process(DeploymentProtectionRuleWebHook webhook)
        {
            var rules = new IssueGateRulesEvaluator(GitHubClient, GateConfiguration);

            // Evaluate the rule. If it fails with an exception the base handler will take care rejecting the gate
            await rules.ValidateRules(webhook.environment, GetRepository(), GetRunID());

            await Approve(); // If we reached this point and no exception has been thrown, then we will approve it
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using GitHubActions.Gates.Framework;
using GitHubActions.Gates.Framework.Models;
using DeployHours.Gate.Rules;
using DeployHours.Gate.Models;
using System.Globalization;
using GitHubActions.Gates.Framework.FunctionHandlers;
using GitHubActions.Gates.Framework.Models.WebHooks;

namespace DeployHours.Gate
{
    public class ProcessFunction : ProcessingHandler<DeployHoursConfiguration, DeployHoursRule>
    {
        public ProcessFunction() : base("Issues Gate", Constants.ProcessQueueName, ".github/deployhours-gate.yml") { }

        private const string LockoutMessage = "You can't deploy. We are in Lockout mode.";
        private const string DelayApprovalMessage = "Deploy requested outside deploy hours. Will be automatically approved on next deploy block on **{0:f} UTC**.";

        // Just so tests can inject their own rules. Need to find a more elegant solution
        protected virtual DeployHoursRulesEvaluator RulesFactory(DeployHoursConfiguration cfg) => new(cfg);

        [FunctionName("GatesProcess")]
        public async Task Run([ServiceBusTrigger(Constants.ProcessQueueName, Connection = Config.SERVICEBUSCONNECTIONNAME)] EventMessage message, ILogger log)
        {
            await ProcessProcessing(message, log);
        }

        /// <summary>
        /// Evalutes the rules.
        /// 
        /// Reject gate if in lockout mode
        /// 
        /// If in deploy hours, approve otherwise delay the approval until next deploy hour
        ///
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected override async Task Process(DeploymentProtectionRuleWebHook payload)
        {
            var rules = RulesFactory(GateConfiguration);

            if (rules.InLockout())
            {
                await Reject(LockoutMessage);
                return;
            }

            if (rules.IsDeployHour(DateTime.UtcNow, payload.environment))
            {
                await Approve();
                return;
            }

            // Not in deploy hours. Lets calculate the next deploy hour and delay the approval until then
            var approvalTime = rules.GetNextDeployHour(DateTime.UtcNow, payload.environment);

            // We (delay) approve first to avoid potential rate limits from the comment
            await Approve(null, approvalTime.ToUniversalTime());

            await AddComment(String.Format(CultureInfo.InvariantCulture, DelayApprovalMessage, approvalTime.ToUniversalTime()));
        }
    }
}

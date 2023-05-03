using GitHubActions.Gates.Framework.FunctionHandlers;
using GitHubActions.Gates.Framework.Clients;
using Microsoft.Extensions.Logging;
using GitHubActions.Gates.Framework.Models.WebHooks;
using GitHubActions.Gates.Framework.Models;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    public class TestableGate : ProcessingHandler<ConfigurationHelper, RuleHelper>
    {
        public const string ConfigFilePath = "gateHelperProcessing.yml";
        private const string GateName = "GateHelper";
        private const string QueueName = "GateHelperProcessing";

        public TestableGate(
            IGitHubAppClient client, ILogger log, DeploymentProtectionRuleWebHook webHookPayload) : base(client, webHookPayload, log, GateName, QueueName, ConfigFilePath)
        {
        }

        public TestableGate(
            IGitHubAppClient client, ILogger log, DeploymentProtectionRuleWebHook webHookPayload, string gateName = GateName, string queueName = QueueName, string configFilePath = ConfigFilePath) : base(client, webHookPayload, log, gateName, queueName, configFilePath)
        {
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task Process(DeploymentProtectionRuleWebHook webHookPayload)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException();
        }

        public async Task CallBaseProcessProcessingForTests(EventMessage message, ILogger log)
        {
            await base.ProcessProcessing(message, log);
        }
    }
}

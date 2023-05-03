#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using GitHubActions.Gates.Framework.Clients;
using Microsoft.Extensions.Logging;
using GitHubActions.Gates.Framework.Models.WebHooks;
using GitHubActions.Gates.Framework.Models;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    /// <summary>
    /// Allow testing of the processing by injecting a custom process body.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TestableGateDynamicProcess : TestableGate
    {
        // No config file. We don't want loading it. This is intentionally
        public const string configFilePath = null;

        public Func<Task> ProcessBody { get; set; }

        public TestableGateDynamicProcess(
            IGitHubAppClient client, ILogger log, DeploymentProtectionRuleWebHook webHookPayload) : base(client, log, webHookPayload, "GateHelper", "GateHelperProcessing", configFilePath)
        {
        }
        protected override async Task Process(DeploymentProtectionRuleWebHook webHookPayload)
        {
            if (ProcessBody != null) await ProcessBody();
        }

        public new async Task CallBaseProcessProcessingForTests(EventMessage message, ILogger log)
        {
            await base.ProcessProcessing(message, log);
        }
    }
}

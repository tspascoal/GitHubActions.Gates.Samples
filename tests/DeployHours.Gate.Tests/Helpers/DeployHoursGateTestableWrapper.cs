using DeployHours.Gate.Models;
using DeployHours.Gate.Rules;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Microsoft.Extensions.Logging;
using Moq;

namespace DeployHours.Gate.Tests.Helpers
{
    /// <summary>
    /// Wrapper to allow testing of the protected method
    /// </summary>
    public class DeployHoursGateTestableWrapper : ProcessFunction
    {
        public DeployHoursRulesEvaluator Rules { get; private set; }

        public DeployHoursGateTestableWrapper(DeployHoursRulesEvaluator rules) 
            : base(new Mock<ILogger<ProcessFunction>>().Object)
        {
            this.Rules = rules;
        }

        protected override DeployHoursRulesEvaluator RulesFactory(DeployHoursConfiguration cfg)
        {
            return Rules;
        }

        public async Task CallProcess(DeploymentProtectionRuleWebHook webhook)
        {
            await Process(webhook);
        }
    }
}

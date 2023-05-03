
using GitHubActions.Gates.Framework.Models.WebHooks;
using Microsoft.Extensions.Logging;
using Moq;

namespace DeployHours.Gate.Tests.Helpers
{
    public class Factories
    {
        internal static DeploymentProtectionRuleWebHook BuildWebHook()
        {
            // TODO: add a more representative webhook
            return new DeploymentProtectionRuleWebHook { environment = "production" };
        }
        internal static Mock<ILogger> CreateLoggerMock(bool callBase = false)
        {
            return new Mock<ILogger>() { CallBase = callBase };
        }
    }
}

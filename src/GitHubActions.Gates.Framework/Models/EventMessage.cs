#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using GitHubActions.Gates.Framework.Models.WebHooks;

namespace GitHubActions.Gates.Framework.Models
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class EventMessage
    {
        public bool Delayed { get; set; }
        public int TryNumber { get; set; }
        public int? RemainingTries { get; set; }
        public string? Id { get; set; }
        public GateOutcome? Outcome { get; set; }
        public DeploymentProtectionRuleWebHook WebHookPayload { get; set; }
    }
}

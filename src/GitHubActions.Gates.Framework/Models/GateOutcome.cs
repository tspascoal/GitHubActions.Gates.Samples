namespace GitHubActions.Gates.Framework.Models
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class GateOutcome
    {

        public OutcomeState State { get; set; }
        public string? Comment { get; set; }
        public DateTime? Schedule { get; set; }
    }
}
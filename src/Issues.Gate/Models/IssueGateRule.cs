using GitHubActions.Gates.Framework.Models;

namespace Issues.Gate.Models
{
    public class IssueGateRule : IGatesRule
    {

        public string Environment { get; set; } = string.Empty;        
        public int WaitMinutes { get; set; }
        public IssueGateSearch? Search { get; set; }
        public IssueGateIssues? Issues { get; set; }
    }
}

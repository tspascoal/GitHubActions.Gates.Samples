using System.Text.RegularExpressions;

namespace Issues.Gate.Models
{
    public class IssueGateIssues
    {        
        public int MaxAllowed { get; set; }
        public string? Repo { get; set; }
        public string? State { get; set; }
        public string? Assignee { get; set; }
        public string? Author { get; set; }
        public string? Mention { get; set; }
        public string? Milestone { get; set; }
        public List<string>? Labels { get; set; }
        public string? Message { get; set; }
        public bool OnlyCreatedBeforeWorkflowCreated { get; set; }

        public IList<string> Validate()
        {
            var errors = new List<string>();
            if (MaxAllowed < 0)
            {
                errors.Add("MaxAllowed must be equal or greater than 0");
            }

            ValidateRepo(Repo, errors);

            if (State != null && string.IsNullOrWhiteSpace(State))
            {
                errors.Add("If State is specified it cannot be empty");
            }
            if (Assignee != null && string.IsNullOrWhiteSpace(Assignee))
            {
                errors.Add("If Assignee is specified it cannot be empty");
            }
            if (Author != null && string.IsNullOrWhiteSpace(Author))
            {
                errors.Add("If Author is specified it cannot be empty");
            }
            if (Mention != null && string.IsNullOrWhiteSpace(Mention))
            {
                errors.Add("If Mention is specified it cannot be empty");
            }
            if (Milestone != null && (Milestone != "NONE" && Milestone != "*" && !long.TryParse(Milestone, out long _)))
            {
                errors.Add("Milestone needs to be either a number, * or NONE");
            }
            if (Message != null && string.IsNullOrWhiteSpace(Message))
            {
                errors.Add("When Message is specified it cannot be empty");
            }
            return errors;
        }

        private static void ValidateRepo(string? repo, List<string> errors)
        {
            if (repo == null) return;
            
            if (string.IsNullOrWhiteSpace(repo))
            {
                errors.Add("If Repo is specified it cannot be empty");
            } else {
                Regex repoRegex = new(@"[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*\/[a-zA-Z0-9-_]+$", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

                if (!repoRegex.IsMatch(repo))
                {
                    errors.Add("Repo must be in format owner/repository");
                }
            }
        }
    }
}

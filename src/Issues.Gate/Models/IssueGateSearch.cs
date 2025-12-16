using System.Collections.Generic;

namespace Issues.Gate.Models
{
    public class IssueGateSearch
    {
        public int MaxAllowed { get; set; }
        public string? Query { get; set; }
        public string? Message { get; set; }
        public bool OnlyCreatedBeforeWorkflowCreated { get; set; }

        public IList<string> Validate()
        {
            var errors = new List<string>();
            if (MaxAllowed < 0)
            {
                errors.Add("MaxAllowed must be equal or greater than 0");
            }
            if (string.IsNullOrWhiteSpace(Query))
            {
                errors.Add("Query must be specified");
            }
            if (Message != null && string.IsNullOrWhiteSpace(Message))
            {
                errors.Add("When Message is specified it cannot be empty");
            }
            return errors;
        }
    }
}

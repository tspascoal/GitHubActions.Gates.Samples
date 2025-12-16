using GitHubActions.Gates.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Issues.Gate.Tests")]
namespace Issues.Gate.Models
{

    public class IssuesConfiguration: BaseConfiguration<IssueGateRule>
    {
        // Class to hold the configuration for this YAML

        //Rules:
        //# Leave empty so the rule applies to any environment
        //- Environment:
        //  Search:
        //    MaxAllowed: 5
        //    Query: 'is:open is:issue label:bug'
        //    Message: 'Too many open bugs' # Optional message

        //- Environment: dummy
        //  Issues:
        //    MaxAllowed: 3
        //    State: "OPEN"
        //    Assignee: ""
        //    Author: ""
        //    Mention: ""
        //    Milestone: ""
        //    Labels:
        //      - BUG
        //      - show-stopper


        public override IList<string> Validate()
        {
            var validatorErrors = new List<string>();

            if (Rules == null || Rules.Count == 0)
            {
                validatorErrors.Add("Rules is mandatory");

                return validatorErrors;
            }
            else
            {
                foreach (var rule in Rules)
                {
                    if(rule.Search == null && rule.Issues == null)
                    {
                        validatorErrors.Add($"Rules for Environment: {(String.IsNullOrWhiteSpace(rule.Environment) ? "ANY" : rule.Environment)} has no Search or Rules element");
                        continue;
                    }

                    if (rule.Search != null)
                    {
                        validatorErrors.AddRange(rule.Search.Validate());
                    }
                    if (rule.Issues != null)
                    {
                        validatorErrors.AddRange(rule.Issues.Validate());
                    }
                }
            }

            return validatorErrors;
        }
    }
}

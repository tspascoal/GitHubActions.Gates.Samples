using GitHubActions.Gates.Framework.Exceptions;
using GitHubActions.Gates.Framework.Clients;
using Issues.Gate.Models;
using System;
using System.Threading.Tasks;
using System.Text;
using GitHubActions.Gates.Framework.Models;

namespace Issues.Gate.Rules
{
    public class IssueGateRulesEvaluator
    {
        private readonly IssuesConfiguration _configuration;
        private readonly IGitHubAppClient _client;

        public IssueGateRulesEvaluator(IGitHubAppClient client, IssuesConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        public async Task ValidateRules(string Environment, Repo Repository, long RunId)
        {
            var rule = _configuration.GetRule(Environment) ?? throw new RejectException($"No rule found for {Environment} environment");

            DateTime? workflowCreatedAt = null;

            // No need to make a call if the data isn't going to be used.
            // Remove this block once run data is part of the event payload.
            if (
                (rule.Search != null && rule.Search.OnlyCreatedBeforeWorkflowCreated) ||
                (rule.Issues != null && rule.Issues.OnlyCreatedBeforeWorkflowCreated)
            )
            {
                var client = await _client.GetOCtokit();

                var getRunResponse = await client.Actions.Workflows.Runs.Get(Repository.Owner, Repository.Name, RunId);

                workflowCreatedAt = getRunResponse.CreatedAt.UtcDateTime;
            }

            if (rule.Issues != null)
            {
                var repo = rule.Issues.Repo != null ? new Repo(rule.Issues.Repo) : Repository;

                var graphQLQuery = $@"query($owner: String!, $repo: String!, $limit: Int, $states: [IssueState!] = OPEN, $assignee: String, $author: String, $mention: String, $milestone: String, $labels: [String!], $since: DateTime) {{
                        repository(owner: $owner, name: $repo) {{
                            before: issues(first: $limit, states: $states, filterBy: {{ assignee: $assignee, createdBy: $author, mentioned: $mention, milestoneNumber: $milestone, labels: $labels}}) {{
                                      totalCount
                                    }}
                            after: issues(first: $limit, states: $states, filterBy: {{ since: $since, assignee: $assignee, createdBy: $author, mentioned: $mention, milestoneNumber: $milestone, labels: $labels}}) {{
                                      totalCount
                                    }}
                        }}   
                    }}";

                var parameters = new
                {
                    owner = repo.Owner,
                    repo = repo.Name,
                    limit = 0,
                    states = rule.Issues.State,
                    assignee = rule.Issues.Assignee,
                    author = rule.Issues.Author,
                    mention = rule.Issues.Mention,
                    milestone = rule.Issues.Milestone ?? "*",
                    labels = rule.Issues.Labels,
                    since = workflowCreatedAt
                };

                try
                {
                    dynamic response = await _client.GraphQLAsync(graphQLQuery, parameters);

                    int nrIssues = response.data.repository.before.totalCount;

                    if (rule.Issues.OnlyCreatedBeforeWorkflowCreated)
                    {
                        nrIssues -= (int)response.data.repository.after.totalCount;
                    }

                    if (nrIssues > rule.Issues.MaxAllowed)
                    {
                        if (String.IsNullOrWhiteSpace(rule.Issues.Message))
                        {
                            // TODO: it would be nice to transform this into a query and link to it with MD.
                            throw new RejectException($"You have **{nrIssues}** {Pluralize("issue", nrIssues)}, this exceeds maximum number **{rule.Issues.MaxAllowed}** in configured query.");
                        }
                        else
                        {
                            throw new RejectException(rule.Issues.Message);
                        }
                    }
                }
                catch (GraphQLException e)
                {
                    RejectWithErrors("Issues", e);
                }
            }

            if (rule.Search != null)
            {
                var query = rule.Search.Query;
                query += rule.Search.OnlyCreatedBeforeWorkflowCreated && workflowCreatedAt != null ? $" created:>={workflowCreatedAt.Value.ToUniversalTime():o}" : "";

                try
                {
                    dynamic response = await _client.GraphQLAsync("query($type: SearchType!, $limit: Int, $query: String!) { search(type: $type, first: $limit, query: $query) { issueCount } }",
                                                                  new { limit = 0, query, type = "ISSUE" });

                    int nrIssues = response.data.search.issueCount;

                    if (nrIssues > rule.Search.MaxAllowed)
                    {
                        if (String.IsNullOrEmpty(rule.Search.Message))
                        {
                            var encodedQuery = Uri.EscapeDataString(query);

                            throw new RejectException($"You have **{nrIssues}** {Pluralize("issue", nrIssues)}, this exceeds maximum number **{rule.Search.MaxAllowed}** in configured [search](/search?q={encodedQuery})");
                        }
                        else
                        {
                            throw new RejectException(rule.Search.Message);
                        }
                    }
                }
                catch (GraphQLException e)
                {
                    RejectWithErrors("Search", e);
                }

            }
        }

        private static void RejectWithErrors(string queryType, GraphQLException e)
        {
            StringBuilder mdErrors = new();
            foreach (var errorMessage in e.Errors)
            {
                mdErrors.AppendLine($"- {errorMessage}");
            }

            throw new RejectException($"Sorry, I have to reject this. {queryType} query execution failed with {Pluralize("error", e.Errors.Count)}:\n{mdErrors}");
        }

        private static string Pluralize(string text, int nrIssues)
        {
            return nrIssues > 1 ? $"{text}s" : text;
        }
    }
}

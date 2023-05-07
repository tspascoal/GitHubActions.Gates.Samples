using GitHubActions.Gates.Framework.Exceptions;
using GitHubActions.Gates.Framework.Clients;
using Issues.Gate.Models;
using System;
using System.Threading.Tasks;
using System.Text;
using GitHubActions.Gates.Framework.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

// Needed for unit tests
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Issues.Gate.Rules
{
    public class IssueGateRulesEvaluator
    {
        private readonly IssuesConfiguration _configuration;
        private readonly IGitHubAppClient _client;
        private readonly ILogger _log;

        public IssueGateRulesEvaluator(IGitHubAppClient client, ILogger log, IssuesConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
            _log = log;
        }

        public async virtual Task<string> ValidateRules(string environment, Repo repository, long RunId)
        {
            var rule = _configuration.GetRule(environment) ?? throw new RejectException($"No rule found for {environment} environment");

            DateTime? workflowCreatedAt = null;

            StringBuilder comment = new("Evaluated Rules:\n");

            // No need to make a call if the data isn't going to be used.
            // Remove this block once run data is part of the event payload.
            if (
                (rule.Search != null && rule.Search.OnlyCreatedBeforeWorkflowCreated) ||
                (rule.Issues != null && rule.Issues.OnlyCreatedBeforeWorkflowCreated)
            )
            {
                var client = await _client.GetOCtokit();

                var getRunResponse = await client.Actions.Workflows.Runs.Get(repository.Owner, repository.Name, RunId);

                workflowCreatedAt = getRunResponse.CreatedAt.UtcDateTime;
            }

            // Issues is executed first, because if issues fails (less costly due to rate limits) so if it fails
            // we don't need to execute search which is more expensive in terms of rate limits
            if (rule.Issues != null)
            {
                await ExecuteIssuesQuery(repository, rule, workflowCreatedAt, comment);
            }
            if (rule.Search != null)
            {
                await ExecuteSearchQuery(rule, workflowCreatedAt, comment);
            }
            return comment.ToString();
        }

        internal async virtual Task ExecuteIssuesQuery(Repo Repository, IssueGateRule rule, DateTime? workflowCreatedAt, StringBuilder comment)
        {
            var repo = rule.Issues.Repo != null ? new Repo(rule.Issues.Repo) : Repository;

            var graphQLQuery = $@"query($owner: String!, $repo: String!, $limit: Int, $states: [IssueState!] = OPEN, $assignee: String, $author: String, $mention: String, $milestone: String, $labels: [String!], $since: DateTime) {{
                        repository(owner: $owner, name: $repo) {{
                            total: issues(first: $limit, states: $states, filterBy: {{ assignee: $assignee, createdBy: $author, mentioned: $mention, milestoneNumber: $milestone, labels: $labels}}) {{
                                      totalCount
                                    }}
                            after: issues(first: $limit, states: $states, filterBy: {{ since: $since, assignee: $assignee, createdBy: $author, mentioned: $mention, milestoneNumber: $milestone, labels: $labels}}) {{
                                      totalCount
                                    }}
                        }}   
                    }}";

            object parameters = BuildIssuesQueryParameters(repo, rule.Issues, workflowCreatedAt);

            try
            {
                dynamic response = await _client.GraphQLAsync(graphQLQuery, parameters);

                int nrIssues = response.data.repository.total.totalCount;

                _log.LogInformation($"IssuesQuery: Total {nrIssues} After {response.data.repository.after.totalCount} issues with max {rule.Issues.MaxAllowed}");

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
                comment.AppendLine($"- **Issues** found **{nrIssues}** {Pluralize("issue", nrIssues)} {BelowEqualThresholdText(nrIssues, rule.Issues.MaxAllowed)}.");
            }
            catch (GraphQLException e)
            {
                RejectWithErrors("Issues", e);
            }
        }
        internal async virtual Task ExecuteSearchQuery(IssueGateRule rule, DateTime? workflowCreatedAt, StringBuilder comment)
        {
            string query = BuildSearchQuery(rule, workflowCreatedAt);

            try
            {
                dynamic response = await _client.GraphQLAsync("query($type: SearchType!, $limit: Int, $query: String!) { search(type: $type, first: $limit, query: $query) { issueCount } }",
                                                              new { limit = 0, query, type = "ISSUE" });

                int nrSearchIssues = response.data.search.issueCount;

                _log.LogInformation($"SearchQuery: ${nrSearchIssues} issues max ${rule.Search.MaxAllowed}");

                if (nrSearchIssues > rule.Search.MaxAllowed)
                {
                    if (String.IsNullOrEmpty(rule.Search.Message))
                    {
                        var encodedQuery = Uri.EscapeDataString(query);

                        throw new RejectException($"You have **{nrSearchIssues}** {Pluralize("issue", nrSearchIssues)}, this exceeds maximum number **{rule.Search.MaxAllowed}** in configured [search](/search?q={encodedQuery})");
                    }
                    else
                    {
                        throw new RejectException(rule.Search.Message);
                    }
                }
                comment.AppendLine($"- **Search** found **{nrSearchIssues}** {Pluralize("issue", nrSearchIssues)} {BelowEqualThresholdText(nrSearchIssues, rule.Search.MaxAllowed)}.");
            }
            catch (GraphQLException e)
            {
                RejectWithErrors("Search", e);
            }
        }

        internal static string BuildSearchQuery(IssueGateRule rule, DateTime? workflowCreatedAt)
        {
            var query = rule.Search.Query;
            query += rule.Search.OnlyCreatedBeforeWorkflowCreated && workflowCreatedAt != null ? $" created:<{workflowCreatedAt.Value.ToUniversalTime():o}" : "";

            return query;
        }

        /// <summary>
        /// Build the parameters for the Issues GraphQL query
        /// 
        /// Enforces the following semantic for milestones
        /// <list type="bullet">
        /// <item>If Milestone filter is specified then only issues with that given milestone are returned (use * for any milestone)</item>
        /// <item>If Milestone filter is not specified then milestone value should be ignored(all other filter applies)</item>
        /// <item>if Milestone filter is NONE then only issues with no milestones should be considered</item>
        /// </list>
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="issues"></param>
        /// <param name="workflowCreatedAt"></param>
        /// <returns></returns>
        internal static object BuildIssuesQueryParameters(Repo repo, IssueGateIssues issues, DateTime? workflowCreatedAt)
        {
            // milestone filter is ONLY added when is not null

            // Very ugly code that doesn't scale if we need to apply same logic to other fields.
            // Dealing with expandos was not nicer

            var parameters = new
            {
                owner = repo.Owner,
                repo = repo.Name,
                limit = 0,
                states = issues.State,
                assignee = issues.Assignee,
                author = issues.Author,
                mention = issues.Mention,
                milestone = issues.Milestone == "NONE" ? null : issues.Milestone,
                labels = issues.Labels,
                since = workflowCreatedAt
            };

            if (issues.Milestone == null)
            {
                // milestone is NOT added on purpose
                return new
                {
                    parameters.owner,
                    parameters.repo,
                    parameters.limit,
                    parameters.states,
                    parameters.assignee,
                    parameters.author,
                    parameters.mention,
                    parameters.labels,
                    parameters.since
                };
            }

            return parameters;
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
            return nrIssues == 1 ? text : $"{text}s";
        }
        private static string BelowEqualThresholdText(int nrIssues, int maxAllowed)
        {
            return $"which is {(nrIssues < maxAllowed ? "below" : "equal to")} threshold of **{maxAllowed}**";
        }
    }
}

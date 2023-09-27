using System.Net;
using System.Text;
using Octokit;
using Moq;

using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Models;
using Issues.Gate.Models;
using Issues.Gate.Rules;
using GitHubActions.TestHelpers;
using Newtonsoft.Json;
using GitHubActions.Gates.Framework.Exceptions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using GitHubActions.Gates.Framework;
using GitHubActions.TestHelpers.Assert;
using Microsoft.CSharp.RuntimeBinder;
using System.Data;

namespace Issues.Gate.Tests
{
    public class IssueGatesRulesEvaluatorTests
    {
        public class BuildSearchQuery
        {
            [Fact]
            public void OnlyCreatedBeforeWorkflowCreatedFalse_DatePredicateNotAdded()
            {
                var rule = new IssueGateRule
                {
                    Search = new()
                    {
                        MaxAllowed = 2,
                        OnlyCreatedBeforeWorkflowCreated = false,
                        Query = "repo:mona/lisa"
                    }
                };

                var query = IssueGateRulesEvaluator.BuildSearchQuery(rule, null);

                Assert.Equal(rule.Search.Query, query);
            }

            [Fact]
            public void OnlyCreatedBeforeWorkflowCreatedTrue_DatePredicateAdded()
            {
                var rule = new IssueGateRule
                {
                    Search = new()
                    {
                        MaxAllowed = 2,
                        OnlyCreatedBeforeWorkflowCreated = true,
                        Query = "repo:mona/lisa"
                    }
                };

                var dateTime = new DateTime(2023, 1, 3, 22, 1, 0, DateTimeKind.Utc);

                var query = IssueGateRulesEvaluator.BuildSearchQuery(rule, dateTime);

                Assert.Equal($"{rule.Search.Query} created:<2023-01-03T22:01:00.0000000Z", query);
            }

            [Fact]
            public void OnlyCreatedBeforeWorkflowCreatedFalseButDateProvided_DatePredicateNotAdded()
            {
                var rule = new IssueGateRule
                {
                    Search = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false, Query = "repo:mona/lisa" }
                };

                var dateTime = new DateTime(2023, 1, 3, 22, 1, 0, DateTimeKind.Utc);

                var query = IssueGateRulesEvaluator.BuildSearchQuery(rule, dateTime);

                Assert.Equal(rule.Search.Query, query);
            }
        }

        public class BuildIssuesQueryParameters
        {
            [Fact]
            public void MilestoneDefined_MilestoneAdded()
            {
                var issues = new IssueGateIssues()
                {
                    Milestone = "2"
                };

                dynamic parameters = IssueGateRulesEvaluator.BuildIssuesQueryParameters(new Repo("mona/lisa"), issues, null);

                Assert.Equal("2", parameters.milestone);
            }

            [Fact]
            public void MilestoneSetNone_MilestoneAddedButNull()
            {
                var issues = new IssueGateIssues()
                {
                    Milestone = "NONE"
                };

                dynamic parameters = IssueGateRulesEvaluator.BuildIssuesQueryParameters(new Repo("mona/lisa"), issues, null);

                Assert.Null(parameters.milestone);
            }

            [Fact]
            public void MilestoneNotDefined_MilestoneNotAdded()
            {
                var issues = new IssueGateIssues() { };

                dynamic parameters = IssueGateRulesEvaluator.BuildIssuesQueryParameters(new Repo("mona/lisa"), issues, null);

                Assert.Throws<RuntimeBinderException>(() => parameters.milestone);
            }
        }

        public class ExecuteSearchQuery
        {
            [Fact]
            public async void OverThreshold_ThrowsRejectException()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Search = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false, Query = "repo:mona/lisa" }
                };

                var graphQLQuery = "query($type: SearchType!, $limit: Int, $query: String!) { search(type: $type, first: $limit, query: $query) { issueCount } }";
                var graphQLVars = new { limit = 0, query = rule.Search.Query, type = "ISSUE" };
                var expectedGraphQLResponse = "{\"data\": {\"search\": {\"issueCount\": 30}}}";

                (var gitHubAppClientMock, _) = Factories.CreateGitHubClientForGraphl(log, graphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                var exception = await Assert.ThrowsAsync<RejectException>(async () => await evaluator.ExecuteSearchQuery(rule, null, comment));

                Assert.Equal("You have **30** issues, this exceeds maximum number **2** in configured [search](/search?q=repo%3Amona%2Flisa)", exception.Message);
            }

            [Fact]
            public async void OverThreshold_CustomMessage_ThrowsRejectException()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Search = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false, Query = "repo:mona/lisa", Message = "CustomMessage" }
                };

                var graphQLQuery = "query($type: SearchType!, $limit: Int, $query: String!) { search(type: $type, first: $limit, query: $query) { issueCount } }";
                var graphQLVars = new { limit = 0, query = rule.Search.Query, type = "ISSUE" };
                var expectedGraphQLResponse = "{\"data\": {\"search\": {\"issueCount\": 30}}}";

                (var gitHubAppClientMock, _) = Factories.CreateGitHubClientForGraphl(log, graphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                var exception = await Assert.ThrowsAsync<RejectException>(async () => await evaluator.ExecuteSearchQuery(rule, null, comment));

                Assert.Equal("CustomMessage", exception.Message);
            }


            [Fact]
            public async void EqualThreshold_AddsComment()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Search = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false, Query = "repo:mona/lisa" }
                };

                var graphQLQuery = "query($type: SearchType!, $limit: Int, $query: String!) { search(type: $type, first: $limit, query: $query) { issueCount } }";
                var graphQLVars = new { limit = 0, query = rule.Search.Query, type = "ISSUE" };

                var expectedGraphQLResponse = "{\"data\": {\"search\": {\"issueCount\": 2}}}";

                (var gitHubAppClientMock, var octoClientMock) = Factories.CreateGitHubClientForGraphl(log, graphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                await evaluator.ExecuteSearchQuery(rule, null, comment);

                Assert.Equal("- **Search** found **2** issues which is equal to threshold of **2**." + Environment.NewLine, comment.ToString());

                GitHubApiAssert.AssertGraphQLCall(octoClientMock, graphQLQuery, graphQLVars);
            }

            [Fact]
            public async void BelowThreshold_And_OnlyCreatedBeforeWorkflowCreatedTrue_AddsComment()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Search = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = true, Query = "repo:mona/lisa" }
                };

                var graphQLQuery = "query($type: SearchType!, $limit: Int, $query: String!) { search(type: $type, first: $limit, query: $query) { issueCount } }";
                var graphQLVars = new
                {
                    limit = 0,
                    query = $"{rule.Search.Query} created:<2023-10-12T07:54:00.0000000Z",
                    type = "ISSUE"
                };

                var expectedGraphQLResponse = "{\"data\": {\"search\": {\"issueCount\": 1}}}";

                (var gitHubAppClientMock, var octoClientMock) = Factories.CreateGitHubClientForGraphl(log, graphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                var workflowCreatedAt = new DateTime(2023, 10, 12, 7, 54, 0, 0, DateTimeKind.Utc);

                await evaluator.ExecuteSearchQuery(rule, workflowCreatedAt, comment);

                Assert.Equal("- **Search** found **1** issue which is below threshold of **2**." + Environment.NewLine, comment.ToString());

                GitHubApiAssert.AssertGraphQLCall(octoClientMock, graphQLQuery, graphQLVars);
            }
        }

        public class ExecuteIssuesQuery
        {
            private const string issuesGraphQLQuery = $@"query($owner: String!, $repo: String!, $limit: Int, $states: [IssueState!] = OPEN, $assignee: String, $author: String, $mention: String, $milestone: String, $labels: [String!], $since: DateTime) {{
                        repository(owner: $owner, name: $repo) {{
                            total: issues(first: $limit, states: $states, filterBy: {{ assignee: $assignee, createdBy: $author, mentioned: $mention, milestoneNumber: $milestone, labels: $labels}}) {{
                                      totalCount
                                    }}
                            after: issues(first: $limit, states: $states, filterBy: {{ since: $since, assignee: $assignee, createdBy: $author, mentioned: $mention, milestoneNumber: $milestone, labels: $labels}}) {{
                                      totalCount
                                    }}
                        }}   
                    }}";

            [Fact]
            public async void OverThresholdOnlyCreatedBeforeWorkflowCreatedFalse_ThrowsRejectException()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Issues = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false }
                };

                DateTime? workflowCreatedAt = null;

                var graphQLVars = new
                {
                    owner = "mona",
                    repo = "lisa",
                    limit = 0,
                    states = null as string,
                    assignee = null as string,
                    author = null as string,
                    mention = null as string,
                    labels = null as string[],
                    since = workflowCreatedAt
                };

                var expectedGraphQLResponse = "{\"data\": {\"repository\": {\"total\": {\"totalCount\": 3},\"after\": {\"totalCount\": 3}}}\r\n}";

                (var gitHubAppClientMock, _) = Factories.CreateGitHubClientForGraphl(log, issuesGraphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                var exception = await Assert.ThrowsAsync<RejectException>(async () => await evaluator.ExecuteIssuesQuery(new Repo("mona/lisa"), rule, workflowCreatedAt, comment));

                Assert.Equal("You have **3** issues, this exceeds maximum number **2** in configured query.", exception.Message);
            }

            [Fact]
            public async void OverThresholdOnlyCreatedBeforeWorkflowCreatedTrue_ThrowsRejectException()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Issues = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = true }
                };

                var workflowCreatedAt = new DateTime(2023, 12, 10, 23, 2, 0, DateTimeKind.Utc);

                var graphQLVars = new
                {
                    owner = "mona",
                    repo = "lisa",
                    limit = 0,
                    states = null as string,
                    assignee = null as string,
                    author = null as string,
                    mention = null as string,
                    labels = null as string[],
                    since = workflowCreatedAt
                };

                var expectedGraphQLResponse = "{\"data\": {\"repository\": {\"total\": {\"totalCount\": 4},\"after\": {\"totalCount\": 1}}}\r\n}";

                (var gitHubAppClientMock, _) = Factories.CreateGitHubClientForGraphl(log, issuesGraphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                var exception = await Assert.ThrowsAsync<RejectException>(async () => await evaluator.ExecuteIssuesQuery(new Repo("mona/lisa"), rule, workflowCreatedAt, comment));

                Assert.Equal("You have **3** issues, this exceeds maximum number **2** in configured query.", exception.Message);
            }


            [Fact]
            public async void OverThreshold_CustomMessage_ThrowsRejectException()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Issues = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false, Message = "CustomMessage" }
                };

                DateTime? workflowCreatedAt = null;

                var graphQLVars = new
                {
                    owner = "mona",
                    repo = "lisa",
                    limit = 0,
                    states = null as string,
                    assignee = null as string,
                    author = null as string,
                    mention = null as string,
                    labels = null as string[],
                    since = workflowCreatedAt
                };

                var expectedGraphQLResponse = "{\"data\": {\"repository\": {\"total\": {\"totalCount\": 3},\"after\": {\"totalCount\": 3}}}\r\n}";

                (var gitHubAppClientMock, _) = Factories.CreateGitHubClientForGraphl(log, issuesGraphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                var exception = await Assert.ThrowsAsync<RejectException>(async () => await evaluator.ExecuteIssuesQuery(new Repo("mona/lisa"), rule, workflowCreatedAt, comment));

                Assert.Equal("CustomMessage", exception.Message);
            }


            [Fact]
            public async void EqualThreshold_AddsComment()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Issues = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = false }
                };

                var workflowCreatedAt = new DateTime(2023, 12, 10, 23, 2, 0, DateTimeKind.Utc);

                var graphQLVars = new
                {
                    owner = "mona",
                    repo = "lisa",
                    limit = 0,
                    states = null as string,
                    assignee = null as string,
                    author = null as string,
                    mention = null as string,
                    labels = null as string[],
                    since = workflowCreatedAt
                };


                var expectedGraphQLResponse = "{\"data\": {\"repository\": {\"total\": {\"totalCount\": 2},\"after\": {\"totalCount\": 2}}}\r\n}";

                (var gitHubAppClientMock, var octoClientMock) = Factories.CreateGitHubClientForGraphl(log, issuesGraphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                await evaluator.ExecuteIssuesQuery(new Repo("mona/lisa"), rule, workflowCreatedAt, comment);

                Assert.Equal("- **Issues** found **2** issues which is equal to threshold of **2**." + Environment.NewLine, comment.ToString());

                GitHubApiAssert.AssertGraphQLCall(octoClientMock, issuesGraphQLQuery, graphQLVars);
            }

            [Fact]
            public async void BelowThreshold_And_OnlyCreatedBeforeWorkflowCreatedTrue_AddsComment()
            {
                var log = Factories.CreateLoggerMock();
                var comment = new StringBuilder();

                var rule = new IssueGateRule
                {
                    Issues = new() { MaxAllowed = 2, OnlyCreatedBeforeWorkflowCreated = true }
                };

                var workflowCreatedAt = new DateTime(2023, 12, 10, 23, 2, 0, DateTimeKind.Utc);

                var graphQLVars = new
                {
                    owner = "mona",
                    repo = "lisa",
                    limit = 0,
                    states = null as string,
                    assignee = null as string,
                    author = null as string,
                    mention = null as string,
                    labels = null as string[],
                    since = workflowCreatedAt
                };


                var expectedGraphQLResponse = "{\"data\": {\"repository\": {\"total\": {\"totalCount\": 2},\"after\": {\"totalCount\": 2}}}\r\n}";

                (var gitHubAppClientMock, var octoClientMock) = Factories.CreateGitHubClientForGraphl(log, issuesGraphQLQuery, graphQLVars, expectedGraphQLResponse);

                var evaluator = new IssueGateRulesEvaluator(gitHubAppClientMock.Object, log.Object, null);

                await evaluator.ExecuteIssuesQuery(new Repo("mona/lisa"), rule, workflowCreatedAt, comment);

                Assert.Equal("- **Issues** found **0** issues which is below threshold of **2**." + Environment.NewLine, comment.ToString());

                GitHubApiAssert.AssertGraphQLCall(octoClientMock, issuesGraphQLQuery, graphQLVars);
            }
        }

        public class ValidateRules
        {
            [Fact]
            public async Task NoRuleForEnvironment_ThrowRejection()
            {
                var configuration = new IssuesConfiguration()
                {
                    Rules = new List<IssueGateRule> {
                            new IssueGateRule() { Environment = "Production" }
                    }
                };
                var log = Factories.CreateLoggerMock();

                var evaluator = new IssueGateRulesEvaluator(null, log.Object, configuration);

                await Assert.ThrowsAsync<RejectException>(async () => await evaluator.ValidateRules("dummy", new Repo("mona/lisa"), 0L));
            }

            [Fact]
            public async Task IssuesDefined_ExecutesIssuesQuery()
            {
                var configuration = new IssuesConfiguration()
                {
                    Rules = new List<IssueGateRule> {
                            new IssueGateRule()
                            {
                                Environment = "Production",
                                Issues = new IssueGateIssues()
                            }

                    }
                };
                var log = Factories.CreateLoggerMock();

                var appClientMock = new Mock<IGitHubAppClient>();
                var evaluatorMock = new Mock<IssueGateRulesEvaluator>(appClientMock.Object, log.Object, configuration);
                var repo = new Repo("mona/lisa");

                evaluatorMock
                    .Setup(e => e.ValidateRules(
                        It.IsAny<string>(),
                        It.IsAny<Repo>(),
                        It.IsAny<long>())
                    ).CallBase();

                await evaluatorMock.Object.ValidateRules("Production", repo, 0L);

                evaluatorMock.
                    Verify(e => e.ValidateRules("Production", repo, 0L));

                evaluatorMock.
                    Verify(e => e.ExecuteIssuesQuery(
                                repo,
                                configuration.Rules[0],
                                null,
                                It.IsAny<StringBuilder>()
                        ), Times.Once);

                evaluatorMock.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task SearchDefined_ExecutesSearchQuery()
            {
                var configuration = new IssuesConfiguration()
                {
                    Rules = new List<IssueGateRule> {
                            new ()
                            {
                                Environment = "Production",
                                Search = new ()
                            }

                    }
                };
                var log = Factories.CreateLoggerMock();

                var appClientMock = new Mock<IGitHubAppClient>();
                var evaluatorMock = new Mock<IssueGateRulesEvaluator>(appClientMock.Object, log.Object, configuration);
                var repo = new Repo("mona/lisa");

                evaluatorMock
                    .Setup(e => e.ValidateRules(
                        It.IsAny<string>(),
                        It.IsAny<Repo>(),
                        It.IsAny<long>())
                    ).CallBase();

                await evaluatorMock.Object.ValidateRules("Production", repo, 0L);

                evaluatorMock.
                    Verify(e => e.ValidateRules("Production", repo, 0L));

                evaluatorMock.
                    Verify(e => e.ExecuteSearchQuery(
                                configuration.Rules[0],
                                null,
                                It.IsAny<StringBuilder>()
                        ), Times.Once);

                evaluatorMock.VerifyNoOtherCalls();
            }
        }
    }
}
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
    }
}
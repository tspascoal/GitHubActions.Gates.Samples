using Issues.Gate.Models;
using System.Diagnostics.CodeAnalysis;

namespace Issues.Gate.Tests
{
    public class IssuesConfigurationTests
    {
        [Fact]
        public void Load_DefaultRulesOnlyTrue()
        {
            var config = new IssuesConfiguration();
            config.Load(@"Rules:
# Leave empty so the rule applies to any environment
- Environment:
  Search:
    MaxAllowed: 5
    Query: 'is:open is:issue label:bug'
");

            Assert.NotNull(config);
            Assert.NotNull(config.Rules);
            Assert.Single(config.Rules);
            Assert.Null(config.Rules[0].Environment);
            Assert.NotNull(config.Rules[0].Search);
            Assert.Equal(5, config.Rules[0].Search.MaxAllowed);
            Assert.Equal("is:open is:issue label:bug", config.Rules[0].Search.Query);
        }

        [Fact]
        public void Load_EnvironmentRulesOnlyTrue()
        {

            var config = new IssuesConfiguration();
            config.Load(@"Rules:
# Leave empty so the rule applies to any environment
- Environment: production
  Search:
    MaxAllowed: 5
    Query: 'is:open is:issue label:bug'
");

            Assert.NotNull(config);
            Assert.NotNull(config.Rules);
            Assert.Single(config.Rules);
            Assert.Equal("production", config.Rules[0].Environment);
            Assert.NotNull(config.Rules[0].Search);
            Assert.Equal(5, config.Rules[0].Search.MaxAllowed);
            Assert.Equal("is:open is:issue label:bug", config.Rules[0].Search.Query);
        }

        [Fact]
        public void Load_DefaultAndEnvironmentRulesTrue()
        {

            var config = new IssuesConfiguration();
            config.Load(@"Rules:
# Leave empty so the rule applies to any environment
- Environment:
  Search:
    MaxAllowed: 5
    Query: 'is:open is:issue label:bug'
- Environment: production
  Search:
    MaxAllowed: 1
    Query: 'is:open is:issue label:critical'
    OnlyCreatedBeforeWorkflowCreated: true
");

            Assert.NotNull(config);
            Assert.NotNull(config.Rules);
            Assert.Equal(2, config.Rules.Count);

            Assert.Null(config.Rules[0].Environment);
            Assert.NotNull(config.Rules[0].Search);
            Assert.Equal(5, config.Rules[0].Search.MaxAllowed);
            Assert.Equal("is:open is:issue label:bug", config.Rules[0].Search.Query);
            Assert.False(config.Rules[0].Search.OnlyCreatedBeforeWorkflowCreated);

            Assert.Equal("production", config.Rules[1].Environment);
            Assert.NotNull(config.Rules[1].Search);
            Assert.Equal(1, config.Rules[1].Search.MaxAllowed);
            Assert.Equal("is:open is:issue label:critical", config.Rules[1].Search.Query);
            Assert.True(config.Rules[1].Search.OnlyCreatedBeforeWorkflowCreated);
        }

        [Fact]
        public void Load_CompleteFile()
        {

            var config = new IssuesConfiguration();
            config.Load(@"Rules:
# Leave empty so the rule applies to any environment
- Environment:
  Search:
    MaxAllowed: 5
    Query: 'is:open is:issue label:bug'
    OnlyCreatedBeforeWorkflowCreated: false
- Environment: production
  Search:
    MaxAllowed: 1
    Query: 'is:open is:issue label:critical'
    OnlyCreatedBeforeWorkflowCreated: true
  Issues:
    MaxAllowed: 0
    State: 'OPEN'
    Repo: 'mona/lisa' # If omitted current repo will be used
    Assignee: 'octocat'
    Author: 'octocat'
    Mention: 'mona/security'
    # Skip Milestone parameter or * for any milestone, value for a specific milestone (NUMBER not label) 
    Milestone: '1' # Milestone number (not label)
    Labels:
      - BUG
      - show-stopper
    OnlyCreatedBeforeWorkflowCreated: true
");

            Assert.NotNull(config);
            Assert.NotNull(config.Rules);
            Assert.Equal(2, config.Rules.Count);

            Assert.Null(config.Rules[0].Environment);
            Assert.NotNull(config.Rules[0].Search);
            Assert.Equal(5, config.Rules[0].Search.MaxAllowed);
            Assert.Equal("is:open is:issue label:bug", config.Rules[0].Search.Query);
            Assert.False(config.Rules[0].Search.OnlyCreatedBeforeWorkflowCreated);

            Assert.Equal("production", config.Rules[1].Environment);
            Assert.NotNull(config.Rules[1].Search);
            Assert.Equal(1, config.Rules[1].Search.MaxAllowed);
            Assert.Equal("is:open is:issue label:critical", config.Rules[1].Search.Query);
            Assert.True(config.Rules[1].Search.OnlyCreatedBeforeWorkflowCreated);
            Assert.Equal(0, config.Rules[1].Issues.MaxAllowed);
            Assert.Equal("OPEN", config.Rules[1].Issues.State);
            Assert.Equal("mona/lisa", config.Rules[1].Issues.Repo);
            Assert.Equal("octocat", config.Rules[1].Issues.Assignee);
            Assert.Equal("octocat", config.Rules[1].Issues.Author);
            Assert.Equal("mona/security", config.Rules[1].Issues.Mention);
            Assert.Equal("1", config.Rules[1].Issues.Milestone);
            Assert.Equal(2, config.Rules[1].Issues.Labels.Count);
            Assert.Equal("BUG", config.Rules[1].Issues.Labels[0]);
            Assert.Equal("show-stopper", config.Rules[1].Issues.Labels[1]);
            Assert.True(config.Rules[1].Issues.OnlyCreatedBeforeWorkflowCreated);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRulesIsEmpty()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>()
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "Rules is mandatory" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRulesIsNull()
        {

            var config = new IssuesConfiguration
            {
                Rules = null
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "Rules is mandatory" }, errors);
        }

        [Fact]
        public void Validate_ReturnsNoErrors_WhenConfigurationIsValid()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Search = new IssueGateSearch
                        {
                            Query = "is:open",
                            MaxAllowed = 10
                        }
                    }
                }
            };


            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenIssuesMaxIsInvalid()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Issues = new ()
                        {
                            MaxAllowed = -1
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "MaxAllowed must be equal or greater than 0" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenIssuesRepoIsDefinedButEmpty()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Issues = new ()
                        {
                            Repo = " "
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "If Repo is specified it cannot be empty" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenIssuesRepoIsInvalid()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Issues = new ()
                        {
                            Repo = "test"
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "Repo must be in format owner/repository" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenIssuesRepoIsvalid()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Issues = new ()
                        {
                            Repo = "octocat/mona-lisa"
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenSearchMaxIsInvalid()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Search = new ()
                        {
                            MaxAllowed = -1,
                            Query = "dummy"
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "MaxAllowed must be equal or greater than 0" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenSearchIsInvalid_QueryEmpty()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Search = new IssueGateSearch
                        {
                            Query = "",
                            MaxAllowed = 0
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "Query must be specified" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenSearchIsInvalid_MessageEmpty()
        {

            var config = new IssuesConfiguration
            {
                Rules = new List<IssueGateRule>
                {
                    new IssueGateRule
                    {
                        Environment = "dev",
                        Search = new IssueGateSearch
                        {
                            Query = "is:public",
                            Message = " ",
                            MaxAllowed = 0
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.NotNull(errors);
            Assert.Equal(new List<string> { "When Message is specified it cannot be empty" }, errors);
        }
    }
}
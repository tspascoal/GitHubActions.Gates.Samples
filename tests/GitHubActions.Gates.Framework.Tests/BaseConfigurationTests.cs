using GitHubActions.Gates.Framework.Tests.Helpers;

namespace GitHubActions.Gates.Framework.Tests
{
    public class BaseConfigurationTests
    {
        [Fact]
        public void GetRule_EnvironmentCaseInsensitive()
        {

            var config = new ConfigurationHelper()
            {
                Rules = new List<RuleHelper>() {
                    new RuleHelper() {
                        Environment = "production"
                    },
                    new RuleHelper()
                    {
                    }
                }
            };

            var rule = config.GetRule("PRODUCTION");

            Assert.NotNull(rule);
            Assert.Equal("production", rule.Environment);
        }

        [Fact]
        public void GetRule_WithNoRules()
        {

            var config = new ConfigurationHelper()
            {
            };

            var rule = config.GetRule("PRODUCTION");

            Assert.Null(rule);
        }

        [Fact]
        public void GetRule_EnvironmentReturnDefaultRule()
        {

            var config = new ConfigurationHelper()
            {
                Rules = new List<RuleHelper>() {
                    new RuleHelper() {
                        Environment = "production"
                    },
                    new RuleHelper()
                    {
                    }
                }
            };

            var rule = config.GetRule("dummy");

            Assert.NotNull(rule);
            Assert.Null(rule.Environment);
        }

        [Fact]
        public void GetRule_NoRule()
        {

            var config = new ConfigurationHelper()
            {
                Rules = new List<RuleHelper>() {
                    new RuleHelper() {
                        Environment = "production"
                    }
                }
            };

            var rule = config.GetRule("dummy");

            Assert.Null(rule);
        }

        [Fact] 
        public void Validate_NoErrors()
        {
            var config = new ConfigurationHelper()
            {
                Rules = new List<RuleHelper>()
                {
                    new RuleHelper()
                    {
                        Environment = "production"
                    }
                }
            };
            var errors = config.Validate();
            Assert.Null(errors);
        }
    }
}
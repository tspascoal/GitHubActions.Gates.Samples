#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GitHubActions.Gates.Framework.Models;

namespace GitHubActions.Gates.Framework.Tests.Helpers
{
    public class ConfigurationHelper : BaseConfiguration<RuleHelper>
    {
        public override IList<string>? Validate()
        {
            if (Rules == null)
            {
                var errors = new List<string>
                {
                    "Rules Cannot Be Empty",
                    "Rules Is required"
                };

                return errors;
            }

            return null;
        }
    }

    public class RuleHelper : IGatesRule
    {
        public string Environment { get; set; }

        public int WaitMinutes => throw new NotImplementedException();
    }
}

using GitHubActions.Gates.Framework;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DeployHours.Gate.Tests")]
namespace DeployHours.Gate.Models
{

    public class DeployHoursConfiguration : BaseConfiguration<DeployHoursRule>
    {
        // Class to hold the configuration for this YAML

        //# Are we currently in a code freeze? This will reject any deployment
        //Lockout: false

        //# Valid Days
        //# "Monday","Tuesday", "Wednesday", "Thursday", "Friday", "Saturday","Sunday"
        //# Only needed if you want to override default value
        //# Default:
        //# DeployDays: ["Monday","Tuesday", "Wednesday", "Thursday", "Friday"]
        //DeployDays: ["Monday","Tuesday", "Wednesday", "Thursday", "Friday","Saturday"]

        //        Rules:
        //# Leave environment value empty so the rule applies to any environment (if no match for a specific environment is found)
        //- Environment:
        //  # Times are defined in UTC
        //  DeploySlots:
        //    - Start: 08:00
        //      End:  12:10
        //    - Start: 14:00
        //      End:  15:30
        //    - Start: 16:15
        //      End:  18:00

        /// <summary>
        /// Is the repo in lockout mode? All deploys will be reject if lockout is true
        /// </summary>
        public bool Lockout { get; internal set; }

        /// <summary>
        /// List of days that are considered deploy days. Default is Monday to Friday
        /// </summary>
        public DayOfWeek[] DeployDays { get; internal set; }

        public DeployHoursConfiguration()
        {
            DeployDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        }

        public override List<string> Validate()
        {
            var validatorErrors = new List<string>();

            if(DeployDays?.Length == 0)
            {
                validatorErrors.Add("If DeployDays is defined it cannot be empty");
            }

            if(Rules == null || Rules.Count == 0)
            {
                validatorErrors.Add("Rules is mandatory");

                return validatorErrors;
            }

            foreach (var rule in Rules)
            {
                if (rule.DeploySlots == null || rule.DeploySlots.Count == 0)
                {
                    validatorErrors.Add($"DeployHours element is mandatory (environment: {rule.Environment ?? "ANY"})");
                }
                else
                {
                    foreach (var range in rule.DeploySlots)
                    {
                        validatorErrors.AddRange(range.Validate());
                    }
                }
            }

            if (validatorErrors.Count > 0)
                return validatorErrors;

            return new List<string>();
        }
    }
}

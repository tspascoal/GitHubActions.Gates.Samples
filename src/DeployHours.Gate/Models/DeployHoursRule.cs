using GitHubActions.Gates.Framework.Models;
using System.Collections.Generic;

namespace DeployHours.Gate.Models
{
    public class DeployHoursRule : IGatesRule
    {
        public string Environment { get; set; }
        public IList<DeploySlotRange> DeploySlots { get; internal set; }

        public int WaitMinutes { get; set; }
    }
}

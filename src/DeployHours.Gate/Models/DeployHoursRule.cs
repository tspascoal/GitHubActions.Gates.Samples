using GitHubActions.Gates.Framework.Models;
using System.Collections.Generic;

namespace DeployHours.Gate.Models
{
    public class DeployHoursRule : IGatesRule
    {
        public string Environment { get; set; } = string.Empty;
        public IList<DeploySlotRange> DeploySlots { get; internal set; } = new List<DeploySlotRange>();
        public int WaitMinutes { get; set; }
    }
}

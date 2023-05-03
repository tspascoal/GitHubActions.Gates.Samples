using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Models
{
    public interface IGatesRule
    {
        public string Environment { get; }
        public int WaitMinutes { get; }
    }
}

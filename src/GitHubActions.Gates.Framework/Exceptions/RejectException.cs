using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Exceptions
{
    public class RejectException : Exception
    {
        public RejectException(string message) : base(message)
        {
        }
    }
}

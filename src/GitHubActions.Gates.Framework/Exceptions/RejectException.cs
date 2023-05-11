using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Exceptions
{
    [Serializable]
    public class RejectException : Exception
    {
        public RejectException(string message) : base(message)
        {
        }
        protected RejectException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}

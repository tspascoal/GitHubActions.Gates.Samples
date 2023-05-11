using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Exceptions
{
    [Serializable]
    public class GraphQLException : Exception
    {
        public IList<string> Errors { get; private set; }
        public GraphQLException(string message, IList<string> errors) : base(message) 
        {
            Errors = errors;
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected GraphQLException(SerializationInfo info, StreamingContext context) : base(info, context)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Exceptions
{
    public class GraphQLException : Exception
    {
        public IList<string> Errors { get; private set; }
        public GraphQLException(string message, IList<string> errors) : base(message) 
        {
            Errors = errors;
        }
    }
}

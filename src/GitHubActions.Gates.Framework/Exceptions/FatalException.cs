﻿using System.Runtime.Serialization;

namespace GitHubActions.Gates.Framework.Exceptions
{
    [Serializable]
    public class FatalException : Exception
    {
        public FatalException(string message) : base(message)
        {
        }
        protected FatalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

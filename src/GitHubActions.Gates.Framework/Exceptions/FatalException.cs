namespace GitHubActions.Gates.Framework.Exceptions
{
    public class FatalException : Exception
    {
        public FatalException(string message) : base(message)
        {
        }
    }
}

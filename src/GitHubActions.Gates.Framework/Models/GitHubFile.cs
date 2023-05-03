#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace GitHubActions.Gates.Framework.Models
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class GitHubFile
    {
        public int Size { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
        public string Sha { get; set; }
        public string HtmlUrl { get; set; }
    }
}

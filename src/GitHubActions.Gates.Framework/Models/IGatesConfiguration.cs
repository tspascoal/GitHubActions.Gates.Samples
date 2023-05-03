using GitHubActions.Gates.Framework.Models;
using YamlDotNet.Serialization;

namespace GitHubActions.Gates.Framework
{
    public interface IGatesConfiguration<R> 
    {
        IList<string>? Validate();
        void Load(string yaml);
        string GenerateMarkdownErrorList(IList<string>? errors);

        R? GetRule(string Environment);
    }
}
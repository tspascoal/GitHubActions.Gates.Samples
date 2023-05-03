using GitHubActions.Gates.Framework.Models;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;

namespace GitHubActions.Gates.Framework
{
    public class BaseConfiguration<T> : IGatesConfiguration<T> where T : IGatesRule, new()
    {
        public int Version { get; set; }
        public List<T>? Rules { get; set; }

        public T? GetRule(string Environment)
        {
            if (Rules == null)
            {
                return default;
            }
            var rule = Rules.FirstOrDefault(r => r.Environment?.ToLowerInvariant() == Environment.ToLowerInvariant());

            // If rule not found, try to find the default rule
            rule ??= Rules.FirstOrDefault(r => String.IsNullOrEmpty(r.Environment));

            return rule;
        }
        public virtual IList<string>? Validate()
        {
            return default;
        }

        public string GenerateMarkdownErrorList(IList<string>? validationErrrors)
        {

            if (validationErrrors == null || validationErrrors.Count == 0)
                return "";

            StringBuilder markdown = new();
            foreach (var error in validationErrrors)
            {
                markdown.Append($"- {error}\n");
            }
            markdown.Append('\n');

            return markdown.ToString();
        }

        public void Load(string yaml)
        {
            var config = new Deserializer().Deserialize(yaml, this.GetType());
            CopyFrom(config!);
        }
        internal void CopyFrom(object obj)
        {
            if (obj == null)
            {
                return;
            }

            var type = obj.GetType();

            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .ToList()
                .ForEach(p => p.SetValue(this, p.GetValue(obj)));

            type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .ToList()
                .ForEach(f => f.SetValue(this, f.GetValue(obj)));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Models
{
    public class Repo
    {
        public string FullName { get; }
        public string Owner { get; }
        public string Name { get; }

        public Repo(string fullname) {
            FullName = fullname;
            var parts = fullname.Split('/');
            Owner = parts[0];
            Name = parts[1];
        }
    }
}

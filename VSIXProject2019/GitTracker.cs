using GitScc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXProject2019
{
    public delegate void ChangedEventHandler(GitTracker tracker);

    public class GitTracker
    {
        public string Directory { get; }
        public GitRepository Repository { get; }

        public event ChangedEventHandler Changed;

        public GitTracker(string directory)
        {
            this.Directory = directory;
            this.Repository = new GitRepository(directory);
        }



    }
}

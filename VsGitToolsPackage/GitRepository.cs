using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GitScc;
using System.Windows.Threading;

namespace F1SYS.VsGitToolsPackage
{
	public class GitRepository
	{
		private string workingDirectory;
		public string WorkingDirectory { get { return workingDirectory; } }

        private bool isGit;
        public bool IsGit { get { return isGit; } }

        public GitRepository(string directory)
		{
            this.workingDirectory = directory;
            var output = GitRun("rev-parse --is-inside-work-tree").Trim();
            isGit = string.Compare("true", output, true) == 0;
		}

		#region Git commands

        private string GitRun(string cmd)
        {
            if (!GitBash.Exists) throw new Exception("git.exe is not found.");
            var result = GitBash.Run(cmd, this.WorkingDirectory);
            return result.Output;
        }

        private void GitRunCmd(string cmd)
        {
            if (!GitBash.Exists) throw new Exception("git.exe is not found.");
            GitBash.RunCmd(cmd, this.WorkingDirectory);
        }

		internal string AddTag(string name, string id)
		{
			return GitRun(string.Format("tag \"{0}\" {1}", name, id));
		}

		internal string GetTagId(string name)
		{
			return GitRun("show-ref refs/tags/" + name);
		}

		internal string DeleteTag(string name)
		{
			return GitRun("tag -d " + name);
		}

		internal string AddBranch(string name, string id)
		{
			return GitRun(string.Format("branch \"{0}\" {1}", name, id));
		}

		internal string GetBranchId(string name)
		{
			return GitRun("show-ref refs/heads/" + name);
		}

		internal string DeleteBranch(string name)
		{
			return GitRun("branch -d " + name);
		}

		internal string CheckoutBranch(string name)
		{
			return GitRun("checkout " + name);
		}

		internal string Archive(string id, string fileName)
		{
			return GitRun(string.Format("archive {0} --format=zip --output \"{1}\"", id, fileName));
		}

		internal void Patch(string id1, string fileName)
		{
			GitRunCmd(string.Format("format-patch {0} -1 --stdout > \"{1}\"", id1, fileName));
		}

		internal void Patch(string id1, string id2, string fileName)
		{
			GitRunCmd(string.Format("format-patch {0}..{1} -o \"{2}\"", id1, id2, fileName));
		}

		#endregion    
	
        internal void InitRepo()
        {
            if (!isGit)
            {
                GitRun("init");
                var ignoreFileName = Path.Combine(WorkingDirectory, ".gitignore");
                if (!File.Exists(ignoreFileName))
                {
                    File.WriteAllText(ignoreFileName, Resources.IgnoreFileContent);
                }
            }
        }
    }
}

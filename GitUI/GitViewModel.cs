using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using GitScc;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GitUI
{
	public class GitViewModel
	{
		#region singleton
		private static GitViewModel current;
		public static GitViewModel Current
		{
			get
			{
				if (current == null)
				{
					var args = Environment.GetCommandLineArgs();
					current = new GitViewModel();
					var directory = args.Length > 1 ? args[1] :	Environment.CurrentDirectory;
					current.Open(directory);
				}

				return current;
			}
		}

		#endregion

		public event EventHandler GraphChanged = delegate { };
		private GitFileStatusTracker tracker;
		private string workingDirectory;

		public GitFileStatusTracker Tracker { get { return tracker; } }
		public string WorkingDirectory { get { return workingDirectory; } }
		public bool ShowSimplifiedView
		{
            get { return tracker.RepositoryGraph.IsSimplified; }
			set
			{
                tracker.RepositoryGraph.IsSimplified = value;
                GraphChanged(this, null); 
			}
		}

		FileSystemWatcher fileSystemWatcher;

		private GitViewModel()
		{
            GitBash.GitExePath = GitSccOptions.Current.GitBashPath;
            GitBash.UseUTF8FileNames = !GitSccOptions.Current.NotUseUTF8FileNames;

            if (!GitBash.Exists) GitBash.GitExePath = TryFindFile(new string[] {
					@"C:\Program Files\Git\bin\sh.exe",
					@"C:\Program Files (x86)\Git\bin\sh.exe",
			});

		}

        private string TryFindFile(string[] paths)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

		internal void Open(string directory)
		{
			workingDirectory = directory;

			tracker = new GitFileStatusTracker(directory);
			if (tracker.IsGit) directory = tracker.WorkingDirectory;

			if (Directory.Exists(directory))
			{
                if (fileSystemWatcher != null) { fileSystemWatcher.Dispose();  }

                fileSystemWatcher = new FileSystemWatcher(directory);
				fileSystemWatcher.IncludeSubdirectories = true;
				//fileSystemWatcher.Created += new FileSystemEventHandler(fileSystemWatcher_Changed);
				fileSystemWatcher.Deleted += new FileSystemEventHandler(fileSystemWatcher_Changed);
				//fileSystemWatcher.Renamed += new FileSystemEventHandler(fileSystemWatcher_Changed);
				fileSystemWatcher.Changed += new FileSystemEventHandler(fileSystemWatcher_Changed);
				fileSystemWatcher.EnableRaisingEvents = true;
			}

            GraphChanged(this, null);
		}

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var name = Path.GetFullPath(e.FullPath);
            if (!(name.EndsWith(".git") && e.ChangeType == WatcherChangeTypes.Changed)
                && !name.EndsWith("index.lock") && !this.tracker.IsIgnored(name))
            {
                NeedRefresh = true;
            }
        }

		internal static void OpenGitBash()
		{
			GitBash.OpenGitBash(Current.WorkingDirectory);
		}

        #region Refresh

        internal bool needRefresh, noRefresh;

        internal bool NeedRefresh
        {
            get { return needRefresh; }
            set
            {
                needRefresh = !noRefresh && value;
                if (needRefresh) Refresh();
            }
        }

        internal bool NoRefresh
        {
            get { return noRefresh; }
            set
            {
                noRefresh = value;
                nextTimeRefresh = DateTime.Now.AddMilliseconds(600);
            }
        }

        private DateTime nextTimeRefresh = DateTime.Now;

        private void Refresh()
        {
            if (NeedRefresh && !NoRefresh)
            {
                double delta = DateTime.Now.Subtract(nextTimeRefresh).TotalMilliseconds;
                if (delta > 200)
                {
                    NoRefresh = true;
                    NeedRefresh = false;
                    RefreshToolWindows();
                }
            }
        }



        internal async Task RefreshToolWindows()
        {
            await Task.Delay(300);

            await Task.Run(() =>
            {
                this.tracker.Refresh();

                // cache branch and changed files
                if (this.tracker != null && this.tracker.IsGit)
                {
                    var b = tracker.CurrentBranch;
                    var c = tracker.ChangedFiles;
                }

                GraphChanged(this, null);
                NoRefresh = false;

            });
        }

        #endregion

		#region Git commands

		//internal GitUI.UI.GitConsole console = null;

		private GitBashResult GitRun(string cmd)
		{
			if (!GitBash.Exists) throw new GitException("git.exe is not found.");
			if (this.Tracker == null) throw new GitException("Git repository is not found.");
            var ret = new GitBashResult { HasError = true };
            try
            {
                ret = GitBash.Run(cmd, this.Tracker.WorkingDirectory);
                HistoryViewCommands.ShowMessage.Execute(new { GitBashResult = ret }, null);
            }
            catch (Exception ex)
            {
                ret.Error = ex.Message;
                HistoryViewCommands.ShowMessage.Execute(new { GitBashResult = ret }, null);
            }
            return ret;
		}

		private void GitRunCmd(string cmd)
		{
			if (!GitBash.Exists) throw new Exception("git.exe is not found.");
			GitBash.RunCmd(cmd, this.Tracker.WorkingDirectory);
		}

        internal GitBashResult AddTag(string name, string id)
		{
            return GitRun(string.Format("tag \"{0}\" {1}", name, id));
		}

		internal GitBashResult GetTagId(string name)
		{
			return GitRun("show-ref refs/tags/" + name);
		}

        internal GitBashResult DeleteTag(string name)
		{
            return GitRun("tag -d " + name);
		}

        internal GitBashResult AddBranch(string name, string id)
		{
            return GitRun(string.Format("branch \"{0}\" {1}", name, id));
		}

		internal GitBashResult GetBranchId(string name)
		{
			return GitRun("show-ref refs/heads/" + name);
		}

        internal GitBashResult DeleteBranch(string name)
		{
            return GitRun("branch -D " + name);
		}

		internal void CheckoutBranch(string name)
		{
            GitRun("checkout " + name);
		}

		internal void Archive(string id, string fileName)
		{
			GitRun(string.Format("archive {0} --format=zip --output \"{1}\"", id, fileName));
		}

		internal void Patch(string id1, string fileName)
		{
			GitRunCmd(string.Format("format-patch {0} -1 --stdout > \"{1}\"", id1, fileName));
		}

		internal void Patch(string id1, string id2, string fileName)
		{
			GitRunCmd(string.Format("format-patch {0}..{1} -o \"{2}\"", id1, id2, fileName));
		}

        internal void CherryPick(string id)
        {
            GitRun(string.Format("cherry-pick {0}", id));
        }

        internal void Init()
        {
            GitRun("init");
        }

        internal void OpenIgnoreFile()
        {
            Tracker.EditIngoreFile();
        }


        internal void Stash()
        {
            GitRun("stash");
        }

        internal void StashPop()
        {
            GitRun("stash pop");
        }

        internal void StashDrop()
        {
            GitRun("stash drop");
        }

        internal void UndoCommit()
        {
            GitRun("reset HEAD~1 --soft");
        }

        //internal void MergeTool()
        //{
        //    GitRunCmd("mergetool");
        //}

        internal void Rebase(string branchName)
        {
            GitRun("rebase " + branchName);
        }

        internal void RebaseI(string id)
        {
            GitRun("rebase -i " + id + "~1");
        }

        internal void Merge(string branchName)
        {
            GitRun("merge " + branchName);
        }
        #endregion

    }
}

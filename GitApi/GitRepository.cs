using GitScc.DataServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Threading;

namespace GitScc
{
	public class GitRepository
	{
		private string workingDirectory;
        private bool isGit;
        private string branch;
        private IEnumerable<GitFile> changedFiles;
        private IEnumerable<string> remotes;
        private IDictionary<string, string> configs;
        private IEnumerable<GitFile> ignored;


		public string WorkingDirectory { get { return workingDirectory; } }
        public bool IsGit { get { return isGit; } }

        public GitRepository(string directory)
		{
            this.workingDirectory = directory;
            Refresh();
            this.isGit = false;
            var result = GitBash.Run("rev-parse --show-toplevel", WorkingDirectory); 
            if (!result.HasError && !result.Output.StartsWith("fatal:"))
            {
                this.workingDirectory = result.Output.Trim();
                result = GitBash.Run("rev-parse --is-inside-work-tree", WorkingDirectory);
                isGit = string.Compare("true", result.Output.Trim(), true) == 0;
            }
		}

        public void Refresh()
        {
            this.repositoryGraph = null;
            this.changedFiles = null;
            this.branch = null;
            this.remotes = null;
            this.configs = null;
            this.ignored = null;
        }

		#region Git commands

        private string GitRun(string cmd)
        {
            if (!this.IsGit) return null;
            var result = GitBash.Run(cmd, this.WorkingDirectory);
            if (result.HasError) throw new GitException(result.Error);
            if (result.Output.StartsWith("fatal:")) throw new GitException(result.Output);
            return result.Output;
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
            string id = null;
            var result = GitBash.Run("rev-parse " + name, this.WorkingDirectory);
            if (!result.HasError && !result.Output.StartsWith("fatal:"))
            {
                id = result.Output.Trim();
            }
            return id;
        }

		internal string DeleteBranch(string name)
		{
			return GitRun("branch -d " + name);
		}

        public void CheckOutBranch(string branch, bool createNew = false)
        {
            this.branch = null;
            GitRun(string.Format("checkout {0} {1}", (createNew ? "-b" : ""), branch));
        }

		internal string Archive(string id, string fileName)
		{
			return GitRun(string.Format("archive {0} --format=zip --output \"{1}\"", id, fileName));
		}

		internal void Patch(string id1, string fileName)
		{
			GitRun(string.Format("format-patch {0} -1 --stdout > \"{1}\"", id1, fileName));
		}

		internal void Patch(string id1, string id2, string fileName)
		{
			GitRun(string.Format("format-patch {0}..{1} -o \"{2}\"", id1, id2, fileName));
		}

		#endregion    
	
        public static void Init(string folderName)
        {
            GitBash.Run("init", folderName);
            GitBash.Run("config core.ignorecase true", folderName);
        }

        public bool IsBinaryFile(string fileName)
        {
            FileStream fs = File.OpenRead(fileName);
            try
            {
                int len = Convert.ToInt32(fs.Length);
                if (len > 1000) len = 1000;
                byte[] bytes = new byte[len];
                fs.Read(bytes, 0, len);
                for (int i = 0; i < len - 1; i++)
                {
                    if (bytes[i] == 0) return true;
                }
                return false;
            }
            finally
            {
                fs.Close();
            }
        }

        public string DiffFileAdv(string fileName, bool diffIndex = false)
        {
            var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), diffIndex ? ".cached.diff" : ".diff");
            try
            {
                if (diffIndex)
                {
                    GitBash.RunCmd(string.Format("diff --cached -- \"{0}\" > \"{1}\"", fileName, tmpFileName), WorkingDirectory);
                }
                else
                {
                    GitBash.RunCmd(string.Format("diff -- \"{0}\" > \"{1}\"", fileName, tmpFileName), WorkingDirectory);
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(tmpFileName, ex.Message);
            }
            return tmpFileName;
        }

        public string DiffFile(string fileName)
        {
            var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
            try
            {
                GitBash.RunCmd(string.Format("diff HEAD -- \"{0}\" > \"{1}\"", fileName, tmpFileName), WorkingDirectory);
            }
            catch (Exception ex)
            {
                File.WriteAllText(tmpFileName, ex.Message);
            }
            return tmpFileName;
        }

        public string ChangedFilesStatus
        {
            get
            {
                var changed = ChangedFiles;
                return string.Format(this.CurrentBranch + " +{0} ~{1} -{2} !{3}",
                    changed.Where(f => f.Status == GitFileStatus.New || f.Status == GitFileStatus.Added).Count(),
                    changed.Where(f => f.Status == GitFileStatus.Modified || f.Status == GitFileStatus.Staged).Count(),
                    changed.Where(f => f.Status == GitFileStatus.Deleted || f.Status == GitFileStatus.Removed).Count(),
                    changed.Where(f => f.Status == GitFileStatus.Conflict).Count());
            }
        }

        public IEnumerable<GitFile> ChangedFiles
        {
            get
            {
                if (changedFiles == null)
                {
                    try
                    {
                        var result = GitBash.Run("status --porcelain -z --untracked-files", WorkingDirectory);
                        changedFiles = ParseGitStatus(result.Output);
                    }
                    catch
                    {
                        changedFiles = new GitFile[] { };
                    }
                }
                return changedFiles;
            }
        }

        public IEnumerable<GitFile> Ignored
        {
            get
            {
                if (ignored == null)
                {
                    try
                    {
                        var result = GitBash.Run("status --porcelain -z --ignored", WorkingDirectory);
                        ignored = ParseGitStatus(result.Output).Where(f => f.Status == GitFileStatus.Ignored);
                    }
                    catch
                    {
                        ignored = new GitFile[] { };
                    }
                }
                return ignored;
            }
        }

        #region copied and modified from git extensions
        public IList<GitFile> ParseGitStatus(string statusString)
        {
            //Debug.WriteLine(statusString);

            var list = new List<GitFile>();
            if (string.IsNullOrEmpty(statusString)) return list;

            // trim warning messages
            var nl = new char[] { '\n', '\r' };
            string trimmedStatus = statusString.Trim(nl);
            int lastNewLinePos = trimmedStatus.LastIndexOfAny(nl);
            if (lastNewLinePos > 0)
            {
                int ind = trimmedStatus.LastIndexOf('\0');
                if (ind < lastNewLinePos) //Warning at end
                {
                    lastNewLinePos = trimmedStatus.IndexOfAny(nl, ind >= 0 ? ind : 0);
                    trimmedStatus = trimmedStatus.Substring(0, lastNewLinePos).Trim(nl);
                }
                else                                              //Warning at beginning
                    trimmedStatus = trimmedStatus.Substring(lastNewLinePos).Trim(nl);
            }


            //Split all files on '\0' (WE NEED ALL COMMANDS TO BE RUN WITH -z! THIS IS ALSO IMPORTANT FOR ENCODING ISSUES!)
            var files = trimmedStatus.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            for (int n = 0; n < files.Length; n++)
            {
                if (string.IsNullOrEmpty(files[n]))
                    continue;

                int splitIndex = files[n].IndexOfAny(new char[] { '\0', '\t', ' ' }, 1);

                string status = string.Empty;
                string fileName = string.Empty;

                if (splitIndex < 0)
                {
                    //status = files[n];
                    //fileName = files[n + 1];
                    //n++;
                    continue;
                }
                else
                {
                    status = files[n].Substring(0, splitIndex);
                    fileName = files[n].Substring(splitIndex);
                }

                //X shows the status of the index, and Y shows the status of the work tree

                char x = status[0];
                char y = status.Length > 1 ? status[1] : ' ';

                var gitFile = new GitFile { FileName = fileName.Trim(), X = x, Y = y  };

                switch (x)
                {
                    case '?':
                        gitFile.Status = GitFileStatus.New;
                        break;
                    case '!':
                        gitFile.Status = GitFileStatus.Ignored;
                        break;
                    case ' ':
                        if (y == 'M') gitFile.Status = GitFileStatus.Modified;
                        else if (y == 'D') gitFile.Status = GitFileStatus.Deleted;
                        break;
                    case 'M':
                        if (y == 'M') gitFile.Status = GitFileStatus.Modified;
                        else gitFile.Status = GitFileStatus.Staged;
                        break;
                    case 'A':
                        gitFile.Status = GitFileStatus.Added;
                        break;
                    case 'D':
                        gitFile.Status = GitFileStatus.Removed;
                        break;
                    case 'R':
                        gitFile.Status = GitFileStatus.Renamed;
                        break;
                    case 'C':
                        gitFile.Status = GitFileStatus.Copied;
                        break;

                    case 'U':
                        gitFile.Status = GitFileStatus.Conflict;
                        break;
                }
                list.Add(gitFile);
            }
            return list;
        }

        #endregion

        #region repository status: branch, in the middle of xxx
        public string CurrentBranch
        {
            get
            {
                if (branch == null)
                {
                    branch = "master";
                    var result = GitBash.Run("rev-parse --abbrev-ref HEAD", this.WorkingDirectory);
                    if (!result.HasError && !result.Output.StartsWith("fatal:"))
                    {
                        branch = result.Output.Trim();
                        if (IsInTheMiddleOfBisect) branch += " | BISECTING";
                        if (IsInTheMiddleOfMerge) branch += " | MERGING";
                        if (IsInTheMiddleOfPatch) branch += " | AM";
                        if (IsInTheMiddleOfRebase) branch += " | REBASE";
                        if (IsInTheMiddleOfRebaseI) branch += " | REBASE-i";
                        if (IsInTheMiddleOfCherryPick) branch += " | CHERRY-PIKCING";
                    }
                }
                return branch;
            }
        }

        public bool IsInTheMiddleOfBisect
        {
            get
            {
                return this.IsGit && FileExistsInGit("BISECT_START");
            }
        }

        public bool IsInTheMiddleOfMerge
        {
            get
            {
                return this.IsGit && FileExistsInGit("MERGE_HEAD");
            }
        }

        public bool IsInTheMiddleOfPatch
        {
            get
            {
                return this.IsGit && FileExistsInGit("rebase-*", "applying");
            }
        }

        public bool IsInTheMiddleOfRebase
        {
            get
            {
                return this.IsGit && FileExistsInGit("rebase-*", "rebasing");
            }
        }

        public bool IsInTheMiddleOfRebaseI
        {
            get
            {
                return this.IsGit && FileExistsInGit("rebase-*", "git-rebase-todo");
            }
        }

        private bool FileExistsInGit(string fileName)
        {
            return this.IsGit && File.Exists(Path.Combine(GitDirectory, fileName));
        }

        public bool IsInTheMiddleOfCherryPick
        {
            get
            {
                return this.IsGit && FileExistsInGit("CHERRY_PICK_HEAD");
            }
        }

        private string GitDirectory
        {
            get
            {
                return Path.Combine(WorkingDirectory, ".git");
            }
        }

        private bool FileExistsInRepo(string fileName)
        {
            return File.Exists(Path.Combine(WorkingDirectory, fileName));
        }

        private bool FileExistsInGit(string directory, string fileName)
        {
            if (Directory.Exists(GitDirectory))
            {
                foreach (var dir in Directory.GetDirectories(GitDirectory, directory))
                {
                    if (File.Exists(Path.Combine(dir, fileName))) return true;
                }
            }
            return false;
        }

        #endregion

        public string LastCommitMessage
        {
            get
            {
                try
                {
                    return GitRun("log -1 --format=%s\r\n\r\n%b").Trim();
                }
                catch
                {
                    return "";
                }
            }
        }

        public GitFileStatus GetFileStatus(string fileName)
        {
            var file = ChangedFiles.Where(f => string.Compare(f.FileName, fileName, true) == 0).FirstOrDefault();
            if (file != null) return file.Status;
            if (FileExistsInRepo(fileName)) return GitFileStatus.Tracked;
            // did not check if the file is ignored for performance reason
            return GitFileStatus.NotControlled;
        }

        public void StageFile(string fileName)
        {
            if (FileExistsInRepo(fileName))
            {
                GitRun(string.Format("add \"{0}\"", fileName));
            }
            else
            {
                GitRun(string.Format("rm --cached -- \"{0}\"", fileName));
            }
        }

        public void UnStageFile(string fileName)
        {
            var head = GetBranchId("HEAD");

            if (head == null)
            {
                GitRun(string.Format("rm --cached -- \"{0}\"", fileName));
            }
            else
            {
                GitRun(string.Format("reset -- \"{0}\"", fileName));
            }
        }

        public void AddIgnoreItem(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            fileName = fileName.Replace("\\", "/");

            var ignoreFile = Path.Combine(WorkingDirectory, ".gitignore");
            if (!File.Exists(ignoreFile))
            {
                using (StreamWriter sw = File.CreateText(ignoreFile))
                {
                    sw.WriteLine(fileName);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(ignoreFile))
                {
                    sw.WriteLine();
                    sw.Write(fileName);
                }
            }
        }

        public bool IsIgnored(string fullPath)
        {
            foreach(var item in this.Ignored)
            {
                var name = Path.GetFullPath(Path.Combine(WorkingDirectory, item.FileName));
                if (Directory.Exists(name) && fullPath.StartsWith(name)) return true;
                if (string.Compare(fullPath, name, true) == 0) return true;
            }
            return false;
        }

        public void CheckOutFile(string fileName)
        {
            GitRun(string.Format("checkout -- \"{0}\"", fileName));
        }

        public string Commit(string message, bool amend = false, bool signoff = false)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Commit message must not be null or empty!", "message");
            }

            var msgFile = Path.Combine(WorkingDirectory, "COMMITMESSAGE");
            File.WriteAllText(msgFile, message);
            try
            {
                string opt = "";
                if (amend) opt += "--amend ";
                if (signoff) opt += "--signoff ";
                return GitRun(string.Format("commit -F \"{0}\" {1}", msgFile, opt));
            }
            finally
            {
                File.Delete(msgFile);
            }
        }

        public bool CurrentCommitHasRefs()
        {
            var head = GetBranchId("HEAD");
            if (head == null) return false;
            var result = GitBash.Run("show-ref --head --dereference", WorkingDirectory);
            if (!result.HasError && !result.Output.StartsWith("fatal:"))
            {
                var refs = result.Output.Split('\n')
                          .Where(t => t.IndexOf(head) >= 0);
                return refs.Count() > 2;
            }
            return false;
        }

        public string DiffFile(string fileName, string commitId1, string commitId2)
        {
            if (!this.IsGit) return "";

            var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
            var fileNameRel = fileName;
            GitBash.RunCmd(string.Format("diff {2} {3} -- \"{0}\" > \"{1}\"", fileNameRel, tmpFileName, commitId1, commitId2), WorkingDirectory);
            return tmpFileName;
        }


        public string Blame(string fileName)
        {
            if (!this.IsGit) return "";

            var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), ".blame");
            var fileNameRel = fileName;
            GitBash.RunCmd(string.Format("blame -M -w -- \"{0}\" > \"{1}\"", fileNameRel, tmpFileName), WorkingDirectory);
            return tmpFileName;

        }

        public IEnumerable<string> GetCommitsForFile(string fileName)
        {
            if (!this.IsGit) return new string[0];

            var fileNameRel = fileName;

            var result = GitBash.Run(string.Format("log -z --ignore-space-change --pretty=format:%H -- \"{0}\"", fileNameRel), WorkingDirectory);
            if (!result.HasError)
                return result.Output.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            return new string[0];
        }


        public void EditIngoreFile()
        {
            var ignoreFile = Path.Combine(WorkingDirectory, ".gitignore");

            var ret = GitBash.Run("config core.editor", WorkingDirectory);
            if (!ret.HasError && ret.Output.Trim() != "")
            {
                var editor = ret.Output.Trim();
                if (editor.Length == 0) editor = "notepad.exe";
                var cmd = string.Format("{0} \"{1}\"", editor, ignoreFile);
                cmd = cmd.Replace("/", "\\");
                var pinfo = new ProcessStartInfo("cmd.exe")
                {
                    Arguments = "/C \"" + cmd + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = this.WorkingDirectory,
                };
                Process.Start(pinfo);
            }
        }

        private RepositoryGraph repositoryGraph;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public RepositoryGraph RepositoryGraph
        {
            get
            {
                if (repositoryGraph == null)
                {
                    repositoryGraph = IsGit ? new RepositoryGraph(this.WorkingDirectory) : null;
                }
                return repositoryGraph;
            }
        }

        public void SaveFileFromLastCommit(string fileName, string tempFile)
        {
            if (!this.isGit) return;
            var head = GetBranchId("HEAD");
            if (head != null)
            {
                GitBash.RunCmd(string.Format("show \"HEAD:{0}\" > \"{1}\"", fileName, tempFile), this.WorkingDirectory);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IEnumerable<string> Remotes
        {
            get
            {
                if (remotes == null)
                {
                    var result = GitBash.Run("remote", this.WorkingDirectory);
                    if (!result.HasError)
                        remotes = result.Output.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s));
                }
                return remotes;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IDictionary<string, string> Configs
        {
            get
            {
                if (configs == null)
                {
                    var result = GitBash.Run("config -l", this.WorkingDirectory);
                    if (!result.HasError)
                    {
                        var lines = result.Output.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s) && s.IndexOf("=") > 0).OrderBy(s => s);

                        configs = new Dictionary<string, string>();
                        foreach (var s in lines)
                        {
                            var pos = s.IndexOf("=");
                            var key = s.Substring(0, pos);
                            if (!configs.Keys.Contains(key))
                                configs.Add(key, s.Substring(pos + 1));
                        }
                    }
                }
                return configs ?? new Dictionary<string, string>();
            }
        }

        private string FixEOL(string line)
        {
            line = line.TrimEnd();
            if (line.Length == 0) line = " ";
            return line + "\n";
        }

        public bool HasChanges(string[] diffLines, int startLine, int endLine)
        {
            var difftool = new DiffTool();
            var hunks = difftool.GetHunks(diffLines, startLine, endLine);
            return hunks.Count() > 0;
        }

        public void Apply(string[] diffLines, int startLine, int endLine, bool cached, bool reverse)
        {
            var difftool = new DiffTool();
            var hunks = difftool.GetHunks(diffLines, startLine, endLine, reverse);
            if (hunks.Count() <=0) throw new Exception("No change selected");

            var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
            using (var file = new StreamWriter(tmpFileName, false, Encoding.UTF8))
            {
                for (int i = 0; i < 4; i++)
                {
                    // Skip the line: index xxxx..xxxx
                    if (i != 1)
                    {
                        var line = diffLines[i];
                        file.Write(FixEOL(line));
                    }
                }
                foreach (var hunk in hunks)
                {
                    var heading = $"@@ -{hunk.OldBlock[0]},{hunk.OldBlock[1]} +{hunk.NewBlock[0]},{hunk.NewBlock[1]} @@{hunk.Heading}";
                    file.Write(FixEOL(heading));
                    foreach(var line in hunk.Lines)
                    {
                        file.Write(FixEOL(line));
                    }
                }
            }
            var cmd = "apply --ignore-whitespace";
            if (cached) cmd += " --cached";
            var result = GitBash.Run($"{cmd} \"{tmpFileName}\"", WorkingDirectory);
            File.Delete(tmpFileName);
            if (result.HasError) throw new GitException(result.Error);
        }

        public void SetConfig(string name, string value)
        {
            GitBash.Run($"config {name} {value}", WorkingDirectory);
        }

        public void RemveConfig(string name)
        {
            GitBash.Run($"config --unset {name}", WorkingDirectory);
        }

        public string GetConfig(string name)
        {
            var result = GitBash.Run($"config --get {name}", WorkingDirectory);
            return result.Output.Trim();
        }

        public string GetCommitTemplate()
        {
            var fileName = GetConfig("commit.template");
            if (!string.IsNullOrWhiteSpace(fileName))
            {

                if (fileName.StartsWith("~"))
                {
                    fileName = fileName.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                }
                else
                {
                    if (!File.Exists(fileName)) fileName = Path.Combine(WorkingDirectory, fileName);
                }

                if (File.Exists(fileName)) return File.ReadAllText(fileName);
            }
            return "";
        }
    }

    public class GitFileStatusTracker: GitRepository
    {
        public GitFileStatusTracker(string directory) : base(directory) { }
    }
}

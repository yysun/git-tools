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
        private bool isGit;

		public string WorkingDirectory { get { return workingDirectory; } }
        public bool IsGit { get { return isGit; } }

        public GitRepository(string directory)
		{
            this.workingDirectory = directory;
            this.changedFiles = null;
            try
            {
                isGit = true;
                var output = GitRun("rev-parse --is-inside-work-tree").Trim();
                isGit = string.Compare("true", output, true) == 0;
            }
            catch 
            {
                isGit = false;
            }
		}

		#region Git commands

        private string GitRun(string cmd)
        {
            if (!this.IsGit) return null;
            var result = GitBash.Run(cmd, this.WorkingDirectory);
            if (result.HasError) throw new GitException(result.Error);
            if (result.Output.Contains("fatal:")) throw new GitException(result.Output);
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
            try
            {
                return GitRun("show-ref refs/heads/" + name);
            }
            catch
            {
                return null;
            }
        }

		internal string DeleteBranch(string name)
		{
			return GitRun("branch -d " + name);
		}

        public void CheckOutBranch(string branch, bool createNew = false)
        {
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
	
        internal void InitRepo()
        {
            if (!isGit)
            {
                GitBash.Run("init", WorkingDirectory);
                var ignoreFileName = Path.Combine(WorkingDirectory, ".gitignore");
                if (!File.Exists(ignoreFileName))
                {
                    File.WriteAllText(ignoreFileName, Resources.IgnoreFileContent);
                }
            }
        }

        private bool IsBinaryFile(string fileName)
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

        internal string DiffFile(string fileName)
        {
            var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
            try
            {
                var status = GetFileStatus(fileName);
                if (status == GitFileStatus.NotControlled || status == GitFileStatus.New || status == GitFileStatus.Added)
                {
                    tmpFileName = Path.ChangeExtension(tmpFileName, Path.GetExtension(fileName));
                    File.Copy(Path.Combine(WorkingDirectory, fileName), tmpFileName);

                    if (IsBinaryFile(tmpFileName))
                    {
                        File.Delete(tmpFileName);
                        File.WriteAllText(tmpFileName, "Binary file: " + fileName);
                    }
                    return tmpFileName;
                }

                GitBash.RunCmd(string.Format("diff HEAD -- \"{0}\" > \"{1}\"", fileName, tmpFileName), WorkingDirectory);
            }
            catch (Exception ex)
            {
                File.WriteAllText(tmpFileName, ex.Message);
            }
            return tmpFileName;
        }

        private IEnumerable<GitFile> changedFiles;

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
            set
            {
                changedFiles = value;
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

                var gitFile = new GitFile { FileName = fileName.Trim() };

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
                var branch = "master";
                try
                {
                    branch = GitRun("rev-parse --abbrev-ref HEAD").Trim();
                    if (IsInTheMiddleOfBisect) branch += " | BISECTING";
                    if (IsInTheMiddleOfMerge) branch += " | MERGING";
                    if (IsInTheMiddleOfPatch) branch += " | AM";
                    if (IsInTheMiddleOfRebase) branch += " | REBASE";
                    if (IsInTheMiddleOfRebaseI) branch += " | REBASE-i";
                }
                catch (GitException ex)
                {
                }
                return branch;
            }
        }

        public bool IsInTheMiddleOfBisect
        {
            get
            {
                return this.IsGit ? FileExistsInRepo("BISECT_START") : false;
            }
        }

        public bool IsInTheMiddleOfMerge
        {
            get
            {
                return this.IsGit ? FileExistsInRepo("MERGE_HEAD") : false;
            }
        }

        public bool IsInTheMiddleOfPatch
        {
            get
            {
                return this.IsGit ? FileExistsInRepo("rebase-*", "applying") : false;
            }
        }

        public bool IsInTheMiddleOfRebase
        {
            get
            {
                return this.IsGit ? FileExistsInRepo("rebase-*", "rebasing") : false;
            }
        }

        public bool IsInTheMiddleOfRebaseI
        {
            get
            {
                return this.IsGit ? FileExistsInRepo("rebase-*", "interactive") : false;
            }
        }

        private bool FileExistsInRepo(string fileName)
        {
            return File.Exists(Path.Combine(WorkingDirectory, fileName));
        }

        private bool FileExistsInRepo(string directory, string fileName)
        {
            if (Directory.Exists(WorkingDirectory))
            {
                foreach (var dir in Directory.GetDirectories(WorkingDirectory, directory))
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
                    return GitRun("log -1 --format=%s\r\n\r\n%b");
                }
                catch
                {
                    return "";
                }
            }
        }

        internal GitFileStatus GetFileStatus(string fileName)
        {
            if (changedFiles == null) return GitFileStatus.NotControlled;

            var file = changedFiles.Where(f => string.Compare(f.FileName, fileName, true) == 0).FirstOrDefault();
            return file == null ? GitFileStatus.NotControlled : file.Status;
        }

        internal void StageFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                GitRun(string.Format("add \"{0}\"", fileName));
            }
            else
            {
                GitRun(string.Format("rm --cached -- \"{0}\"", fileName));
            }
        }

        internal void UnStageFile(string fileName)
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

        internal void AddIgnoreItem(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
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

        internal void CheckOutFile(string fileName)
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
    }

}

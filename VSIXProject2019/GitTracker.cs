using GitScc;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

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
            WatchFileChanges(directory);
        }

        //public bool NoRefresh { get; set; }

        Timer timer;
        FileSystemWatcher fileSystemWatcher;
        private void WatchFileChanges(string folder)
        {
            Debug.WriteLine("GT ==== Monitoring: " + folder);
            UnWatchFileChanges();

            if (!GitSccOptions.Current.DisableAutoRefresh)
            {
                fileSystemWatcher = new FileSystemWatcher(folder);
                fileSystemWatcher.IncludeSubdirectories = true;
                fileSystemWatcher.Deleted += new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.Changed += new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        private void UnWatchFileChanges()
        {
            if (fileSystemWatcher != null)
            {
                fileSystemWatcher.Deleted -= new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.Changed -= new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
                fileSystemWatcher = null;
            }
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var name = Path.GetFullPath(e.FullPath);
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                Debug.WriteLine("GT ==== File system changed [" + e.ChangeType.ToString() + "]" + e.FullPath);
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                timer = new Timer();
                timer.Interval = 500;
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = false;
                timer.Start();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            Debug.WriteLine("GT ==== Timer_Elapsed");
            Changed(this);
        }

    }
}

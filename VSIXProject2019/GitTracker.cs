using GitScc;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace VSIXProject2019
{
    public delegate void ChangedEventHandler(GitTracker tracker);

    public class GitTracker: IDisposable
    {
        public static bool NoRefresh = false;

        public string Directory { get; }
        public GitRepository Repository { get; }

        public event ChangedEventHandler Changed;

        public GitTracker(string directory)
        {
            this.Directory = directory;
            this.Repository = new GitRepository(directory);
            WatchFileChanges();
        }

        public void Dispose()
        {
            Debug.WriteLine("GT ==== Dispose ");
            if (timer != null ) timer.Stop();
            UnWatchFileChanges();
        }

        Timer timer;
        FileSystemWatcher fileSystemWatcher;
        private void WatchFileChanges()
        {
            UnWatchFileChanges();

            if (!GitSccOptions.Current.DisableAutoRefresh)
            {
                Debug.WriteLine("GT ==== Monitoring: " + Directory);

                fileSystemWatcher = new FileSystemWatcher(Directory);
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
                Debug.WriteLine("GT ==== Strop Monitoring: " + Directory);

                fileSystemWatcher.Deleted -= new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.Changed -= new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
                fileSystemWatcher = null;
            }
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!NoRefresh
                && !(e.Name.EndsWith(".git") && e.ChangeType == WatcherChangeTypes.Changed)
                && !e.Name.EndsWith(".lock") 
                && !this.Repository.IsIgnored(e.FullPath))
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
            Debug.WriteLine("GT ==== Timer_Elapsed - Fire Changed Event");
            Repository.Refresh();
            Changed(this);
        }

    }
}

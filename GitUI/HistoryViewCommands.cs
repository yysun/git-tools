using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace GitUI
{
    public static class HistoryViewCommands
    {
        public static readonly RoutedUICommand CloseCommitDetails = new RoutedUICommand("CloseCommitDetails", "CloseCommitDetails", typeof(MainWindow));
        public static readonly RoutedUICommand OpenCommitDetails = new RoutedUICommand("OpenCommitDetails", "OpenCommitDetails", typeof(MainWindow));
        public static readonly RoutedUICommand SelectCommit = new RoutedUICommand("SelectCommit", "SelectCommit", typeof(MainWindow));
        public static readonly RoutedUICommand CompareCommits = new RoutedUICommand("CompareCommits", "CompareCommits", typeof(MainWindow));
        public static readonly RoutedUICommand ExportGraph = new RoutedUICommand("ExportGraph", "ExportGraph", typeof(MainWindow));
        public static readonly RoutedUICommand RefreshGraph = new RoutedUICommand("RefreshGraph", "RefreshGraph", typeof(MainWindow));
        public static readonly RoutedUICommand ScrollToCommit = new RoutedUICommand("ScrollToCommit", "ScrollToCommit", typeof(MainWindow));
        public static readonly RoutedUICommand GraphLoaded = new RoutedUICommand("GraphLoaded", "GraphLoaded", typeof(MainWindow));
        public static readonly RoutedUICommand PendingChanges = new RoutedUICommand("PendingChanges", "PendingChanges", typeof(MainWindow));
        public static readonly RoutedUICommand ShowMessage = new RoutedUICommand("ShowMessage", "ShowMessage", typeof(MainWindow));
        public static readonly RoutedUICommand OpenRepository = new RoutedUICommand("OpenRepository", "OpenRepository", typeof(MainWindow));
    }

}

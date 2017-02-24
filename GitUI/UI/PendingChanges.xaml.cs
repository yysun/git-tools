using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GitScc;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitUI.UI
{
    /// <summary>
    /// Interaction logic for PendingChanges.xaml
    /// </summary>
    public partial class PendingChanges : UserControl
    {
        const string DISPLAY_MODE_NAME = "vd-git-tools.mode";
        private ListView activeListView;

        GitFileStatusTracker tracker;
        GitViewModel service;

        private string[] diffLines;

        public PendingChanges()
        {
            InitializeComponent();
        }

        #region Events
        private string sortMemberPath = "FileName";
        private ListSortDirection sortDirection = ListSortDirection.Ascending;

        private void dataGrid1_Sorting(object sender, DataGridSortingEventArgs e)
        {
            sortMemberPath = e.Column.SortMemberPath;
            sortDirection = e.Column.SortDirection != ListSortDirection.Ascending ?
                ListSortDirection.Ascending : ListSortDirection.Descending;
        }

        private void dataGrid1_KeyDown(object sender, KeyEventArgs e)
        {
            var selectedItem = this.dataGrid1.SelectedItem as GitFile;
            if (selectedItem == null || e.Key != Key.Space) return;
            var selected = !selectedItem.IsSelected;
            foreach (var item in this.dataGrid1.SelectedItems)
            {
                ((GitFile)item).IsSelected = selected;
            }
        }

        private void checkBoxSelected_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            foreach (var item in this.dataGrid1.SelectedItems)
            {
                ((GitFile)item).IsSelected = checkBox.IsChecked == true;
            }
        }

        private void checkBoxAllStaged_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            foreach (var item in this.dataGrid1.Items.Cast<GitFile>())
            {
                ((GitFile)item).IsSelected = checkBox.IsChecked == true;
            }
        }

        private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fileName = GetSelectedFileName();
            if (fileName == null)
            {
                this.ClearEditor();
                diffLines = new string[0];
                return;
            }

            Action act = () =>
            {
                service.NoRefresh = true;
                try
                {
                    //var ret = tracker.DiffFile(fileName);
                    //ret = ret.Replace("\r", "").Replace("\n", "\r\n");

                    //var tmpFileName = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
                    //File.WriteAllText(tmpFileName, ret);

                    var tmpFileName = tracker.DiffFile(fileName);
                    if (!string.IsNullOrWhiteSpace(tmpFileName) && File.Exists(tmpFileName))
                    {
                        if (new FileInfo(tmpFileName).Length > 2 * 1024 * 1024)
                        {
                            this.DiffEditor.Text = "File is too big to display: " + fileName;
                        }
                        else
                        {
                            diffLines = File.ReadAllLines(tmpFileName);
                            this.ShowFile(tmpFileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //ShowStatusMessage(ex.Message);
                    this.DiffEditor.Text = ex.Message;
                }
                service.NoRefresh = false;

            };

            this.Dispatcher.BeginInvoke(act, DispatcherPriority.ApplicationIdle);
        }

        private void ClearEditor()
        {
            this.DiffEditor.Text = "";
        }

        private void ShowFile(string fileName)
        {
            try
            {
                var ext = Path.GetExtension(fileName);
                if (ext == ".diff")
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    using (Stream s = assembly.GetManifestResourceStream("GitUI.Resources.Patch-Mode.xshd"))
                    {
                        using (XmlTextReader reader = new XmlTextReader(s))
                        {
                            DiffEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                }
                else
                {
                    this.DiffEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(
                        ext);
                }
                this.DiffEditor.ShowLineNumbers = true;
                this.DiffEditor.Load(fileName);

                pnlChangedFileTool.Visibility = Visibility.Collapsed;
                pnlStagedFileTool.Visibility = Visibility.Collapsed;
                if (this.activeListView == this.listUnstaged)
                {
                    pnlChangedFileTool.Visibility = Visibility.Visible;
                }
                else if (this.activeListView == this.listStaged)
                {
                    pnlStagedFileTool.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        private void ShowSelectedFile()
        {
            var fileName = GetSelectedFileName();

            this.ClearEditor();

            if (fileName == null)
            {
                diffLines = new string[0];
                return;
            }

            try
            {
                string tmpFileName = "";

                var status = tracker.GetFileStatus(fileName);
                if (status == GitFileStatus.NotControlled || status == GitFileStatus.New)
                {
                    tmpFileName = Path.Combine(tracker.WorkingDirectory, fileName);
                }
                else
                {
                    var diffAgainstIndex = this.activeListView == this.listStaged;
                    tmpFileName = tracker.DiffFileAdv(fileName, diffAgainstIndex);
                    
                }
                if (!string.IsNullOrWhiteSpace(tmpFileName) && File.Exists(tmpFileName))
                {
                    if (tracker.IsBinaryFile(tmpFileName))
                    {
                        this.DiffEditor.Text  = $"File \"{fileName}\" is binary that cannot be displayed.";
                    }
                    else if (new FileInfo(tmpFileName).Length > 2 * 1024 * 1024)
                    {
                        this.DiffEditor.Text = "File is too big to display: " + fileName;
                    }
                    else
                    {
                        diffLines = File.ReadAllLines(tmpFileName);
                        this.ShowFile(tmpFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                ShowStatusMessage(message);
            }
        }

        #endregion

        #region Select File
        private string GetSelectedFileName()
        {
            if (this.activeListView.SelectedItems.Count == 0)
                return null;
            var selectedItem = this.activeListView.SelectedItems[0] as GitFile;
            if (selectedItem == null) return null;
            return selectedItem.FileName;
        }

        private void GetSelectedFileName(Action<string> action, bool changeToGitPathSeparator = false)
        {
            var fileName = GetSelectedFileName();
            if (fileName == null) return;
            try
            {
                if (changeToGitPathSeparator) fileName.Replace("\\", "/");
                action(fileName);
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
            }
        }

        private void GetSelectedFiles(Action<string> action)
        {
            try
            {
                this.activeListView.SelectedItems.Cast<GitFile>()
                    .Select(item => item.FileName)
                    .ToList()
                    .ForEach(fileName => action(fileName));
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
            }
        }

        #endregion

        #region Git functions

        DateTime lastTimeRefresh = DateTime.Now.AddDays(-1);
        internal void Refresh(GitFileStatusTracker tracker)
        {
            this.tracker = tracker;

            if (this.activeListView == null) this.activeListView = this.listStaged;

            if (tracker == null)
            {
                ClearUI();
                return;
            }

            Action act = () =>
            {
                if (string.IsNullOrEmpty(Comments)) Comments = tracker.GetCommitTemplate();

                lblMessage.Content = "Commit to: " + tracker.CurrentBranch;
                //service.NoRefresh = true;
                ShowStatusMessage("Getting changed files ...");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                //var selectedFile = GetSelectedFileName();
                //var selectedFiles = this.dataGrid1.Items.Cast<GitFile>()
                //    .Where(i => i.IsSelected)
                //    .Select(i => i.FileName).ToList();
                //this.dataGrid1.BeginInit();

                var selectedFile = GetSelectedFileName();
                var selectedFiles = this.activeListView.Items.Cast<GitFile>()
                    .Where(i => i.IsSelected)
                    .Select(i => i.FileName).ToList();

                try
                {

                    //this.dataGrid1.ItemsSource = tracker.ChangedFiles;

                    //ICollectionView view = CollectionViewSource.GetDefaultView(this.dataGrid1.ItemsSource);
                    //if (view != null)
                    //{
                    //    view.SortDescriptions.Clear();
                    //    view.SortDescriptions.Add(new SortDescription(sortMemberPath, sortDirection));
                    //    view.Refresh();
                    //}

                    //this.dataGrid1.SelectedValue = selectedFile;
                    //selectedFiles.ForEach(fn =>
                    //{
                    //    var item = this.dataGrid1.Items.Cast<GitFile>()
                    //        .Where(i => i.FileName == fn)
                    //        .FirstOrDefault();
                    //    if (item != null) item.IsSelected = true;
                    //});

                    //ShowStatusMessage("");

                    //this.label3.Content = string.Format("Changed files: ({0}) {1}", tracker.CurrentBranch, tracker.ChangedFilesStatus);


                    this.listStaged.ItemsSource = tracker.ChangedFiles.Where(f => f.X != ' ' && f.X != '?');
                    this.listUnstaged.ItemsSource = tracker.ChangedFiles.Where(f => f.Y != ' ');

                    this.activeListView.SelectedValue = selectedFile;
                    selectedFiles.ForEach(fn =>
                    {
                        var item = this.activeListView.Items.Cast<GitFile>()
                            .Where(i => i.FileName == fn)
                            .FirstOrDefault();
                        if (item != null) item.IsSelected = true;
                    });

                    this.label3.Content = string.Format("Git Status:  {0}", tracker.ChangedFilesStatus);

                }
                catch (Exception ex)
                {
                    ShowStatusMessage(ex.Message);
                }
                //this.dataGrid1.EndInit();

                stopwatch.Stop();
                Debug.WriteLine("**** PendingChangesView Refresh: " + stopwatch.ElapsedMilliseconds);
            };

            this.Dispatcher.BeginInvoke(act, DispatcherPriority.ApplicationIdle);
        }

        internal void ClearUI()
        {
            this.label3.Content = "Changed files";
            this.chkAmend.IsChecked = false;
            this.chkSignOff.IsChecked = false;
            this.chkNewBranch.IsChecked = false;

            this.dataGrid1.ItemsSource = null;
            this.listStaged.ItemsSource = null;
            this.listUnstaged.ItemsSource = null;

            this.textBoxComments.Document.Blocks.Clear();
            this.ClearEditor();
            var chk = this.dataGrid1.FindVisualChild<CheckBox>("checkBoxAllStaged");
            if (chk != null) chk.IsChecked = false;
        }

        private string Comments
        {
            get
            {
                TextRange textRange = new TextRange(
                    this.textBoxComments.Document.ContentStart,
                    this.textBoxComments.Document.ContentEnd);
                return textRange.Text;
            }
            set
            {
                TextRange textRange = new TextRange(
                    this.textBoxComments.Document.ContentStart,
                    this.textBoxComments.Document.ContentEnd);
                textRange.Text = value;
            }
        }

        private bool HasComments()
        {
            if (string.IsNullOrWhiteSpace(Comments))
            {
                MessageBox.Show("Please enter comments for the commit.", "Commit",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
                return true;
        }

        private void StageSelectedFiles()
        {
            var unstaged = this.dataGrid1.Items.Cast<GitFile>()
                               .Where(item => item.IsSelected && !item.IsStaged)
                               .ToArray();
            var count = unstaged.Length;
            int i = 0;
            foreach (var item in unstaged)
            {
                tracker.StageFile(item.FileName);
                ShowStatusMessage(string.Format("Staged ({0}/{1}): {2}", i++, count, item.FileName));
            }
        }

        private void ShowStatusMessage(string msg)
        {

        }
        #endregion

        #region Menu Events

        private void dataGrid1_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (this.dataGrid1.SelectedItems.Count == 0) return;

            if (this.dataGrid1.SelectedItems.Count == 1)
            {
                var selectedItem = this.dataGrid1.SelectedItem as GitFile;
                if (selectedItem == null) return;

                switch (selectedItem.Status)
                {
                    case GitFileStatus.Added:
                    case GitFileStatus.New:
                        //menuCompare.IsEnabled = 
                        menuUndo.IsEnabled = false;
                        break;

                    case GitFileStatus.Modified:
                    case GitFileStatus.Staged:
                        //menuCompare.IsEnabled = 
                        menuUndo.IsEnabled = true;
                        break;

                    case GitFileStatus.Removed:
                    case GitFileStatus.Deleted:
                        //menuCompare.IsEnabled = false;
                        menuUndo.IsEnabled = true;
                        break;
                }

                menuStage.Visibility = selectedItem.IsStaged ? Visibility.Collapsed : Visibility.Visible;
                menuUnstage.Visibility = !selectedItem.IsStaged ? Visibility.Collapsed : Visibility.Visible;
                menuDeleteFile.Visibility = (selectedItem.Status == GitFileStatus.New || selectedItem.Status == GitFileStatus.Modified) ?
                    Visibility.Visible : Visibility.Collapsed;
                menuIgnore.IsEnabled = true;
            }
            else
            {
                menuStage.Visibility = 
                menuUnstage.Visibility =
                menuDeleteFile.Visibility = Visibility.Visible;
                menuUndo.IsEnabled = true;
                menuIgnore.IsEnabled = false;
                //menuCompare.IsEnabled = false;
            }
        }

        private void menuCompare_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFiles(fileName =>
            {
                //service.CompareFile(fileName);
            });
        }

        private void menuUndo_Click(object sender, RoutedEventArgs e)
        {
            const string deleteMsg = @"

Note: Undo file changes will restore the file(s) from the last commit.";

            var filesToUndo = new List<string>();

            GetSelectedFiles(fileName => filesToUndo.Add(fileName));

            string title = (filesToUndo.Count == 1) ? "Undo File Changes" : "Undo Files Changes for " + filesToUndo.Count + " Files?";
            string message = (filesToUndo.Count == 1) ?
                "Are you sure you want to undo changes to file: " + Path.GetFileName(filesToUndo.First()) + deleteMsg :
                String.Format("Are you sure you want to undo changes to {0} files", filesToUndo.Count) + deleteMsg;

            if (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var fileName in filesToUndo)
                    tracker.CheckOutFile(fileName);
            }
        }

        private async void StageFiles(IEnumerable<GitFile> unstaged)
        {
            int i = 1, count = unstaged.Count();
            service.NoRefresh = true;
            await Task.Run(() =>
            {
                foreach (var item in unstaged)
                {
                    tracker.StageFile(item.FileName);
                    ShowStatusMessage(string.Format("Staged ({0}/{1}): {2}", i++, count, item.FileName));
                }
            });
            service.NoRefresh = false;
            HistoryViewCommands.RefreshGraph.Execute(null, this);
        }

        private void menuStageAll_Click(object sender, RoutedEventArgs e)
        {
            var unstaged = this.activeListView.ItemsSource.Cast<GitFile>()
               .Where(item => !item.IsStaged);
            this.StageFiles(unstaged);
        }

        private void menuStage_Click(object sender, RoutedEventArgs e)
        {
            var unstaged = this.activeListView.SelectedItems.Cast<GitFile>()
               .Where(item => !item.IsStaged);
            this.StageFiles(unstaged);
        }

        private async void UnStageFiles(IEnumerable<GitFile> staged)
        {
            int i = 1, count = staged.Count();
            service.NoRefresh = true;
            await Task.Run(() =>
            {
                foreach (var item in staged)
                {
                    tracker.UnStageFile(item.FileName);
                    ShowStatusMessage(string.Format("Unstaged ({0}/{1}): {2}", i++, count, item.FileName));
                }
            });
            service.NoRefresh = false;
            HistoryViewCommands.RefreshGraph.Execute(null, this);
        }

        private void menuUnstageAll_Click(object sender, RoutedEventArgs e)
        {
            var staged = this.activeListView.ItemsSource.Cast<GitFile>()
               .Where(item => item.IsStaged);
            this.UnStageFiles(staged);
        }

        private void menuUnstage_Click(object sender, RoutedEventArgs e)
        {
            var staged = this.activeListView.SelectedItems.Cast<GitFile>()
               .Where(item => item.IsStaged);
            this.UnStageFiles(staged);
        }

        private void menuDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            const string deleteMsg = @"";

            var filesToDelete = new List<string>();

            GetSelectedFiles(fileName => filesToDelete.Add(fileName));

            string title = (filesToDelete.Count == 1) ? "Delete File" : "Delete " + filesToDelete.Count + " Files?";
            string message = (filesToDelete.Count == 1) ?
                "Are you sure you want to delete file: " + Path.GetFileName(filesToDelete.First()) + deleteMsg :
                String.Format("Are you sure you want to delete {0} files", filesToDelete.Count) + deleteMsg;

            if (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var fileName in filesToDelete)
                {
                    File.Delete(Path.Combine(this.tracker.WorkingDirectory, fileName));
                }
            }
        }

        #endregion

        #region listView1
        private GridViewColumnHeader _currentSortedColumn;
        private ListSortDirection _lastSortDirection;
        //private T FindAncestorOfType<T>(DependencyObject dependencyObject)
        //    where T : DependencyObject
        //{
        //    for (var current = dependencyObject; current != null; current = VisualTreeHelper.GetParent(current))
        //    {
        //        T typed = current as T;
        //        if (typed != null)
        //            return typed;
        //    }

        //    return null;
        //}

        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void listView1_Click(object sender, RoutedEventArgs e)
        {
            this.activeListView = sender as ListView;
            GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
            if (header == null || header.Role == GridViewColumnHeaderRole.Padding)
                return;

            ListSortDirection direction = ListSortDirection.Ascending;
            if (header == _currentSortedColumn && _lastSortDirection == ListSortDirection.Ascending)
                direction = ListSortDirection.Descending;

            Sort(header, direction);
            UpdateColumnHeaderTemplate(header, direction);
            _currentSortedColumn = header;
            _lastSortDirection = direction;
        }

        private void SortCurrentColumn()
        {
            if (_currentSortedColumn != null)
                Sort(_currentSortedColumn, _lastSortDirection);
        }

        private void Sort(GridViewColumnHeader header, ListSortDirection direction)
        {
            if (activeListView.ItemsSource != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(activeListView.ItemsSource);
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(header.Tag as string, direction));
                view.Refresh();
            }
        }

        private void UpdateColumnHeaderTemplate(GridViewColumnHeader header, ListSortDirection direction)
        {
            if (header.Column == null) return;

            if (direction == ListSortDirection.Ascending)
                header.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
            else
                header.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;

            if (_currentSortedColumn != null && _currentSortedColumn != header &&
                _currentSortedColumn.Column != null)
                _currentSortedColumn.Column.HeaderTemplate = null;
        }

        private void listView1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.activeListView != sender)
            {
                this.activeListView = sender as ListView;
                ShowSelectedFile();
            }
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.activeListView = sender as ListView;
            ShowSelectedFile();
        }

        #endregion

        #region Ignore files
        private void menuIgnore_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuIgnoreFile_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName((fileName) =>
            {
                tracker.AddIgnoreItem(fileName);
            }, true);
        }

        private void menuIgnoreFilePath_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName((fileName) =>
            {
                tracker.AddIgnoreItem(Path.GetDirectoryName(fileName) + "*/");
            }, true);
        }

        private void menuIgnoreFileExt_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName((fileName) =>
            {
                tracker.AddIgnoreItem("*" + Path.GetExtension(fileName));
            }, true);
        }

        private void menuRefresh_Click(object sender, RoutedEventArgs e)
        {
            HistoryViewCommands.RefreshGraph.Execute(null, this);
        } 

        #endregion

        private void chkNewBranch_Checked(object sender, RoutedEventArgs e)
        {
            txtNewBranch.Focus();
        }

        private void txtNewBranch_TextChanged(object sender, TextChangedEventArgs e)
        {
            chkNewBranch.IsChecked = txtNewBranch.Text.Length > 0;
        }

        private void chkAmend_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Comments))
            {
                Comments = tracker.LastCommitMessage;
                //service.UndoCommit(); // not sure if this is a good idea
            }
        }

        private void btnPendingChanges_Click(object sender, RoutedEventArgs e)
        {
            OnCommit();
        }

        internal async void OnCommit()
        {
            if (tracker == null) return;

            try
            {
                if (chkNewBranch.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(txtNewBranch.Text))
                    {
                        MessageBox.Show("Please enter new branch name.", "Commit",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        txtNewBranch.Focus();
                        return;
                    }
                    tracker.CheckOutBranch(txtNewBranch.Text, true);
                }

                var isAmend = chkAmend.IsChecked == true;

                if (string.IsNullOrWhiteSpace(Comments))
                {
                    Comments = tracker.GetCommitTemplate();
                    if (string.IsNullOrWhiteSpace(Comments))
                    {
                        MessageBox.Show("Please enter comments for the commit.", "Commit",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    return;
                }

                var changed = this.listUnstaged.ItemsSource.Cast<GitFile>();
                var count = changed.Count();

                ShowStatusMessage("Staging files ...");

                if (!isAmend)
                {
                    tracker.Refresh();
                    bool hasStaged = tracker == null ? false :
                                     tracker.ChangedFiles.Any(f => f.X != ' ') || count > 0; 

                    if (!hasStaged)
                    {
                        MessageBox.Show("No file has been selected/staged for commit.", "Commit",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                else
                {
                    const string amendMsg = @"You are about to amend a commit that has tags or remotes, which could cause issues in local and remote repositories.

Are you sure you want to continue?";

                    if (tracker.CurrentCommitHasRefs() && MessageBox.Show(amendMsg, "Amend Last Commit",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                service.NoRefresh = true;
                int i = 1;
                bool signoff = chkSignOff.IsChecked == true;

                await Task.Run(() =>
                {
                    if (changed.Count() > 0 && listStaged.Items.Count == 0)
                    {
                        // auto stage all changes if nothing is staged
                        foreach (var item in changed)
                        {
                            tracker.StageFile(item.FileName);
                            ShowStatusMessage(string.Format("Staged ({0}/{1}): {2}", i++, count, item.FileName));
                        }
                    }

                    var id = tracker.Commit(Comments, isAmend, signoff);
                    ShowStatusMessage("Commit successfully. Commit Hash: " + id);
                });

                ClearUI();
                Comments = tracker.GetCommitTemplate();
                tracker.Refresh();
                if (tracker.ChangedFiles.Count() == 0)
                {
                    HistoryViewCommands.CloseCommitDetails.Execute("PendingChanges", this);
                }
                service.NoRefresh = false;
                HistoryViewCommands.RefreshGraph.Execute(null, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ShowStatusMessage(ex.Message);
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                btnPendingChanges_Click(this, null);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.service = GitViewModel.Current;
        }

        private async void TryRun(Action act)
        {
            string message = "";
            Mouse.OverrideCursor = Cursors.Wait;
            service.NoRefresh = true;
            await Task.Run(() =>
            {
                try
                {
                    act();
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    ShowStatusMessage(message);
                }
            });
            if (message.Length > 0) MessageBox.Show(message, "Failed git apply", MessageBoxButton.OK, MessageBoxImage.Error);
            service.NoRefresh = false;
            HistoryViewCommands.RefreshGraph.Execute(null, this);
            Mouse.OverrideCursor = null;
        }

        private void btnStageFile_Click(object sender, RoutedEventArgs e)
        {
            var fileName = ((GitFile)this.activeListView.SelectedItem).FileName;
            TryRun(() =>
            {
                this.tracker.StageFile(fileName);
            });
        }

        private void btnStageSelected_Click(object sender, RoutedEventArgs e)
        {
            TryRun(() =>
            {
                var selectionPosition = this.GetEditorSelectionPosition();
                this.tracker.Apply(diffLines, selectionPosition[0], selectionPosition[1], true, false);
            });
        }

        private void btnResetFile_Click(object sender, RoutedEventArgs e)
        {
            var file = ((GitFile)this.activeListView.SelectedItem);
            var fileName = file.FileName;
            TryRun(() =>
            {
                if (file.Status == GitFileStatus.NotControlled || file.Status == GitFileStatus.New)
                {
                    File.Delete(Path.Combine(this.tracker.WorkingDirectory, fileName));
                }
                else
                {
                    this.tracker.CheckOutFile(fileName);
                }
            });
        }

        private void btnResetSelected_Click(object sender, RoutedEventArgs e)
        {
            TryRun(() =>
            {
                var selectionPosition = this.GetEditorSelectionPosition();
                this.tracker.Apply(diffLines, selectionPosition[0], selectionPosition[1], false, true);
            });
        }

        private void btnUnStageFile_Click(object sender, RoutedEventArgs e)
        {
            var fileName = ((GitFile)this.activeListView.SelectedItem).FileName;
            TryRun(() =>
            {
                this.tracker.UnStageFile(fileName);
            });
        }

        private void btnUnStageSelected_Click(object sender, RoutedEventArgs e)
        {
            TryRun(() =>
            {
                var selectionPosition = this.GetEditorSelectionPosition();
                this.tracker.Apply(diffLines, selectionPosition[0], selectionPosition[1], true, true);
            });
        }

        private int[] GetEditorSelectionPosition()
        {
            return new int[] { this.DiffEditor.SelectionStart, this.DiffEditor.SelectionStart + this.DiffEditor.SelectionLength };
        }

        private void DiffEditor_Click(object sender, RoutedEventArgs e)
        {
            if (this.tracker == null) return;

            var selectionPosition = this.GetEditorSelectionPosition();
            var hasChanges = tracker.HasChanges(diffLines, selectionPosition[0], selectionPosition[1]);
            btnResetSelected.Visibility = btnStageSelected.Visibility = btnUnStageSelected.Visibility =
                (hasChanges ? Visibility.Visible : Visibility.Collapsed);
        }
    }


    public static class ExtHelper
    {
        public static TChild FindVisualChild<TChild>(this DependencyObject obj, string name = null) where TChild : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChild && (name == null || ((Control)child).Name == name))
                {
                    return (TChild)child;
                }
                else
                {
                    TChild childOfChild = FindVisualChild<TChild>(child, name);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}

namespace VSIXProject2019
{
    using GitScc;
    using System;
    using System.Linq;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IO;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Interaction logic for GitChangesWindowControl.
    /// </summary>
    public partial class GitChangesWindowControl : UserControl
    {
        const string DISPLAY_MODE_NAME = "vs-git-tools.mode";

        private GitTracker tracker;
        private GitRepository repository;
        private ListView activeListView;
        private string[] diffLines = new string[] { };

        private GitChangesWindow toolWindow;
        private IVsTextView textView;

        EnvDTE80.DTE2 DTE { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitChangesWindowControl"/> class.
        /// </summary>
        public GitChangesWindowControl(GitChangesWindow toolWindow)
        {
            this.InitializeComponent();
            this.toolWindow = toolWindow;
            this.DTE = toolWindow.DTE;
            this.gitConsole1.ShowStatusMessage = msg => ShowStatusMessage(msg);
            this.gitConsole1.JoinableTaskFactory = toolWindow.AsyncPackage.JoinableTaskFactory;
        }

        public void Refresh(GitTracker tracker)
        {
            this.tracker = tracker;
            this.repository = tracker?.Repository;
            Refresh();
        }

        #region Events

        private void checkBoxSelected_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            foreach (var item in this.listView1.SelectedItems)
            {
                ((GitFile)item).IsSelected = checkBox.IsChecked == true;
            }
        }

        private void checkBoxAllStaged_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            foreach (var item in this.listView1.Items)
            {
                ((GitFile)item).IsSelected = checkBox.IsChecked == true;
            }
        }


        private void ClearEditor()
        {
            this.toolWindow.ClearEditor();
            fileInEditor = null;
            pnlChangedFileTool.Visibility = activeListView == listUnstaged &&
                listUnstaged.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            pnlStagedFileTool.Visibility = activeListView == listStaged &&
                listStaged.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            btnResetSelected.Visibility = btnStageSelected.Visibility
                = btnUnStageSelected.Visibility = Visibility.Collapsed;
        }

        string fileInEditor;

        private void ShowFile(string fileName)
        {
            try
            {
                this.textView = this.toolWindow.SetDisplayedFile(fileName);
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
                //File.Delete(fileName);
                fileInEditor = fileName;
            }
        }

        internal void ReloadEditor()
        {
            if (fileInEditor != null) ShowFile(fileInEditor);
        }

        #endregion

        #region listView1
        private GridViewColumnHeader _currentSortedColumn;
        private ListSortDirection _lastSortDirection;

        private void listView1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            this.activeListView = sender as ListView;
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            this.activeListView = sender as ListView;
            if (e.Key != Key.Space)
                return;

            var selectedItem = this.activeListView.SelectedItem as GitFile;
            if (selectedItem == null) return;
            var selected = !selectedItem.IsSelected;
            foreach (var item in this.activeListView.SelectedItems)
            {
                ((GitFile)item).IsSelected = selected;
            }

            e.Handled = true;
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.activeListView = sender as ListView;
            ShowSelectedFile();
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
                if (this.tabControl1.SelectedIndex != 0) this.tabControl1.SelectedIndex = 0;

                string tmpFileName = "";

                var status = repository.GetFileStatus(fileName);
                if (status == GitFileStatus.NotControlled || status == GitFileStatus.New)
                {
                    tmpFileName = Path.Combine(repository.WorkingDirectory, fileName);
                }
                else
                {
                    if (this.activeListView == listView1)
                    {
                        tmpFileName = repository.DiffFile(fileName);
                    }
                    else
                    {
                        var diffAgainstIndex = this.activeListView == this.listStaged;
                        tmpFileName = repository.DiffFileAdv(fileName, diffAgainstIndex);
                    }
                }
                if (!string.IsNullOrWhiteSpace(tmpFileName) && File.Exists(tmpFileName))
                {
                    if (repository.IsBinaryFile(tmpFileName))
                    {
                        this.DiffEditor.Content = $"File \"{fileName}\" is binary that cannot be displayed. Double click to to view.";
                    }
                    //if (new FileInfo(tmpFileName).Length > 2 * 1024 * 1024)
                    //{
                    //    this.DiffEditor.Content = "File is too big to display: " + fileName;
                    //}
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

        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.activeListView = sender as ListView;
            // only enable double-click to open when exactly one item is selected
            if (this.activeListView.SelectedItems.Count != 1)
                return;

            // disable double-click to open for the checkbox
            var checkBox = FindAncestorOfType<CheckBox>(e.OriginalSource as DependencyObject);
            if (checkBox != null)
                return;

            GetSelectedFileName((fileName) =>
            {
                OpenFile(fileName);
            });
        }

        private void listView1_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
                return;

            if (this.listView1.SelectedItems.Count == 1)
            {
                var selectedItem = this.listView1.SelectedItems[0] as GitFile;
                if (selectedItem == null) return;

                switch (selectedItem.Status)
                {
                    case GitFileStatus.Added:
                    case GitFileStatus.New:
                        menuCompare.IsEnabled = menuUndo.IsEnabled = false;
                        break;

                    case GitFileStatus.Modified:
                    case GitFileStatus.Staged:
                        menuCompare.IsEnabled = menuUndo.IsEnabled = true;
                        break;

                    case GitFileStatus.Removed:
                    case GitFileStatus.Deleted:
                        menuCompare.IsEnabled = false;
                        menuUndo.IsEnabled = true;
                        break;
                }

                menuStage.Visibility = selectedItem.IsStaged ? Visibility.Collapsed : Visibility.Visible;
                menuUnstage.Visibility = !selectedItem.IsStaged ? Visibility.Collapsed : Visibility.Visible;
                menuDeleteFile.Visibility = (selectedItem.Status == GitFileStatus.New || selectedItem.Status == GitFileStatus.Modified) ?
                    Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                menuStage.Visibility =
                menuUnstage.Visibility =
                menuDeleteFile.Visibility = Visibility.Visible;
                menuUndo.IsEnabled = true;
                menuIgnore.IsEnabled = false;
                menuCompare.IsEnabled = false;
            }

            //todo: evtl. hide menuCompareVS if both use the vs-diff tool...
            menuCompareVS.IsEnabled = menuCompare.IsEnabled;
        }

        private T FindAncestorOfType<T>(DependencyObject dependencyObject)
            where T : DependencyObject
        {
            for (var current = dependencyObject; current != null; current = VisualTreeHelper.GetParent(current))
            {
                T typed = current as T;
                if (typed != null)
                    return typed;
            }

            return null;
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

            // don't change the template if we're sorting by the check state
            GridViewColumn checkStateColumn = ((GridView)listView1.View).Columns[0];
            if (header.Column != checkStateColumn)
            {
                if (direction == ListSortDirection.Ascending)
                    header.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                else
                    header.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
            }

            if (_currentSortedColumn != null && _currentSortedColumn != header &&
                _currentSortedColumn.Column != null && _currentSortedColumn.Column != checkStateColumn)
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

        private void Refresh()
        {
            ShowStatusMessage("Refresh Git Changes");

            this.gitConsole1.Refresh(repository);

            if (this.activeListView == null) this.activeListView = this.listView1;

            if (tracker == null || repository == null)
            {
                ClearUI();
                return;
            }

            if (string.IsNullOrEmpty(Comments)) Comments = repository.GetCommitTemplate();
            this.chkAdvMode.IsChecked = this.repository.GetConfig(DISPLAY_MODE_NAME) == "advanced";

            var selectedFile = GetSelectedFileName();
            var selectedFiles = this.activeListView.Items.Cast<GitFile>()
                .Where(i => i.IsSelected)
                .Select(i => i.FileName).ToList();

            //this.activeListView.BeginInit();
            try
            {
                this.listView1.ItemsSource = repository.ChangedFiles;
                this.listStaged.ItemsSource = repository.ChangedFiles.Where(f => f.X != ' ' && f.X != '?');
                this.listUnstaged.ItemsSource = repository.ChangedFiles.Where(f => f.Y != ' ');

                this.activeListView.SelectedValue = selectedFile;
                selectedFiles.ForEach(fn =>
                {
                    var item = this.activeListView.Items.Cast<GitFile>()
                        .Where(i => i.FileName == fn)
                        .FirstOrDefault();
                    if (item != null) item.IsSelected = true;
                });

                this.label3.Content = string.Format("Git Status:  {0}", repository.ChangedFilesStatus);

                ShowSelectedFile();
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
                this.DiffEditor.Content = ex.Message;
            }

            //this.activeListView.EndInit();

            if (GitSccOptions.Current.DisableAutoRefresh)
                this.label4.Visibility = Visibility.Visible;
            else
                this.label4.Visibility = Visibility.Collapsed;

            ShowStatusMessage("");
        }

        internal void ClearUI()
        {
            this.label3.Content = "Not a Git repository";
            this.chkAmend.IsChecked = false;
            this.chkSignOff.IsChecked = false;

            this.listView1.ItemsSource = null;
            this.listStaged.ItemsSource = null;
            this.listUnstaged.ItemsSource = null;

            this.textBoxComments.Document.Blocks.Clear();
            this.ClearEditor();
            var chk = this.listView1.FindVisualChild<CheckBox>("checkBoxAllStaged");
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


        private async Task ShowStatusMessage(string msg)
        {
            await toolWindow.AsyncPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
            Action action = () => { Dispatcher.VerifyAccess(); DTE.StatusBar.Text = msg; };
            Dispatcher.Invoke(action);
        }
        #endregion

        #region Menu Events

        private void menuCompare_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName(fileName =>
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                GitFileStatus status = this.repository.GetFileStatus(fileName);
                if (status == GitFileStatus.Modified || status == GitFileStatus.Staged)
                {
                    if (sender == menuCompareVS)
                    {
                        string tempFile = Path.GetFileName(fileName);
                        tempFile = Path.Combine(Path.GetTempPath(), tempFile);
                        this.repository.SaveFileFromLastCommit(fileName, tempFile);

                        fileName = Path.Combine(this.repository.WorkingDirectory, fileName);
                        toolWindow.DiffService.OpenComparisonWindow(tempFile, fileName);
                    }
                    else
                    {
                        fileName = Path.Combine(this.repository.WorkingDirectory, fileName);
                        this.repository.DiffTool(fileName);
                    }
                }
            });
        }

        private async void menuUndo_Click(object sender, RoutedEventArgs e)
        {

            const string deleteMsg = @"

Note: Changes that have not committed will be lost.";

            var filesToUndo = this.activeListView.SelectedItems.Cast<GitFile>();
            if (filesToUndo.Count() <= 0) return;

            string title = (filesToUndo.Count() == 1) ? "Undo File Changes" : "Undo Files Changes for " + filesToUndo.Count() + " Files?";
            string message = (filesToUndo.Count() == 1) ?
                "Are you sure you want to undo changes to file: " + Path.GetFileName(filesToUndo.First().FileName) + deleteMsg :
                String.Format("Are you sure you want to undo changes to {0} files", filesToUndo.Count()) + deleteMsg;


            if (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    foreach (var file in filesToUndo)
                    {
                        if (file.Status == GitFileStatus.NotControlled || file.Status == GitFileStatus.New)
                        {
                            File.Delete(Path.Combine(this.repository.WorkingDirectory, file.FileName));
                        }
                        else
                        {
                            this.repository.CheckOutFile(file.FileName);
                        }
                    }
                });
            }
        }

        private async void StageFiles(IEnumerable<GitFile> unstaged)
        {
            int i = 1, count = unstaged.Count();
            await Task.Run(() =>
            {
                foreach (var item in unstaged)
                {
                    repository.StageFile(item.FileName);
                    ShowStatusMessage(string.Format("Staged ({0}/{1}): {2}", i++, count, item.FileName));
                }
            });
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
            await Task.Run(() =>
            {
                foreach (var item in staged)
                {
                    repository.UnStageFile(item.FileName);
                    ShowStatusMessage(string.Format("Unstaged ({0}/{1}): {2}", i++, count, item.FileName));
                }
            });
        }

        private void menuUnstage_Click(object sender, RoutedEventArgs e)
        {
            var staged = this.activeListView.SelectedItems.Cast<GitFile>()
               .Where(item => item.IsStaged || item.X != ' ');
            this.UnStageFiles(staged);
        }

        private void menuDeleteFile_Click(object sender, RoutedEventArgs e)
        {

            const string deleteMsg = @"

Note: if the file is included project, you need to delete the file from project in solution explorer.";

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
                    File.Delete(Path.Combine(this.repository.WorkingDirectory, fileName));
                }
            }
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
                repository.AddIgnoreItem(fileName);
            }, true);
        }

        private void menuIgnoreFilePath_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName((fileName) =>
            {
                repository.AddIgnoreItem(Path.GetDirectoryName(fileName) + "*/");
            }, true);
        }

        private void menuIgnoreFileExt_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName((fileName) =>
            {
                repository.AddIgnoreItem("*" + Path.GetExtension(fileName));
            }, true);
        }

        #endregion


        private void chkAmend_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Comments) && tracker != null)
            {
                Comments = repository.LastCommitMessage;
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OnCommit();
            }
        }

        private int[] GetEditorSelectionPosition()
        {
            int sl = 0, sc = 0, el = 0, ec = 0;
            try
            {
                if (0 != textView.GetSelection(out sl, out sc, out el, out ec))
                {
                    textView.GetCaretPos(out sl, out sc);
                    el = sl;
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
            }
            return new int[2] { sl + 1, el + 1 };
        }

        private void DiffEditor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var hasChanges = false;
            if (this.tracker != null)
            {
                var selectionPosition = this.GetEditorSelectionPosition();
                hasChanges = repository.HasChanges(diffLines, selectionPosition[0], selectionPosition[1]);
            }
            btnResetSelected.Visibility = btnStageSelected.Visibility = btnUnStageSelected.Visibility =
                (hasChanges ? Visibility.Visible : Visibility.Collapsed);
        }


        private void DiffEditor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int start = 1, column = 1; bool diff = false;
            try
            {
                if (this.textView != null && diffLines != null && diffLines.Length > 0)
                {
                    int line;
                    textView.GetCaretPos(out line, out column);

                    string text = diffLines[line];
                    while (line >= 0)
                    {
                        var match = Regex.Match(text, "^@@(.+)@@");
                        if (match.Success)
                        {
                            var s = match.Groups[1].Value;
                            s = s.Substring(s.IndexOf('+') + 1);
                            s = s.Substring(0, s.IndexOf(','));
                            start += Convert.ToInt32(s) - 2;
                            diff = true;
                            break;
                        }
                        else if (text.StartsWith("-"))
                        {
                            start--;
                        }

                        start++;
                        --line;
                        text = line >= 0 ? diffLines[line] : "";
                    }
                }
                if (!diff) start--;
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
            }
            GetSelectedFileName((fileName) =>
            {
                OpenFile(fileName);
                var selection = DTE.ActiveDocument.Selection as EnvDTE.TextSelection;
                selection.MoveToLineAndOffset(start, column);
            });
        }

        private void OpenFile(string fileName)
        {
            fileName = System.IO.Path.Combine(this.repository.WorkingDirectory, fileName);
            if (string.IsNullOrWhiteSpace(fileName)) return;
            fileName = fileName.Replace("/", "\\");
            DTE.ItemOperations.OpenFile(fileName);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            GridViewColumnCollection columns = ((GridView)listView1.View).Columns;
            _currentSortedColumn = (GridViewColumnHeader)columns[columns.Count - 1].Header;
            _lastSortDirection = ListSortDirection.Ascending;
            UpdateColumnHeaderTemplate(_currentSortedColumn, _lastSortDirection);
        }
        internal void OnSettings()
        {
            Settings.Show(this.tracker?.Repository);
        }

        internal async Task OnCommit()
        {
            if (tracker == null) return;
            if (!hasFileSaved()) return;

            try
            {
                var isAmend = chkAmend.IsChecked == true;

                if (string.IsNullOrWhiteSpace(Comments))
                {
                    Comments = repository.GetCommitTemplate();
                    if (string.IsNullOrWhiteSpace(Comments))
                    {
                        MessageBox.Show("Please enter comments for the commit.", "Commit",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    return;
                }

                var unstaged = this.listView1.Items.Cast<GitFile>()
                   .Where(item => item.IsSelected && !item.IsStaged);

                var count = unstaged.Count();

                var advancedMode = this.chkAdvMode.IsChecked == true;
                var changed = this.listUnstaged.ItemsSource.Cast<GitFile>();

                ShowStatusMessage("Staging files ...");

                if (!isAmend)
                {
                    bool hasStaged = false;

                    if (advancedMode)
                    {
                        // advanced mode
                        hasStaged = repository == null ? false :
                                    repository.ChangedFiles.Any(f => f.X != ' ') || count > 0;

                        // if nothing staged, staged to be all changes
                        if (!hasStaged) hasStaged = changed.Count() > 0;
                    }
                    else
                    {
                        // simple mode
                        hasStaged = repository == null ? false :
                                    repository.ChangedFiles.Any(f => f.IsStaged) || count > 0;
                    }

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

                    if (repository.CurrentCommitHasRefs() && MessageBox.Show(amendMsg, "Amend Last Commit",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                int i = 1;
                bool signoff = chkSignOff.IsChecked == true;

                await Task.Run(() =>
                {
                    if (advancedMode && changed.Count() > 0 && listStaged.Items.Count == 0)
                    {
                        count = changed.Count();
                        // auto stage all changes if nothing is staged
                        foreach (var item in changed)
                        {
                            repository.StageFile(item.FileName);
                            ShowStatusMessage(string.Format("Staged ({0}/{1}): {2}", i++, count, item.FileName));
                        }
                    }
                    else
                    {
                        foreach (var item in unstaged)
                        {
                            repository.StageFile(item.FileName);
                            ShowStatusMessage(string.Format("Staged ({0}/{1}): {2}", i++, count, item.FileName));
                        }
                    }
                    var id = repository.Commit(Comments, isAmend, signoff);
                    ShowStatusMessage("Commit successfully. Commit Hash: " + id);
                });
                ClearUI();
                Comments = repository.GetCommitTemplate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ShowStatusMessage(ex.Message);
            }
        }

        private void SetSimpleMode()
        {

            if (this.listView1 != null) this.listView1.Visibility = Visibility.Visible;
            if (this.gridAdvancedMode != null) this.gridAdvancedMode.Visibility = Visibility.Collapsed;
            if (this.pnlChangedFileTool != null) this.pnlChangedFileTool.Visibility = Visibility.Collapsed;
            if (this.pnlStagedFileTool != null) this.pnlStagedFileTool.Visibility = Visibility.Collapsed;
            if (this.repository != null && (this.repository.GetConfig(DISPLAY_MODE_NAME) != "" ||
                this.repository.GetConfig(DISPLAY_MODE_NAME) != "simple"))
                this.repository.SetConfig(DISPLAY_MODE_NAME, "simple");
        }

        private void SetAdvancedMode()
        {
            if (this.listView1 != null) this.listView1.Visibility = Visibility.Collapsed;
            if (this.gridAdvancedMode != null) this.gridAdvancedMode.Visibility = Visibility.Visible;
            if (this.repository != null && this.repository.GetConfig(DISPLAY_MODE_NAME) != "advanced")
                this.repository.SetConfig(DISPLAY_MODE_NAME, "advanced");
        }

        private void chkAdvMode_Checked(object sender, RoutedEventArgs e)
        {
            SetAdvancedMode();
        }

        private void chkAdvMode_Unchecked(object sender, RoutedEventArgs e)
        {
            SetSimpleMode();
        }

        private async void TryRun(Action act)
        {
            string message = "";
            Mouse.OverrideCursor = Cursors.Wait;
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
            Mouse.OverrideCursor = null;
        }

        private void btnStageFile_Click(object sender, RoutedEventArgs e)
        {
            menuStage_Click(this, null);
        }

        private void btnStageSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectionPosition = this.GetEditorSelectionPosition();
            TryRun(() =>
            {
                this.repository.Apply(diffLines, selectionPosition[0], selectionPosition[1], true, false);
            });
        }

        private void btnResetFile_Click(object sender, RoutedEventArgs e)
        {
            menuUndo_Click(this, null);
        }

        private void btnResetSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectionPosition = this.GetEditorSelectionPosition();
            TryRun(() =>
            {
                this.repository.Apply(diffLines, selectionPosition[0], selectionPosition[1], false, true);
            });
        }

        private void btnUnStageFile_Click(object sender, RoutedEventArgs e)
        {
            menuUnstage_Click(this, null);
        }

        private void btnDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            menuDeleteFile_Click(this, null);
        }

        private void btnUnStageSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectionPosition = this.GetEditorSelectionPosition();
            TryRun(() =>
            {
                this.repository.Apply(diffLines, selectionPosition[0], selectionPosition[1], true, true);
            });
        }

        private void btnStageAll_Click(object sender, RoutedEventArgs e)
        {
            var unstaged = this.listUnstaged.ItemsSource.Cast<GitFile>()
               .Where(item => !item.IsStaged);
            StageFiles(unstaged);
        }

        private void btnUnstageAll_Click(object sender, RoutedEventArgs e)
        {
            var staged = this.listStaged.ItemsSource.Cast<GitFile>()
               .Where(item => item.IsStaged);
            UnStageFiles(staged);
        }

        private void listUnstaged_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            foreach (MenuItem item in listUnstaged.ContextMenu.Items)
                item.IsEnabled = listUnstaged.SelectedItems.Count > 0;
        }

        private void listStaged_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            foreach (MenuItem item in listStaged.ContextMenu.Items)
                item.IsEnabled = listStaged.SelectedItems.Count > 0;
        }

        internal bool hasFileSaved()
        {
            return DTE.ItemOperations.PromptToSave != EnvDTE.vsPromptResult.vsPromptResultCancelled;
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
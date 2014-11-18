using GitScc;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace F1SYS.VsGitToolsPackage
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        GitRepository tracker;
        VsGitToolsService service;

        private MyToolWindow toolWindow;
        private IVsTextView textView;
        private string[] diffLines;

        public MyControl(MyToolWindow toolWindow)
        {
            InitializeComponent();
            this.toolWindow = toolWindow;
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
            foreach (var item in this.listView1.Items.Cast<GitFile>())
            {
                ((GitFile)item).IsSelected = checkBox.IsChecked == true;
            }
        }


        private void ClearEditor()
        {
            this.toolWindow.ClearEditor();
            this.DiffEditor.Content = null;
        }

        private void ShowFile(string fileName)
        {
            try
            {
                var tuple = this.toolWindow.SetDisplayedFile(fileName);
                if (tuple != null)
                {
                    this.DiffEditor.Content = tuple.Item1;
                    this.textView = tuple.Item2;
                }
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        #endregion

        #region listView1
        private GridViewColumnHeader _currentSortedColumn;
        private ListSortDirection _lastSortDirection;

        private void listView1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space)
                return;

            var selectedItem = this.listView1.SelectedItem as GitFile;
            if (selectedItem == null) return;
            var selected = !selectedItem.IsSelected;
            foreach (var item in this.listView1.SelectedItems)
            {
                ((GitFile)item).IsSelected = selected;
            }

            e.Handled = true;
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fileName = GetSelectedFileName();
            if (fileName == null)
            {
                this.ClearEditor();
                diffLines = new string[0];
                return;
            }

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
                        Action action = () => this.DiffEditor.Content = "File is too big to display: " + fileName;
                        Dispatcher.Invoke(action);
                    }
                    else
                    {
                        diffLines = File.ReadAllLines(tmpFileName);
                        Action action = () => this.ShowFile(tmpFileName);
                        Dispatcher.Invoke(action);
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
            // only enable double-click to open when exactly one item is selected
            if (listView1.SelectedItems.Count != 1)
                return;

            // disable double-click to open for the checkbox
            var checkBox = FindAncestorOfType<CheckBox>(e.OriginalSource as DependencyObject);
            if (checkBox != null)
                return;

            GetSelectedFileFullName((fileName) =>
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
            if (listView1.ItemsSource != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(listView1.ItemsSource);
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(header.Tag as string, direction));
                view.Refresh();
            }
        }

        private void UpdateColumnHeaderTemplate(GridViewColumnHeader header, ListSortDirection direction)
        {
            // don't change the template if we're sorting by the check state
            GridViewColumn checkStateColumn = ((GridView)listView1.View).Columns[0];
            if (header.Column != checkStateColumn)
            {
                if (direction == ListSortDirection.Ascending)
                    header.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                else
                    header.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
            }

            if (_currentSortedColumn != null && _currentSortedColumn != header && _currentSortedColumn.Column != checkStateColumn)
                _currentSortedColumn.Column.HeaderTemplate = null;
        }

        #endregion

        #region Select File
        private string GetSelectedFileName()
        {
            if (this.listView1.SelectedItems.Count == 0)
                return null;
            var selectedItem = this.listView1.SelectedItems[0] as GitFile;
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

        private void GetSelectedFileFullName(Action<string> action, bool fileMustExists = true)
        {
            try
            {
                var files = this.listView1.SelectedItems.Cast<GitFile>()
                    .Select(item => System.IO.Path.Combine(this.tracker.WorkingDirectory, item.FileName))
                    .ToList();

                foreach (var fileName in files)
                {
                    if (fileMustExists && !File.Exists(fileName)) return;
                    action(fileName);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
            }
        }

        #endregion

        #region Git functions

        internal void Refresh(VsGitToolsService service)
        {
            this.label3.Content = "Changed files";
            this.service = service;
            this.tracker = service.Repository;

            this.chkAmend.IsChecked = false;
            this.chkSignOff.IsChecked = false;
            //this.chkNewBranch.IsChecked = false;

            if (tracker == null || tracker == null)
            {
                ClearUI();
                return;
            }

            service.NoRefresh = true;
            ShowStatusMessage("Getting changed files ...");

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

            var selectedFile = GetSelectedFileName();
            var selectedFiles = this.listView1.Items.Cast<GitFile>()
                .Where(i => i.IsSelected)
                .Select(i => i.FileName).ToList();

            this.listView1.BeginInit();
            try
            {

                this.listView1.ItemsSource = tracker.ChangedFiles;

                this.listView1.SelectedValue = selectedFile;
                selectedFiles.ForEach(fn =>
                {
                    var item = this.listView1.Items.Cast<GitFile>()
                        .Where(i => i.FileName == fn)
                        .FirstOrDefault();
                    if (item != null) item.IsSelected = true;
                });

                ShowStatusMessage("");

                var changed = tracker.ChangedFiles;
                this.label3.Content = string.Format("Changed files:  +{0} ~{1} -{2} !{3}",
                    changed.Where(f => f.Status == GitFileStatus.New || f.Status == GitFileStatus.Added).Count(),
                    changed.Where(f => f.Status == GitFileStatus.Modified || f.Status == GitFileStatus.Staged).Count(),
                    changed.Where(f => f.Status == GitFileStatus.Deleted || f.Status == GitFileStatus.Removed).Count(),
                    changed.Where(f => f.Status == GitFileStatus.Conflict).Count());
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
                this.DiffEditor.Content = ex.Message;
            }

            this.listView1.EndInit();



            //stopwatch.Stop();
            //Debug.WriteLine("**** PendingChangesView Refresh: " + stopwatch.ElapsedMilliseconds);

            //if (!GitSccOptions.Current.DisableAutoRefresh && stopwatch.ElapsedMilliseconds > 1000)
            //    this.label4.Visibility = Visibility.Visible;
            //else
            //    this.label4.Visibility = Visibility.Collapsed;

            service.NoRefresh = false;
        }

        internal void ClearUI()
        {
            this.listView1.ItemsSource = null;
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

        private void StageSelectedFiles()
        {
            var unstaged = this.listView1.Items.Cast<GitFile>()
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
            Action action = () => { toolWindow.dte.StatusBar.Text = msg; };
            Dispatcher.Invoke(action);
        }
        #endregion

        #region Menu Events

        private void menuCompare_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileFullName(fileName =>
            {
                //service.CompareFile(fileName);
            });
        }

        private void menuUndo_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileName(fileName =>
            {
                GitFileStatus status = tracker.GetFileStatus(fileName);
                if (status == GitFileStatus.Modified || status == GitFileStatus.Staged ||
                    status == GitFileStatus.Deleted || status == GitFileStatus.Removed)
                {
                    if (MessageBox.Show("Are you sure you want to undo changes for " + Path.GetFileName(fileName) +
                        " and restore a version from the last commit? ",
                        "Undo Changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        tracker.CheckOutFile(fileName);
                    }
                }
            }, false); // file must exists check flag is false
        }

        private void menuStage_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileFullName(fileName =>
            {
                tracker.StageFile(fileName);
                ShowStatusMessage("Staged file: " + fileName);
            }, false);
        }

        private void menuUnstage_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileFullName(fileName =>
            {
                tracker.UnStageFile(fileName);
                ShowStatusMessage("Un-staged file: " + fileName);
            }, false);
        }

        private void menuDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedFileFullName(fileName =>
            {
                if (MessageBox.Show("Are you sure you want to delete file: " + Path.GetFileName(fileName),
                                   "Delete File",
                                   MessageBoxButton.YesNo,
                                   MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    File.Delete(fileName);
                }
            });
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

        #endregion

        //private void chkNewBranch_Checked(object sender, RoutedEventArgs e)
        //{
        //    txtNewBranch.Focus();
        //}

        //private void txtNewBranch_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    chkNewBranch.IsChecked = txtNewBranch.Text.Length > 0;
        //}

        private void chkAmend_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Comments) && tracker != null)
            {
                Comments = tracker.LastCommitMessage;
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OnCommit();
            }
        }

        private void DiffEditor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int start = 1, column = 1;
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
            }
            catch (Exception ex)
            {
                ShowStatusMessage(ex.Message);
            }
            GetSelectedFileFullName((fileName) =>
            {
                OpenFile(fileName);
                var dte = toolWindow.dte;
                var selection = dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                selection.MoveToLineAndOffset(start, column);
            });
        }

        private void OpenFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            fileName = fileName.Replace("/", "\\");
            var dte = toolWindow.dte;
            bool opened = false;
            Array projects = (Array)dte.ActiveSolutionProjects;
            foreach (dynamic project in projects)
            {
                foreach (dynamic item in project.ProjectItems)
                {
                    if (string.Compare(item.FileNames[0], fileName, true) == 0)
                    {
                        dynamic wnd = item.Open(EnvDTE.Constants.vsViewKindPrimary);
                        wnd.Activate();
                        opened = true;
                        break;
                    }
                }
                if (opened) break;
            }

            if (!opened) dte.ItemOperations.OpenFile(fileName);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.DiffEditor.Content = "";

            GridViewColumnCollection columns = ((GridView)listView1.View).Columns;
            _currentSortedColumn = (GridViewColumnHeader)columns[columns.Count - 1].Header;
            _lastSortDirection = ListSortDirection.Ascending;
            UpdateColumnHeaderTemplate(_currentSortedColumn, _lastSortDirection);
        }
        internal void OnSettings()
        {
            Settings.Show();
        }

        internal void OnCommit()
        {
            if (tracker == null) return;

            try
            {
                service.NoRefresh = true;

                //if (chkNewBranch.IsChecked == true)
                //{
                //    if (string.IsNullOrWhiteSpace(txtNewBranch.Text))
                //    {
                //        MessageBox.Show("Please enter new branch name.", "Commit",
                //            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //        txtNewBranch.Focus();
                //        return;
                //    }
                //    tracker.CheckOutBranch(txtNewBranch.Text, true);
                //}

                var isAmend = chkAmend.IsChecked == true;

                if (string.IsNullOrWhiteSpace(Comments))
                {
                    MessageBox.Show("Please enter comments for the commit.", "Commit",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                ShowStatusMessage("Staging files ...");
                StageSelectedFiles();

                if (!isAmend)
                {
                    tracker.Refresh();
                    bool hasStaged = tracker == null ? false :
                                     tracker.ChangedFiles.Any(f => f.IsStaged);
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

                var id = tracker.Commit(Comments, isAmend, chkSignOff.IsChecked == true);
                ShowStatusMessage("Commit successfully. Commit Hash: " + id);
                ClearUI();
                service.NoRefresh = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ShowStatusMessage(ex.Message);
            }
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
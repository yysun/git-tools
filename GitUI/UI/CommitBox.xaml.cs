using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GitUI;
using Microsoft.VisualBasic;
using GitScc.DataServices;

namespace GitScc.UI
{
    /// <summary>
    /// Interaction logic for CommitBox.xaml
    /// </summary>
    public partial class CommitBox : UserControl
    {
        // need to match the size of top grid
        internal const int HEIGHT = 120;
        internal const int WIDTH = 200;

        private bool selected;
        public bool Selected
        {
            get { return selected; }
            set
            {
                this.selected = value;
                VisualStateManager.GoToElementState(this.root, this.selected ? "SelectedSate" : "NotSelectedState", true);
            }
        }

        public CommitBox()
        {
            InitializeComponent();
        }

        private void root_MouseEnter(object sender, MouseEventArgs e)
        {
            VisualStateManager.GoToElementState(this.root, "MouseOverState", true);
        }

        private void root_MouseLeave(object sender, MouseEventArgs e)
        {
            VisualStateManager.GoToElementState(this.root, "NormalState", true);
        }

        private void root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //this.Selected = !this.Selected;
            HistoryViewCommands.OpenCommitDetails.Execute(this.txtId.Text, null);
        }

        private void NewTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic commit = this.DataContext;
                var text = string.Format("Enter tag for commit: {0}",
                    commit.ShortId, commit.Comments, commit.Author, commit.Date);

                string tag = Interaction.InputBox(text, "git tag", "");

                if (string.IsNullOrWhiteSpace(tag)) return;

                //var tag1 = ((Ref[])commit.Refs).Where(r => r.Type == RefTypes.Tag
                //    && r.Name == tag).FirstOrDefault();
                //if (tag1 != null && tag1.Id.StartsWith(commit.ShortId)) return;

                string tagId = GitViewModel.Current.GetTagId(tag).Output;

                if (!string.IsNullOrWhiteSpace(tagId))
                {
                    MessageBox.Show("Tag already exists for " + tagId, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var ret = GitViewModel.Current.AddTag(tag, commit.ShortId);
                    HistoryViewCommands.ShowMessage.Execute(new { GitBashResult = ret }, this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewBranch_Click(object sender, RoutedEventArgs e)
        {

            dynamic commit = this.DataContext;
            var text = string.Format("Enter branch name for commit: {0}",
                commit.ShortId, commit.Comments, commit.Author, commit.Date);

            string branch = Interaction.InputBox(text, "git branch", "");

            if (string.IsNullOrWhiteSpace(branch)) return;

            var branch1 = ((Ref[])commit.Refs).Where(r => r.Type == RefTypes.Branch
                && r.Name == branch).FirstOrDefault();
            if (branch1 != null && branch1.Id.StartsWith(commit.ShortId)) return;

            string branchId = GitViewModel.Current.GetBranchId(branch).Output;

            GitBashResult ret;
            MessageBoxResult result;

            if (!string.IsNullOrWhiteSpace(branchId))
            {
                result = MessageBox.Show("Branch " + branch + " already exists.\r\n\r\n" +
                    "Do you want to move it to here?", "Warning",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ret = GitViewModel.Current.DeleteBranch(branch);
                    if (ret.HasError)
                    {
                        HistoryViewCommands.ShowMessage.Execute(new { GitBashResult = ret }, this);
                        return;
                    }
                }
            }

            ret = GitViewModel.Current.AddBranch(branch, commit.ShortId);
            if (!ret.HasError)
            {
                result = MessageBox.Show("Branch " + branch + " has been created.\r\n\r\n" +
                        "Do you want to check it out?", "Checkout",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    GitViewModel.Current.CheckoutBranch(branch);
                }
            }

            HistoryViewCommands.ShowMessage.Execute(new { GitBashResult = ret }, this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HistoryViewCommands.SelectCommit.Execute(this.txtId.Text, this);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".zip";
            dlg.Filter = "Archive (.zip)|*.zip";
            dlg.FileName = this.txtId.Text + ".zip";
            if (dlg.ShowDialog() == true)
            {
                GitViewModel.Current.Archive(this.txtId.Text, dlg.FileName);
            }
        }

        private void CheckoutCommit_Click(object sender, RoutedEventArgs e)
        {
            GitViewModel.Current.CheckoutBranch(this.txtId.Text);
        }

        private void CherryPick_Click(object sender, RoutedEventArgs e)
        {
            var branch = GitViewModel.Current.Tracker.CurrentBranch;
            var result = MessageBox.Show("Are you sure you want to cherry pick " + txtId.Text
                + " to current branch " + branch + "?", "Warning",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                GitViewModel.Current.CherryPick(this.txtId.Text);
            }
        }

        private void RebaseI_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to start interactive rebase from HEAD to " + 
                txtId.Text + "?", "Warning",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                GitViewModel.Current.RebaseI(this.txtId.Text);
            }

        }

    }
}

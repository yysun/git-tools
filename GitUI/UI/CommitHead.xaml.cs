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

namespace GitScc.UI
{
    /// <summary>
    /// Interaction logic for CommitHead.xaml
    /// </summary>
    public partial class CommitHead : UserControl
    {
        public CommitHead()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (BranchName == "HEAD" || this.txtHead.Text == "*")
            {
                this.border.Background = this.border.BorderBrush =
                this.polygon.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                this.txtName.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                this.txtHead.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                this.menuCheckoutBranch.IsEnabled = this.menuDeleteBranch.IsEnabled =
                this.menuRebase.IsEnabled = false;
            }
        }

        private string BranchName { get { return this.txtName.Text; } }

        private void CheckoutBranch_Click(object sender, RoutedEventArgs e)
        {
            GitViewModel.Current.CheckoutBranch(BranchName);
        }

        private void DeleteBranch_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete branch: " + BranchName,
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                GitViewModel.Current.DeleteBranch(BranchName);
            }
        }

        private void Rebase_Click(object sender, RoutedEventArgs e)
        {
            var branch = GitViewModel.Current.Tracker.CurrentBranch;

            var result = MessageBox.Show("Are you sure you want to rebase current branch: " + branch
                + " on top of branch: " + BranchName + "?", "Rebase",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                GitViewModel.Current.Rebase(BranchName);
            }
        }

        private void Merge_Click(object sender, RoutedEventArgs e)
        {
            var branch = GitViewModel.Current.Tracker.CurrentBranch;

            var result = MessageBox.Show("Are you sure you want to merge current branch: " + branch
                + " with branch: " + BranchName + "?", "Merge",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                GitViewModel.Current.Merge(BranchName);
            }
        }
    }
}

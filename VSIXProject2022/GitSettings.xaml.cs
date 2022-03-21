using GitScc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VSIXProject2019
{
    /// <summary>
    /// Interaction logic for GitSettings.xaml
    /// </summary>
    public partial class GitSettings : UserControl
    {
        GitRepository tracker;

        public GitSettings()
        {
            InitializeComponent();
            txtDonationLink.NavigateUri = new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KBCLF3PZD6C98&lc=US&item_name=Git%20Tools%20for%20Visual%20Studio&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted");
        }
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "git.exe";
            dlg.DefaultExt = ".exe";
            dlg.Filter = "EXE (.exe)|*.exe";

            if (dlg.ShowDialog() == true)
            {
                txtGitExePath.Text = dlg.FileName;

                CheckGitBash();
            }
        }

        private void CheckGitBash()
        {
            GitBash.GitExePath = txtGitExePath.Text;
            txtGitExePath.Text = GitBash.GitExePath;
            try
            {
                var result = GitBash.Run("version");
                txtMessage.Content = result.Output;
                result = GitBash.Run("config --get user.name");
                txtUserName.Text = result.Output.Trim();
                result = GitBash.Run("config --get user.email");
                txtUserEmail.Text = result.Output.Trim();
                result = GitBash.Run("config --get credential.helper");
                var msg = string.IsNullOrWhiteSpace(result.Output.Trim()) ?
                    "Click here to install Windows Credential for Git" :
                    "You have installed git credential helper.";
                txtGitCredentialHelper.Inlines.Clear();
                txtGitCredentialHelper.Inlines.Add(msg);
                result = GitBash.Run("config --get merge.tool");
                msg = string.IsNullOrWhiteSpace(result.Output.Trim()) ?
                   "Git merge tool is not configured." :
                   "You have configured git merge tool to be: " + result.Output.Trim();
                txtGitMergeTool.Inlines.Clear();
                txtGitMergeTool.Inlines.Add(msg);
            }
            catch (Exception ex)
            {
                txtMessage.Content = ex.Message;
            }

            btnOK.IsEnabled = GitBash.Exists && txtMessage.Content != null
                && txtMessage.Content.ToString().StartsWith("git version");
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                MessageBox.Show("Please enter user name", "Error", MessageBoxButton.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUserEmail.Text))
            {
                MessageBox.Show("Please enter user email", "Error", MessageBoxButton.OK);
                return;
            }

            try
            {
                GitSccOptions.Current.GitBashPath = GitBash.GitExePath;
                GitSccOptions.Current.SaveConfig();

                if (this.tracker != null && this.tracker.IsGit)
                {
                    GitBash.Run("config user.name \"" + txtUserName.Text + "\"", tracker.WorkingDirectory);
                    GitBash.Run("config user.email " + txtUserEmail.Text, tracker.WorkingDirectory);
                }
                else
                {
                    GitBash.Run("config --global user.name \"" + txtUserName.Text + "\"");
                    GitBash.Run("config --global user.email " + txtUserEmail.Text);
                }
                Hide();
            }
            catch (Exception ex)
            {
                txtMessage.Content = ex.Message;
            }
        }

        internal void Show(GitRepository tracker)
        {
            this.tracker = tracker;
            this.Visibility = Visibility.Visible;
            txtGitExePath.Text = GitBash.GitExePath;
            btnOK.IsEnabled = false;
            txtGitExePath.Text = GitSccOptions.Current.GitBashPath;
            txtMessage.Content = "";
            CheckGitBash();
        }

        internal void Hide()
        {
            this.Visibility = Visibility.Hidden;
        }

        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            btnOK.IsEnabled = false;
            txtMessage.Content = "";
            CheckGitBash();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void txtGitExePath_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckGitBash();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}

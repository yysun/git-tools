using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GitScc;
using Microsoft.Windows.Shell;
using Mono.Options;
using Gitscc;
using System.Diagnostics;
using System.Text;

namespace GitUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private GitViewModel gitViewModel;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//this.Style = (Style)Resources["GradientStyle"];

            this.gitViewModel = GitViewModel.Current;
            this.gitConsole.GitExePath = GitBash.GitExePath;
            this.rootGrid.RowDefinitions[2].Height = new GridLength(this.ActualHeight / 3);
            this.gitConsole.mainWindow = this;

			if (gitViewModel.Tracker.IsGit)
				this.Title = gitViewModel.Tracker.WorkingDirectory;

            this.gitViewModel.GraphChanged += gitViewModel_GraphChanged;
            gitViewModel_GraphChanged(this, null);

            Action a1 = () =>
            {
                this.WindowState = WindowState.Maximized;
                this.console_height = (this.ActualHeight - 60) * 0.5;
            };
			this.Dispatcher.BeginInvoke(a1, DispatcherPriority.ApplicationIdle);

			var optionSet = new OptionSet()
			{
				{"c|commit", "show commit UI", v => {
					if (!string.IsNullOrWhiteSpace(v))
					{
						Action act = () => HistoryViewCommands.PendingChanges.Execute(null, this);
						this.Dispatcher.BeginInvoke(act, DispatcherPriority.ApplicationIdle);
					}
				} },
			};

			optionSet.Parse(Environment.GetCommandLineArgs());
		}

        void gitViewModel_GraphChanged(object sender, EventArgs e)
        {
            //Action a = () => loading.Visibility = Visibility.Visible;
            //this.Dispatcher.BeginInvoke(a, DispatcherPriority.Render);

            this.ShowStatusMessage("Analyzing Git Repository ...");

            Action act = () =>
            {

                gitViewModel.NoRefresh = true;

                this.topToolBar.GitViewModel = gitViewModel;
                //this.bottomToolBar.GitViewModel = GitViewModel.Current;

                this.txtRepo.Text = gitViewModel.Tracker.WorkingDirectory;
                this.txtPrompt.Text = "(Not a Git repository)";

                if (gitViewModel.Tracker.IsGit)
                {
                    this.txtPrompt.Text = gitViewModel.Tracker.ChangedFilesStatus;
                }

                this.Title = string.Format("{0} ({1})", this.txtRepo.Text, this.txtPrompt.Text);

                this.graph.Show(gitViewModel.Tracker, true);
                this.pendingChanges.Refresh(gitViewModel.Tracker);
                this.gitConsole.Refresh(gitViewModel.Tracker);

                //Action b = () => loading.Visibility = Visibility.Collapsed;
                //this.Dispatcher.BeginInvoke(b, DispatcherPriority.Render);

                //loading.Visibility = Visibility.Collapsed;
            };
            
            this.Dispatcher.BeginInvoke(act, DispatcherPriority.ApplicationIdle);
        }

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F5)
			{
				HistoryViewCommands.RefreshGraph.Execute(null, this);
			}
            else if (e.Key == Key.F2)
            {
                toogleGitConsole();
            }
		}

		private void ExportGraph(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new Microsoft.Win32.SaveFileDialog();
			dlg.DefaultExt = ".xps";
			dlg.Filter = "XPS documents (.xps)|*.xps";
			if (dlg.ShowDialog() == true)
			{
				this.graph.SaveToFile(dlg.FileName);
                Process.Start(dlg.FileName);
			}
		}

		#region show commit details

		private void ShowCommitDetails(string id)
		{
			if (id != null)
			{
				this.details.RenderTransform.SetValue(TranslateTransform.XProperty, this.ActualWidth);
				this.details.Visibility = Visibility.Visible;
				var animationDuration = TimeSpan.FromSeconds(.5);
				var animation = new DoubleAnimation(0, new Duration(animationDuration));
				animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

				loading.Visibility = Visibility.Visible;

				animation.Completed += (_, e) =>
				{
					this.details.Show(this.gitViewModel.Tracker, id);
					loading.Visibility = Visibility.Collapsed;
				};
				this.details.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
			}
		}

		private void OpenCommitDetails_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				ShowCommitDetails(e.Parameter as string);
			}
			catch (Exception ex)
			{
				Log.WriteLine("MainWindow.OpenCommitDetails_Executed: {0}", ex.ToString());
			}
		}

		private void CloseCommitDetails_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				if (e.Parameter == null)
				{
					var animationDuration = TimeSpan.FromSeconds(.2);
					var animation = new DoubleAnimation(this.ActualWidth + 200, new Duration(animationDuration));
					animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn };
					animation.Completed += (o, _) => this.details.Visibility = Visibility.Collapsed;
					this.details.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
				}
				else
				{
					var animationDuration = TimeSpan.FromSeconds(.2);
					var animation = new DoubleAnimation(-this.ActualWidth, new Duration(animationDuration));
					animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn };
					animation.Completed += (o, _) => this.pendingChanges.Visibility = Visibility.Collapsed;
					this.pendingChanges.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine("MainWindow.CloseCommitDetails_Executed: {0}", ex.ToString());
			}
		}

		private void ScrollToCommit_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				this.graph.ScrollToCommit(e.Parameter as string);
			}
			catch (Exception ex)
			{
				Log.WriteLine("MainWindow.ScrollToCommit_Executed: {0}", ex.ToString());
			}
		}

        private void GraphLoaded_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            gitViewModel.NoRefresh = false;
            loading.Visibility = Visibility.Collapsed;
        }

		private void RefreshGraph_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			gitViewModel.RefreshToolWindows();
		}

		private void ShowMessage_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			dynamic msg = e.Parameter;
			var ret = msg.GitBashResult as GitBashResult;
			if (ret == null) return;

            int max_lines = (int) this.ActualHeight / 40;
			var text = string.Format("{0} {1}", ret.Output, ret.Error);
            var lines = text.Split('\n');
            if (lines.Length > max_lines)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < max_lines-3; i++)
                {
                    sb.Append("\n" + lines[i]);
                }
                text = string.Format("{0}\n .......\n{1}\n{2}", 
                    sb.ToString(), lines[lines.Length - 2], lines[lines.Length - 1]);
            }

            txtMessage.Text = text;
			txtMessage.Foreground = new SolidColorBrush(
				ret.HasError ? Colors.Red : Colors.Navy);

			txtMessage.Visibility = Visibility.Visible;
			txtMessage.Opacity = 1.0;
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
                Duration = new Duration(ret.HasError ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(10)),
				From = 1.0,
				To = 0.0
			};
			doubleAnimation.Completed += (o, _) => txtMessage.Visibility = Visibility.Collapsed;
			txtMessage.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		}

		#endregion

		#region select and comapre commits

		private void SelectCommit_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.topToolBar.SelectCommit(e.Parameter as string, null);
		}

		private void CompareCommits_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				this.details.RenderTransform.SetValue(TranslateTransform.XProperty, this.ActualWidth);
				this.details.Visibility = Visibility.Visible;
				var animationDuration = TimeSpan.FromSeconds(.5);
				var animation = new DoubleAnimation(0, new Duration(animationDuration));
				animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

				loading.Visibility = Visibility.Visible;
				animation.Completed += (_, x) =>
				{
					var ids = e.Parameter as string[];
					this.details.Show(this.gitViewModel.Tracker, ids[0], ids[1]);
					loading.Visibility = Visibility.Collapsed;
				};

				this.details.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
			}
			catch (Exception ex)
			{
				Log.WriteLine("MainWindow.CompareCommits_Executed {0}", ex.ToString());
			}
		}

		private void PendingChanges_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				this.pendingChanges.RenderTransform.SetValue(TranslateTransform.XProperty, -this.ActualWidth);
				this.pendingChanges.Visibility = Visibility.Visible;
				var animationDuration = TimeSpan.FromSeconds(.5);
				var animation = new DoubleAnimation(0, new Duration(animationDuration));
				animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

				loading.Visibility = Visibility.Visible;
				animation.Completed += (_, x) =>
				{
					this.pendingChanges.Refresh(gitViewModel.Tracker);
					loading.Visibility = Visibility.Collapsed;
				};
				this.pendingChanges.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);

			}
			catch (Exception ex)
			{
				Log.WriteLine("MainWindow.PendingChanges_Executed {0}", ex.ToString());
			}
		}
		#endregion

		private void OpenRepository(string path)
		{
			HistoryViewCommands.CloseCommitDetails.Execute(null, this);
			HistoryViewCommands.CloseCommitDetails.Execute("PendingChanges", this);
			this.gitViewModel.Open(path);
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				this.Activate();
				var dropped = ((string[])e.Data.GetData(DataFormats.FileDrop, true))[0];
				if (!Directory.Exists(dropped)) dropped = Path.GetDirectoryName(dropped);
                if (Directory.Exists(dropped))
                {
                    this.OpenRepository(dropped);
                    if (!GitViewModel.Current.Tracker.IsGit && MessageBox.Show("There is no git repository found in " + dropped + 
                        "\r\n\r\n Do you want to initialize a new git repository?",
                    "Git repository NOT found", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        GitViewModel.Current.Init();
                    }
                }
			}
		}

		private void _OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
		{
            Microsoft.Windows.Shell.SystemCommands.CloseWindow(this);
		}

		private void OpenRepository_Executed(object sender, ExecutedRoutedEventArgs e)
		{
            using (var dialog = new System.Windows.Forms.OpenFileDialog { 
                Filter = "All Files|*.*", Title = "Open Git Repository", RestoreDirectory = true })
            {
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Folder Selection";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var folder = dialog.FileName.Replace("\\Folder Selection", "");
                    this.OpenRepository(folder);
                }
            }
		}

        #region toggle console
        double console_height;
        private void txtSettings_MouseUp(object sender, MouseButtonEventArgs e)
        {
            toogleGitConsole();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustConsoleHeight();
        }

        private void AdjustConsoleHeight()
        {
            var mh = this.ActualHeight / 2 - 30;
            var h = this.rootGrid.RowDefinitions[2].Height.Value;
            if (h> 0 && h > mh)
            {
                this.rootGrid.RowDefinitions[2].Height = new GridLength(mh);
                console_height = mh;
            }
        }

        private void toogleGitConsole()
        {
            var h = this.rootGrid.RowDefinitions[2].Height.Value;
            this.rootGrid.RowDefinitions[2].Height = h > 0 ?
                new GridLength(0) :
                new GridLength(console_height);
            console_height = h;
            AdjustConsoleHeight();
        }
        #endregion

        internal void ShowStatusMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                Action b = () =>
                {
                    loading.SetText("");
                    loading.Visibility = Visibility.Collapsed;
                };
                this.Dispatcher.BeginInvoke(b, DispatcherPriority.Render);
            }
            else
            {
                Action a = () =>
                {
                    loading.SetText(msg);
                    loading.Visibility = Visibility.Visible;
                };
                this.Dispatcher.BeginInvoke(a, DispatcherPriority.Render);
            }
        }
    }
}

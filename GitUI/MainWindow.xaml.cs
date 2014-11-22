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
			this.Style = (Style)Resources["GradientStyle"];

			//this.gitConsole.GitExePath = GitBash.GitExePath;
			//this.rootGrid.RowDefinitions[0].Height = new GridLength(this.ActualHeight - 60);

			this.gitViewModel = GitViewModel.Current;
			//this.bottomToolBar.GitViewModel = GitViewModel.Current;

			if (gitViewModel.Tracker.IsGit)
				this.Title = gitViewModel.Tracker.WorkingDirectory;

			this.gitViewModel.GraphChanged += (o, reload) =>
			{
				// show loading sign immediately
				////Action a = () => loading.Visibility = Visibility.Visible;
				////this.Dispatcher.BeginInvoke(a, DispatcherPriority.Render);

				loading.Visibility = Visibility.Visible;
				Action act = () =>
				{
					if (!gitViewModel.NoRefresh && gitViewModel.Tracker.IsGit)
					{
						this.txtRepo.Text = gitViewModel.Tracker.WorkingDirectory;

                        var changed = gitViewModel.Tracker.ChangedFiles;
                        var prompt = string.Format("{4}:  +{0} ~{1} -{2} !{3}",
                            changed.Where(f => f.Status == GitFileStatus.New || f.Status == GitFileStatus.Added).Count(),
                            changed.Where(f => f.Status == GitFileStatus.Modified || f.Status == GitFileStatus.Staged).Count(),
                            changed.Where(f => f.Status == GitFileStatus.Deleted || f.Status == GitFileStatus.Removed).Count(),
                            changed.Where(f => f.Status == GitFileStatus.Conflict).Count(),
                            gitViewModel.Tracker.CurrentBranch);

						this.txtPrompt.Text = prompt;
					}
					this.graph.Show(gitViewModel.Tracker, reload != null);
					this.pendingChanges.Refresh(gitViewModel.Tracker);
				};
				this.Dispatcher.BeginInvoke(act, DispatcherPriority.ApplicationIdle);
			};

			this.gitViewModel.Refresh(true);

			Action a1 = () => this.WindowState = WindowState.Maximized;
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

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F5)
			{
				HistoryViewCommands.RefreshGraph.Execute(null, this);
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
			gitViewModel.DisableAutoRefresh();

			this.loading.Visibility = Visibility.Collapsed;
			this.topToolBar.GitViewModel = gitViewModel;

			this.Title = gitViewModel.Tracker.IsGit ?
				string.Format("{0} ({1})", gitViewModel.Tracker.WorkingDirectory, gitViewModel.Tracker.CurrentBranch) :
				string.Format("{0} (No Repository)", gitViewModel.WorkingDirectory);

			gitViewModel.EnableAutoRefresh();
		}

		private void RefreshGraph_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			gitViewModel.Refresh(true);
		}

		private void ShowMessage_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			//dynamic msg = e.Parameter;
			//txtMessage.Text = msg.Message;
			//txtMessage.Foreground = new SolidColorBrush(
			//    msg.Error ? Colors.Red : Colors.Navy);

			dynamic msg = e.Parameter;
			var ret = msg.GitBashResult as GitBashResult;
			if (ret == null) return;

			txtMessage.Text = string.Format("{0} {1}", ret.Output, ret.Error);
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
			this.gitViewModel.Refresh(true);
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				this.Activate();

				var dropped = ((string[])e.Data.GetData(DataFormats.FileDrop, true))[0];

				if (!Directory.Exists(dropped)) dropped = Path.GetDirectoryName(dropped);

                var repo = new GitRepository(dropped);

                if (Directory.Exists(dropped) && repo.IsGit &&
                    MessageBox.Show("Do you want to open Git repository from " + repo.WorkingDirectory,
                    "Git repository found", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    this.OpenRepository(dropped);
                }
			}
		}

		private void _OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
		{
			SystemCommands.CloseWindow(this);
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
	}
}

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitScc;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject2019
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Custom Command Sample", "Shows how to hook up a command in VS", "1.0")]       
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("9C86573C-CB62-45D0-9C1A-DAD967BBBDC4")] // must match GUID in the .vsct file
    [ProvideToolWindow(typeof(GitChangesWindow))]
    public sealed class MyPackage : AsyncPackage
    {
        static EnvDTE80.DTE2 dte;

        // This method is run automatically the first time the command is being executed
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            //await MyCommand.InitializeAsync(this);

            #region commands
            var commandService = await GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Assumes.Present(commandService);

            CommandID toolwndCommandID = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.cmdidGitToolsWindow);
            var menuToolWin = new OleMenuCommand((s, e) => OpenToolWindow(this), toolwndCommandID);
            commandService.AddCommand(menuToolWin);


            CommandID cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitBash);
            OleMenuCommand menu = new OleMenuCommand(new EventHandler(OnGitBashCommand), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesRefresh);
            menu = new OleMenuCommand(new EventHandler(OnRefreshCommand), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandInit);
            menu = new OleMenuCommand(new EventHandler(OnInitCommand), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandEditIgnore);
            menu = new OleMenuCommand(new EventHandler(OnEditIgnore), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesCommit);
            menu = new OleMenuCommand(new EventHandler(OnCommitCommand), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitExtension);
            menu = new OleMenuCommand(new EventHandler(OnGitExtensionCommand), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitTortoise);
            menu = new OleMenuCommand(new EventHandler(OnTortoiseGitCommand), cmd);
            commandService.AddCommand(menu);


            for (int i = 0; i < GitToolCommands.GitExtCommands.Count; i++)
            {
                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdGitExtCommand1 + i);
                var mc = new OleMenuCommand(new EventHandler(OnGitExtCommandExec), cmd);
                mc.BeforeQueryStatus += new EventHandler(OnStatus);
                commandService.AddCommand(mc);
            }

            for (int i = 0; i < GitToolCommands.GitTorCommands.Count; i++)
            {
                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdGitTorCommand1 + i);
                var mc = new OleMenuCommand(new EventHandler(OnGitTorCommandExec), cmd);
                mc.BeforeQueryStatus += new EventHandler(OnStatus);
                commandService.AddCommand(mc);
            }

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandAbout);
            menu = new OleMenuCommand(new EventHandler(OnAbout), cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesSettings);
            menu = new OleMenuCommand(new EventHandler(OnSettings), cmd);
            commandService.AddCommand(menu);
            #endregion

        }

        private static string CurrentGitWorkingDirectory
        {
            get
            {
                EnvDTE.Properties properties = dte.get_Properties("Environment", "ProjectsAndSolution");
                EnvDTE.Property p = properties.Item("ProjectsLocation");
                return p.Value as string;
            }
        }



        #region open tool window
        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            return toolWindowType.Equals(Guid.Parse(GitChangesWindow.WindowGuidString)) ? this : null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            return toolWindowType == typeof(GitChangesWindow) ? GitChangesWindow.Title : base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            // Perform as much work as possible in this method which is being run on a background thread.
            // The object returned from this method is passed into the constructor of the GitChangesWindow 
            var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            return new GitChangesWindowState
            {
                DTE = dte
            };
        }
        #endregion

        #region command event handlers
        private static void RunDetatched(string cmd, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardInput = false;

                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.FileName = cmd;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = CurrentGitWorkingDirectory;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.StartInfo.LoadUserProfile = true;

                process.Start();
            }
        }

        private static void OnStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command != null)
            {
                var cmdId = command.CommandID.ID;
                if (cmdId >= PkgCmdIDList.icmdGitExtCommand1 &&
                    cmdId < PkgCmdIDList.icmdGitExtCommand1 + GitToolCommands.GitExtCommands.Count)
                {
                    int idx = cmdId - PkgCmdIDList.icmdGitExtCommand1;
                    command.Text = GitToolCommands.GitExtCommands[idx].Name;
                }
                else if (cmdId >= PkgCmdIDList.icmdGitTorCommand1 &&
                    cmdId < PkgCmdIDList.icmdGitTorCommand1 + GitToolCommands.GitTorCommands.Count)
                {
                    int idx = cmdId - PkgCmdIDList.icmdGitTorCommand1;
                    command.Text = GitToolCommands.GitTorCommands[idx].Name;
                }
            }
        }


        private static void OpenToolWindow(AsyncPackage package)
        {
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(
                    typeof(GitChangesWindow),
                    0,
                    create: true,
                    cancellationToken: package.DisposalToken);
            });
        }

        private void OnSettings(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnAbout(object sender, EventArgs e)
        {
            Process.Start("https://github.com/yysun/git-tools");
        }

        private static void OnGitBashCommand(object sender, EventArgs e)
        {
            var gitExePath = GitSccOptions.Current.GitBashPath;
            var gitBashPath = gitExePath.Replace("git.exe", "sh.exe");
            RunDetatched("cmd.exe", string.Format("/c \"{0}\" --login -i", gitBashPath));
        }

        private static void OnGitTorCommandExec(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (null != menuCommand)
            {
                int idx = menuCommand.CommandID.ID - PkgCmdIDList.icmdGitTorCommand1;
                var cmd = GitToolCommands.GitTorCommands[idx];
                var targetPath = CurrentGitWorkingDirectory;
                var tortoiseGitPath = GitSccOptions.Current.TortoiseGitPath;
                RunDetatched(tortoiseGitPath, cmd.Command + " /path:\"" + targetPath + "\"");
            }
        }

        private static void OnGitExtCommandExec(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (null != menuCommand)
            {
                int idx = menuCommand.CommandID.ID - PkgCmdIDList.icmdGitExtCommand1;
                var gitExtensionPath = GitSccOptions.Current.GitExtensionPath;
                RunDetatched(gitExtensionPath, GitToolCommands.GitExtCommands[idx].Command);
            }
        }

        private static void OnTortoiseGitCommand(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnCommitCommand(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnEditIgnore(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnInitCommand(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnGitExtensionCommand(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnRefreshCommand(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion



    }
}

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using GitScc;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Process = System.Diagnostics.Process;
using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject2019
{
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Git Tools 2019", "This extension provides a git changes window, and menus to launch Git Bash, Git Extenstions and TortoiseGit.", "3.1.1")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("9C86573C-CB62-45D0-9C1A-DAD967BBBDC4")] // must match GUID in the .vsct file
    [ProvideToolWindow(typeof(GitChangesWindow))]
    public sealed class MyPackage : AsyncPackage
    {
        static DTE2 dte;
        static string CurrentGitWorkingDirectory;
        internal GitTracker tracker;

        // This method is run automatically the first time the command is being executed
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            GitBash.GitExePath = GitSccOptions.Current.GitBashPath;
            GitBash.UseUTF8FileNames = !GitSccOptions.Current.NotUseUTF8FileNames;

            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            SolutionEvents.OnAfterOpenSolution += (o, e) => OpenRepository();
            SolutionEvents.OnAfterCloseSolution += (o, e) => CloseRepository();
            SolutionEvents.OnAfterOpenFolder += (o, e) => OpenRepository();
            SolutionEvents.OnAfterCloseFolder += (o, e) => CloseRepository();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(dte);
            if (isSolutionLoaded) OpenRepository();

            #region commands
            var commandService = await GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Assumes.Present(commandService);

            CommandID toolwndCommandID = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.cmdidGitToolsWindow);
            var menuToolWin = new OleMenuCommand((s, e) => OpenToolWindow(this), toolwndCommandID);
            commandService.AddCommand(menuToolWin);

            CommandID cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitBash);
            OleMenuCommand menu = new OleMenuCommand(OnGitBashCommand, cmd);
            menu.BeforeQueryStatus += (s, e) => {
                var command = s as OleMenuCommand;
                command.Enabled = File.Exists(GitSccOptions.Current.GitBashPath);
            };
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesRefresh);
            menu = new OleMenuCommand(OnRefreshCommand, cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandInit);
            menu = new OleMenuCommand(OnInitCommand, cmd);
            menu.BeforeQueryStatus += (s, e) => { ((OleMenuCommand)s).Visible = tracker!= null && !IsSolutionGitControlled;  };
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandEditIgnore);
            menu = new OleMenuCommand(OnEditIgnore, cmd);
            menu.BeforeQueryStatus += (s, e) => { ((OleMenuCommand)s).Visible = IsSolutionGitControlled; };
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesCommit);
            menu = new OleMenuCommand(OnCommitCommand, cmd);
            menu.BeforeQueryStatus += (s, e) => { ((OleMenuCommand)s).Visible = IsSolutionGitControlled; };
            commandService.AddCommand(menu);

            for (int i = 0; i < GitToolCommands.GitExtCommands.Count; i++)
            {
                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdGitExtCommand1 + i);
                var mc = new OleMenuCommand(OnGitExtCommandExec, cmd);
                mc.BeforeQueryStatus += (s, e) => {
                    var command = s as OleMenuCommand;
                    command.Text = GitToolCommands.GitExtCommands[command.CommandID.ID - PkgCmdIDList.icmdGitExtCommand1].Name;
                    command.Enabled = File.Exists(GitSccOptions.Current.GitExtensionPath);
                };
                commandService.AddCommand(mc);
            }

            for (int i = 0; i < GitToolCommands.GitTorCommands.Count; i++)
            {
                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdGitTorCommand1 + i);
                var mc = new OleMenuCommand(OnGitTorCommandExec, cmd);
                mc.BeforeQueryStatus += (s, e) => {
                    var command = s as OleMenuCommand;
                    command.Text = GitToolCommands.GitExtCommands[command.CommandID.ID - PkgCmdIDList.icmdGitTorCommand1].Name;
                    command.Enabled = File.Exists(GitSccOptions.Current.TortoiseGitPath);
                };
                commandService.AddCommand(mc);
            }

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandAbout);
            menu = new OleMenuCommand(OnAbout, cmd);
            commandService.AddCommand(menu);

            cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesSettings);
            menu = new OleMenuCommand(OnSettings, cmd);
            commandService.AddCommand(menu);
            #endregion

        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(solService);
            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
            return value is bool isSolOpen && isSolOpen;
        }

        private void OpenRepository()
        {
            var solutionFileName = dte.Solution?.FullName;

            if (!string.IsNullOrEmpty(solutionFileName))
            {
                CurrentGitWorkingDirectory = File.Exists(solutionFileName) ? Path.GetDirectoryName(solutionFileName) : solutionFileName;
                tracker = new GitTracker(CurrentGitWorkingDirectory);
                if (tracker.Repository.IsGit) CurrentGitWorkingDirectory = tracker.Repository.WorkingDirectory;
                tracker.Changed += (tracker) => _ = RefreshAsync(tracker);
                _ = RefreshAsync(tracker);
            }

            Debug.WriteLine("GT === Open repository: " + CurrentGitWorkingDirectory);
        }

        private void CloseRepository()
        {
            CurrentGitWorkingDirectory = "";
            tracker.Dispose();
            tracker = null;
            _ = RefreshAsync(tracker);
        }

        private GitChangesWindowControl FindMyControl()
        {
            var window = FindToolWindow(typeof(GitChangesWindow), 0, true) as GitChangesWindow;
            return window?.Content as GitChangesWindowControl;
        }

        private async Task RefreshAsync(GitTracker tracker)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                if (!GitTracker.NoRefresh) FindMyControl()?.Refresh(tracker);
                ((Commands2)dte.Commands).UpdateCommandUI(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Git Tools Refresh Exception:" + ex.ToString());
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
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            var oleServiceProvider = await GetServiceAsync(typeof(IServiceProvider)) as IServiceProvider;
            IVsInvisibleEditorManager invisibleEditorManager = await GetServiceAsync(typeof(SVsInvisibleEditorManager)) as IVsInvisibleEditorManager;
            var componentModel = GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            return new GitChangesWindowState
            {
                DTE = dte,
                AsyncPackage = this,
                OleServiceProvider = oleServiceProvider,
                InvisibleEditorManager = invisibleEditorManager,
                EditorAdapter = editorAdapter
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

        private bool IsSolutionGitControlled
        {
            get
            {
                var isGit = tracker?.Repository?.IsGit;
                return isGit == true;
            }
        }

        private void OpenToolWindow(AsyncPackage package)
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

        private void OnAbout(object sender, EventArgs e)
        {
            Process.Start("https://github.com/yysun/git-tools");
        }

        private void OnGitBashCommand(object sender, EventArgs e)
        {
            var gitExePath = GitSccOptions.Current.GitBashPath;
            var gitBashPath = gitExePath.Replace("git.exe", "sh.exe");
            RunDetatched("cmd.exe", string.Format("/c \"{0}\" --login -i", gitBashPath));
        }

        private void OnGitTorCommandExec(object sender, EventArgs e)
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

        private void OnGitExtCommandExec(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (null != menuCommand)
            {
                int idx = menuCommand.CommandID.ID - PkgCmdIDList.icmdGitExtCommand1;
                var gitExtensionPath = GitSccOptions.Current.GitExtensionPath;
                RunDetatched(gitExtensionPath, GitToolCommands.GitExtCommands[idx].Command);
            }
        }


        private void OnCommitCommand(object sender, EventArgs e)
        {
            FindMyControl()?.OnCommit();
        }

        private void OnSettings(object sender, EventArgs e)
        {
            FindMyControl()?.OnSettings();
        }

        private void OnInitCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentGitWorkingDirectory)) return;
            GitRepository.Init(CurrentGitWorkingDirectory);
            var ignoreFileName = Path.Combine(CurrentGitWorkingDirectory, ".gitignore");
            if (!File.Exists(ignoreFileName))
            {
                File.WriteAllText(ignoreFileName, IgnoreFileContent);
            }
        }

        private void OnEditIgnore(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentGitWorkingDirectory)) return;
            var fn = Path.Combine(CurrentGitWorkingDirectory, ".gitignore");
            if (!File.Exists(fn)) File.WriteAllText(fn, "# git ignore file");
            dte.ItemOperations.OpenFile(fn);
        }

        private void OnRefreshCommand(object sender, EventArgs e)
        {
            _ = RefreshAsync(tracker);
        }
        #endregion

        #region git ignore

        const string IgnoreFileContent = @"
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# Visual Studio 2015 cache/options directory
.vs/
# Uncomment if you have tasks that create the project's static files in wwwroot
#wwwroot/

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

# NUNIT
*.VisualState.xml
TestResult.xml

# Build Results of an ATL Project
[Dd]ebugPS/
[Rr]eleasePS/
dlldata.c

# DNX
project.lock.json
project.fragment.lock.json
artifacts/

*_i.c
*_p.c
*_i.h
*.ilk
*.meta
*.obj
*.pch
*.pdb
*.pgc
*.pgd
*.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# Chutzpah Test files
_Chutzpah*

# Visual C++ cache files
ipch/
*.aps
*.ncb
*.opendb
*.opensdf
*.sdf
*.cachefile
*.VC.db
*.VC.VC.opendb

# Visual Studio profiler
*.psess
*.vsp
*.vspx
*.sap

# TFS 2012 Local Workspace
$tf/

# Guidance Automation Toolkit
*.gpState

# ReSharper is a .NET coding add-in
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# JustCode is a .NET coding add-in
.JustCode

# TeamCity is a build add-in
_TeamCity*

# DotCover is a Code Coverage Tool
*.dotCover

# NCrunch
_NCrunch_*
.*crunch*.local.xml
nCrunchTemp_*

# MightyMoose
*.mm.*
AutoTest.Net/

# Web workbench (sass)
.sass-cache/

# Installshield output folder
[Ee]xpress/

# DocProject is a documentation generator add-in
DocProject/buildhelp/
DocProject/Help/*.HxT
DocProject/Help/*.HxC
DocProject/Help/*.hhc
DocProject/Help/*.hhk
DocProject/Help/*.hhp
DocProject/Help/Html2
DocProject/Help/html

# Click-Once directory
publish/

# Publish Web Output
*.[Pp]ublish.xml
*.azurePubxml
# TODO: Comment the next line if you want to checkin your web deploy settings
# but database connection strings (with potential passwords) will be unencrypted
*.pubxml
*.publishproj

# Microsoft Azure Web App publish settings. Comment the next line if you want to
# checkin your Azure Web App publish settings, but sensitive information contained
# in these scripts will be unencrypted
PublishScripts/

# NuGet Packages
*.nupkg
# The packages folder can be ignored because of Package Restore
**/packages/*
# except build/, which is used as an MSBuild target.
!**/packages/build/
# Uncomment if necessary however generally it will be regenerated when needed
#!**/packages/repositories.config
# NuGet v3's project.json files produces more ignoreable files
*.nuget.props
*.nuget.targets

# Microsoft Azure Build Output
csx/
*.build.csdef

# Microsoft Azure Emulator
ecf/
rcf/

# Windows Store app package directories and files
AppPackages/
BundleArtifacts/
Package.StoreAssociation.xml
_pkginfo.txt

# Visual Studio cache files
# files ending in .cache can be ignored
*.[Cc]ache
# but keep track of directories ending in .cache
!*.[Cc]ache/

# Others
ClientBin/
~$*
*~
*.dbmdl
*.dbproj.schemaview
*.pfx
*.publishsettings
node_modules/
orleans.codegen.cs

# Since there are multiple workflows, uncomment next line to ignore bower_components
# (https://github.com/github/gitignore/pull/1529#issuecomment-104372622)
#bower_components/

# RIA/Silverlight projects
Generated_Code/

# Backup & report files from converting an old project file
# to a newer Visual Studio version. Backup files are not needed,
# because we have git ;-)
_UpgradeReport_Files/
Backup*/
UpgradeLog*.XML
UpgradeLog*.htm

# SQL Server files
*.mdf
*.ldf

# Business Intelligence projects
*.rdl.data
*.bim.layout
*.bim_*.settings

# Microsoft Fakes
FakesAssemblies/

# GhostDoc plugin setting file
*.GhostDoc.xml

# Node.js Tools for Visual Studio
.ntvs_analysis.dat

# Visual Studio 6 build log
*.plg

# Visual Studio 6 workspace options file
*.opt

# Visual Studio LightSwitch build output
**/*.HTMLClient/GeneratedArtifacts
**/*.DesktopClient/GeneratedArtifacts
**/*.DesktopClient/ModelManifest.xml
**/*.Server/GeneratedArtifacts
**/*.Server/ModelManifest.xml
_Pvt_Extensions

# Paket dependency manager
.paket/paket.exe
paket-files/

# FAKE - F# Make
.fake/

# JetBrains Rider
.idea/
*.sln.iml
";

        #endregion
    }
}

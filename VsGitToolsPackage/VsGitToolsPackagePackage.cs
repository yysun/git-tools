using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Reflection;
using GitScc;

namespace F1SYS.VsGitToolsPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#100", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    [Guid(GuidList.guidVsGitToolsPackagePkgString)]
    public sealed class VsGitToolsPackagePackage : Package, IOleCommandTarget
    {
        //private SccOnIdleEvent _OnIdleEvent = new SccOnIdleEvent();
        private VsGitToolsService service;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VsGitToolsPackagePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            GitBash.GitExePath = GitSccOptions.Current.GitBashPath;
            GitBash.UseUTF8FileNames = !GitSccOptions.Current.NotUseUTF8FileNames;
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            service = new VsGitToolsService(this);

            ((IServiceContainer)this).AddService(typeof(VsGitToolsService), service, true);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, (int)PkgCmdIDList.cmdidGitToolsWindow);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);

                CommandID cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitBash);
                MenuCommand menu = new MenuCommand(new EventHandler(OnGitBashCommand), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesRefresh);
                menu = new MenuCommand(new EventHandler(OnRefreshCommand), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitExtension);
                menu = new MenuCommand(new EventHandler(OnGitExtensionCommand), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandInit);
                menu = new MenuCommand(new EventHandler(OnInitCommand), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandEditIgnore);
                menu = new MenuCommand(new EventHandler(OnEditIgnore), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesCommit);
                menu = new MenuCommand(new EventHandler(OnCommitCommand), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandHistory);
                menu = new MenuCommand(new EventHandler(ShowHistoryWindow), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdPendingChangesSettings);
                menu = new MenuCommand(new EventHandler(OnSettings), cmd);
                mcs.AddCommand(menu);

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandGitTortoise);
                menu = new MenuCommand(new EventHandler(OnTortoiseGitCommand), cmd);

                mcs.AddCommand(menu);
                for (int i = 0; i < GitToolCommands.GitExtCommands.Count; i++)
                {
                    cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdGitExtCommand1 + i);
                    var mc = new MenuCommand(new EventHandler(OnGitExtCommandExec), cmd);
                    mcs.AddCommand(mc);
                }

                for (int i = 0; i < GitToolCommands.GitTorCommands.Count; i++)
                {
                    cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdGitTorCommand1 + i);
                    var mc = new MenuCommand(new EventHandler(OnGitTorCommandExec), cmd);
                    mcs.AddCommand(mc);
                }

                cmd = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.icmdSccCommandAbout);
                menu = new MenuCommand(new EventHandler(OnAbout), cmd);
                mcs.AddCommand(menu);

            }

            //_OnIdleEvent.RegisterForIdleTimeCallbacks(GetGlobalService(typeof(SOleComponentManager)) as IOleComponentManager);
            //_OnIdleEvent.OnIdleEvent += new OnIdleEvent(service.OnIdle);
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine(String.Format(CultureInfo.CurrentUICulture, "Entering Dispose() of: {0}", this.ToString()));

            //_OnIdleEvent.OnIdleEvent -= new OnIdleEvent(service.OnIdle);
            //_OnIdleEvent.UnRegisterForIdleTimeCallbacks();

            base.Dispose(disposing);
            service.Dispose();
        }
        #endregion

        #region menu commands

        private void OnRefreshCommand(object sender, EventArgs e)
        {
            GetToolWindowPane<MyToolWindow>().Refresh(true);
        }

        private void OnInitCommand(object sender, EventArgs e)
        {
            GitRepository.Init(repository.WorkingDirectory);
            var ignoreFileName = Path.Combine(repository.WorkingDirectory, ".gitignore");
            if (!File.Exists(ignoreFileName))
            {
                File.WriteAllText(ignoreFileName, Resources.IgnoreFileContent);
            }
            repository.Refresh();
        }

        private void OnGitBashCommand(object sender, EventArgs e)
        {
            GitBash.OpenGitBash(this.CurrentGitWorkingDirectory);
        }

        private void OnGitExtensionCommand(object sender, EventArgs e)
        {
            var gitExtensionPath = GitSccOptions.Current.GitExtensionPath;
            RunDetatched(gitExtensionPath, "");
        }

        private void OnTortoiseGitCommand(object sender, EventArgs e)
        {
            var tortoiseGitPath = GitSccOptions.Current.TortoiseGitPath;
            RunDetatched(tortoiseGitPath, "/command:log");
        }

        private void OnGitTorCommandExec(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (null != menuCommand)
            {
                int idx = menuCommand.CommandID.ID - PkgCmdIDList.icmdGitTorCommand1;

                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture,
                                  "Run GitTor Command {0}", GitToolCommands.GitTorCommands[idx].Command));

                var cmd = GitToolCommands.GitTorCommands[idx];
                var targetPath = this.CurrentGitWorkingDirectory;
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
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture,
                                  "Run GitExt Command {0}", GitToolCommands.GitExtCommands[idx].Command));

                var gitExtensionPath = GitSccOptions.Current.GitExtensionPath;
                RunDetatched(gitExtensionPath, GitToolCommands.GitExtCommands[idx].Command);
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
            //var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //path = Path.Combine(path, "Readme.html");
            Process.Start("http://yysun.github.io/git-tools");
        }

        private void OnEditIgnore(object sender, EventArgs e)
        {
            if (this.repository != null)
            {
                var dte = GetServiceEx<EnvDTE.DTE>() as EnvDTE.DTE;
                var fn = Path.Combine(CurrentGitWorkingDirectory, ".gitignore");
                if (!File.Exists(fn)) File.WriteAllText(fn, "# git ignore file");
                dte.ItemOperations.OpenFile(fn);
            }
        }

        private void OnCommitCommand(object sender, EventArgs e)
        {
            GetToolWindowPane<MyToolWindow>().OnCommitCommand();
        }

        private void OnSettings(object sender, EventArgs e)
        {
            GetToolWindowPane<MyToolWindow>().OnSettings();
        }

        private T GetToolWindowPane<T>() where T : ToolWindowPane
        {
            return (T)this.FindToolWindow(typeof(T), 0, true);
        }

        private void ShowHistoryWindow(object sender, EventArgs e)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "Resources\\Dragon.exe");
            var tmpPath = Path.Combine(Path.GetTempPath(), "Dragon.exe");

            var needCopy = !File.Exists(tmpPath);
            if (!needCopy)
            {
                var date1 = File.GetLastWriteTimeUtc(path);
                var date2 = File.GetLastWriteTimeUtc(tmpPath);
                needCopy = (date1 > date2);
            }

            if (needCopy)
            {
                try
                {
                    File.Copy(path, tmpPath, true);
                }
                catch // try copy file silently
                {
                }
            }

            if (File.Exists(tmpPath))
            {
                Process.Start(tmpPath, "\"" + repository.WorkingDirectory + "\"");
            }
        }

        #endregion

        #region IOleCommandTarget
        int IOleCommandTarget.QueryStatus(ref Guid guidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            Debug.Assert(cCmds == 1, "Multiple commands");
            Debug.Assert(prgCmds != null, "NULL argument");

            if ((prgCmds == null)) return VSConstants.E_INVALIDARG;

            // Filter out commands that are not defined by this package
            if (guidCmdGroup != GuidList.guidVsGitToolsPackageCmdSet)
            {
                return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
            }

            OLECMDF cmdf = OLECMDF.OLECMDF_SUPPORTED;
            //cmdf = cmdf | OLECMDF.OLECMDF_INVISIBLE;
            cmdf = cmdf & ~(OLECMDF.OLECMDF_ENABLED);

            // Process our Commands
            switch (prgCmds[0].cmdID)
            {

                case PkgCmdIDList.icmdSccCommandGitBash:
                    if (GitBash.Exists) cmdf |= OLECMDF.OLECMDF_ENABLED;
                    break;

                case PkgCmdIDList.icmdSccCommandGitExtension:
                    var gitExtensionPath = GitSccOptions.Current.GitExtensionPath;
                    if (!string.IsNullOrEmpty(gitExtensionPath) && File.Exists(gitExtensionPath))
                    {
                        cmdf |= OLECMDF.OLECMDF_ENABLED;
                    }
                    break;

                case PkgCmdIDList.icmdSccCommandGitTortoise:
                    var tortoiseGitPath = GitSccOptions.Current.TortoiseGitPath;
                    if (!string.IsNullOrEmpty(tortoiseGitPath) && File.Exists(tortoiseGitPath))
                    {
                        cmdf |= OLECMDF.OLECMDF_ENABLED;
                    }
                    break;

                case PkgCmdIDList.icmdSccCommandEditIgnore:
                    if (IsSolutionGitControlled) cmdf |= OLECMDF.OLECMDF_ENABLED;
                    else cmdf = cmdf | OLECMDF.OLECMDF_INVISIBLE;
                    break;

                case PkgCmdIDList.icmdSccCommandHistory:
                case PkgCmdIDList.icmdSccCommandPendingChanges:
                case PkgCmdIDList.icmdPendingChangesAmend:
                case PkgCmdIDList.icmdPendingChangesCommit:
                case PkgCmdIDList.icmdPendingChangesCommitToBranch:
                    if (GitBash.Exists && IsSolutionGitControlled) cmdf |= OLECMDF.OLECMDF_ENABLED;
                    else cmdf = cmdf | OLECMDF.OLECMDF_INVISIBLE;
                    break;

                case PkgCmdIDList.icmdSccCommandAbout:
                case PkgCmdIDList.icmdPendingChangesRefresh:
                case PkgCmdIDList.icmdPendingChangesSettings:
                    cmdf |= OLECMDF.OLECMDF_ENABLED;
                    break;

                case PkgCmdIDList.icmdSccCommandInit:
                    if (repository!=null && !IsSolutionGitControlled)
                        cmdf |= OLECMDF.OLECMDF_ENABLED;
                    else
                        //cmdf = cmdf & ~(OLECMDF.OLECMDF_ENABLED);
                        cmdf = cmdf | OLECMDF.OLECMDF_INVISIBLE;
                    break;

                default:
                    var gitExtPath = GitSccOptions.Current.GitExtensionPath;
                    var torGitPath = GitSccOptions.Current.TortoiseGitPath;
                    if (prgCmds[0].cmdID >= PkgCmdIDList.icmdGitExtCommand1 &&
                        prgCmds[0].cmdID < PkgCmdIDList.icmdGitExtCommand1 + GitToolCommands.GitExtCommands.Count &&
                        !string.IsNullOrEmpty(gitExtPath) && File.Exists(gitExtPath))
                    {
                        int idx = (int)prgCmds[0].cmdID - PkgCmdIDList.icmdGitExtCommand1;
                        SetOleCmdText(pCmdText, GitToolCommands.GitExtCommands[idx].Name);
                        cmdf |= OLECMDF.OLECMDF_ENABLED;
                        break;
                    }
                    else if (prgCmds[0].cmdID >= PkgCmdIDList.icmdGitTorCommand1 &&
                        prgCmds[0].cmdID < PkgCmdIDList.icmdGitTorCommand1 + GitToolCommands.GitTorCommands.Count &&
                        !string.IsNullOrEmpty(torGitPath) && File.Exists(torGitPath))
                    {
                        int idx = (int)prgCmds[0].cmdID - PkgCmdIDList.icmdGitTorCommand1;
                        SetOleCmdText(pCmdText, GitToolCommands.GitTorCommands[idx].Name);
                        cmdf |= OLECMDF.OLECMDF_ENABLED;
                        break;
                    }
                    else
                        return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
            }

            prgCmds[0].cmdf = (uint)(cmdf);
            return VSConstants.S_OK;
        }

        public void SetOleCmdText(IntPtr pCmdText, string text)
        {
            OLECMDTEXT CmdText = (OLECMDTEXT)Marshal.PtrToStructure(pCmdText, typeof(OLECMDTEXT));
            char[] buffer = text.ToCharArray();
            IntPtr pText = (IntPtr)((long)pCmdText + (long)Marshal.OffsetOf(typeof(OLECMDTEXT), "rgwz"));
            IntPtr pCwActual = (IntPtr)((long)pCmdText + (long)Marshal.OffsetOf(typeof(OLECMDTEXT), "cwActual"));
            // The max chars we copy is our string, or one less than the buffer size,
            // since we need a null at the end.
            int maxChars = (int)Math.Min(CmdText.cwBuf - 1, buffer.Length);
            Marshal.Copy(buffer, 0, pText, maxChars);
            // append a null
            Marshal.WriteInt16((IntPtr)((long)pText + (long)maxChars * 2), (Int16)0);
            // write out the length + null char
            Marshal.WriteInt32(pCwActual, maxChars + 1);
        }
        #endregion

        #region Run Command
        internal void RunDetatched(string cmd, string arguments)
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
        #endregion

        public object GetServiceEx<T>() 
        {
            return GetService(typeof(T));
        }

        private GitRepository repository { get {return service.Repository;} }

        internal bool IsSolutionGitControlled
        {
            get
            {
                return repository == null ? false : repository.IsGit;
            }
        }

        internal string CurrentGitWorkingDirectory
        {
            get
            {
                if (repository == null)
                {
                    var dte = GetServiceEx<EnvDTE.DTE>() as EnvDTE.DTE;
                    EnvDTE.Properties properties = dte.get_Properties("Environment", "ProjectsAndSolution"); 
                    EnvDTE.Property p = properties.Item("ProjectsLocation");
                    return p.Value as string;
                }
                else
                    return repository.WorkingDirectory;
            }
        }
    }
}


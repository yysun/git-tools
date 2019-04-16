namespace VSIXProject2019
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid(WindowGuidString)]
    public class GitChangesWindow : ToolWindowPane
    {
        public const string WindowGuidString = "e0487501-8bf2-4e94-8b35-ceb6f0010c44"; // Replace with new GUID in your own code
        public const string Title = "Git Changes Window";

        /// <summary>
        /// Initializes a new instance of the <see cref="GitChangesWindow"/> class.
        /// </summary>
        public GitChangesWindow(GitChangesWindowState state) : base()
        {
            this.Caption = "Git Changes";
            this.ToolBar = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.imnuGitChangesToolWindowToolbarMenu);
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new GitChangesWindowControl();
        }
    }

    public class GitChangesWindowState
    {
        public EnvDTE80.DTE2 DTE { get; set; }
    }
}

namespace VSIXProject2019
{
    using EnvDTE80;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Controls;
    using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

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
    public class GitChangesWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        // This will actually be defined as _codewindowbehaviorflags2.CWB_DISABLEDIFF once the latest version of
        // Microsoft.VisualStudio.TextManager.Interop.16.0.DesignTime is published. Setting the flag will have no effect
        // on releases prior to d16.0.
        const _codewindowbehaviorflags CWB_DISABLEDIFF = (_codewindowbehaviorflags)0x04;

        public EnvDTE80.DTE2 DTE;

        internal MyPackage AsyncPackage;
        private IServiceProvider OleServiceProvider;
        private IVsInvisibleEditorManager InvisibleEditorManager;
        private IVsEditorAdaptersFactoryService EditorAdapter;
        private IVsInvisibleEditor invisibleEditor;
        private IVsCodeWindow codeWindow;
        internal IVsTextView textView;

        public const string WindowGuidString = "e0487501-8bf2-4e94-8b35-ceb6f0010c44"; // Replace with new GUID in your own code
        public const string Title = "Git Changes Window";

        private ContentControl DiffEditor;
        private string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitChangesWindow"/> class.
        /// </summary>
        public GitChangesWindow(GitChangesWindowState state) : base()
        {
            this.Caption = "Git Changes";
            this.ToolBar = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.imnuGitChangesToolWindowToolbarMenu);
            this.DTE = state.DTE;
            this.AsyncPackage = state.AsyncPackage;
            this.OleServiceProvider = state.OleServiceProvider;
            this.InvisibleEditorManager = state.InvisibleEditorManager;
            this.EditorAdapter = state.EditorAdapter;
            this.Content = new GitChangesWindowControl(this);
            this.DiffEditor = ((GitChangesWindowControl)Content).DiffEditor;
        }

        internal IVsDifferenceService DiffService
        {
            get
            {
                return (IVsDifferenceService) this.GetService(typeof(SVsDifferenceService));
            }
        }

        #region vs editor

        internal IVsTextView SetDisplayedFile(string filePath)
        {
            if (this.filePath == filePath) return textView;
            this.filePath = filePath;

            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return CreateEditor(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Cleans up an existing editor if we are about to put a new one in place, used to close down the old editor bits as well as
        /// nulling out any cached objects that we have that came from the now dead editor.
        /// </summary>
        internal void ClearEditor()
        {
            filePath = "";
            if (this.codeWindow != null)
            {
                this.codeWindow.Close();
                this.codeWindow = null;
            }

            if (this.textView != null)
            {
                this.textView.CloseView();
                this.textView = null;
            }
            this.invisibleEditor = null;
        }
        #endregion


        public IVsTextView CreateEditor(string filePath)
        {
            //IVsInvisibleEditors are in-memory represenations of typical Visual Studio editors.
            //Language services, highlighting and error squiggles are hooked up to these editors
            //for us once we convert them to WpfTextViews. 
            ErrorHandler.ThrowOnFailure(this.InvisibleEditorManager.RegisterInvisibleEditor(
                filePath
                , pProject: null
                , dwFlags: (uint)_EDITORREGFLAGS.RIEF_ENABLECACHING
                , pFactory: null
                , ppEditor: out invisibleEditor));

            var docDataPointer = IntPtr.Zero;
            Guid guidIVsTextLines = typeof(IVsTextLines).GUID;

            ErrorHandler.ThrowOnFailure(invisibleEditor.GetDocData(
                fEnsureWritable: 1
                , riid: ref guidIVsTextLines
                , ppDocData: out docDataPointer));

            IVsTextLines docData = (IVsTextLines)Marshal.GetObjectForIUnknown(docDataPointer);

            //Create a code window adapter
            codeWindow = EditorAdapter.CreateVsCodeWindowAdapter(OleServiceProvider);

            // You need to disable the dropdown, splitter and -- for d16.0 -- diff since you are extracting the code window's TextViewHost and using it.
            ((IVsCodeWindowEx)codeWindow).Initialize((uint)_codewindowbehaviorflags.CWB_DISABLESPLITTER | (uint)_codewindowbehaviorflags.CWB_DISABLEDROPDOWNBAR | (uint)CWB_DISABLEDIFF,
                                                     VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter,
                                                     string.Empty,
                                                     string.Empty,
                                                     0,
                                                     new INITVIEW[1]);

            ErrorHandler.ThrowOnFailure(codeWindow.SetBuffer(docData));

            ErrorHandler.ThrowOnFailure(codeWindow.GetPrimaryView(out textView));

            var textViewHost = EditorAdapter.GetWpfTextViewHost(textView);
            textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.ChangeTrackingId, false);
            textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
            textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);
            DiffEditor.Content = textViewHost.HostControl;

            return textView;
        }

        #region IVsWindowFrameNotify3
        public int OnShow(int fShow)
        {
            if (fShow == 1)
            {
                if (GitTracker.NoRefresh)
                {
                    AsyncPackage.tracker?.Repository?.Refresh();
                }
                GitTracker.NoRefresh = false;
                ((GitChangesWindowControl)Content).Refresh(AsyncPackage.tracker);
                ((Commands2)DTE.Commands).UpdateCommandUI(true);
            }
            else if(fShow == 0)
            {
                GitTracker.NoRefresh = true;
            }
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnSize(int x, int y, int w, int h)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnDockableChange(int fDockable, int x, int y, int w, int h)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        #endregion
    }


    public class GitChangesWindowState
    {
        public EnvDTE80.DTE2 DTE { get; set; }
        public MyPackage AsyncPackage { get; set; }
        public IServiceProvider OleServiceProvider { get; set; }
        public IVsInvisibleEditorManager InvisibleEditorManager { get; set; }
        public IVsEditorAdaptersFactoryService EditorAdapter { get; set; }
    }
}

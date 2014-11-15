// This sample has been created from the sample on MSDN code gallery, originally 
// created by Chris Granger (http://code.msdn.microsoft.com/EditorToolwindow)

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.VisualStudio;


namespace F1SYS.VsGitToolsPackage
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("11dffb59-3169-48ac-9676-2916d06a36de")]
    public class MyToolWindow : ToolWindowPane, IOleCommandTarget
    {
        IVsTextView _ViewAdapter;
        IVsTextBuffer _BufferAdapter;
        IVsEditorAdaptersFactoryService _EditorAdapterFactory;
        IServiceProvider _OleServiceProvider;
        ITextBufferFactoryService _BufferFactory;
        IWpfTextViewHost _TextViewHost;
        private MyControl _Control;
    
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowTitle;

            //// set the CommandID for the window ToolBar
            base.ToolBar = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, PkgCmdIDList.imnuGitChangesToolWindowToolbarMenu);

            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            base.Content = new MyControl(this);
        }

        internal void OnCommitCommand()
        {
            if (!hasFileSaved()) return;
            //_Control.Commit();
        }

        internal void OnSettings()
        {
           _Control.OnSettings();
        }

        internal void Refresh(VsGitToolsService vsGitToolsService, GitRepository gitRepository)
        {
            Debug.WriteLine("VS Git Tools - Refresh Git Changes Tool Windows ");

            hasFileSaved(); //just a reminder, refresh anyway
            _Control.Refresh(vsGitToolsService, gitRepository);
        }

        internal EnvDTE.DTE dte
        {
            get 
            {
                return this.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            }
        }

        internal bool hasFileSaved()
        {
           return dte.ItemOperations.PromptToSave != EnvDTE.vsPromptResult.vsPromptResultCancelled;
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Get the content of the window pane: an instance of the editor to be embedded into
        /// the pane.
        /// </summary>
        // ----------------------------------------------------------------------------------
        override public object Content
        {
            get
            {
                if (_Control == null)
                {
                    _Control = new MyControl(this);
                    _Control.DiffEditor.Content = TextViewHost;
                }
                return _Control;
            }
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Turns on or off the read-only mode of the editor.
        /// </summary>
        // ----------------------------------------------------------------------------------
        public void SetReadOnly(bool isReadOnly)
        {
            uint flags;
            _BufferAdapter.GetStateFlags(out flags);
            var newFlags = isReadOnly
                             ? flags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY
                             : flags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
            _BufferAdapter.SetStateFlags(newFlags);
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Initialize the editor
        /// </summary>
        // ----------------------------------------------------------------------------------
        private void InitializeEditor()
        {
            const string message = "";

            var componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(
              typeof(SComponentModel));
            _OleServiceProvider = (IServiceProvider)GetService(typeof(IServiceProvider));
            _BufferFactory = componentModel.GetService<ITextBufferFactoryService>();

            _EditorAdapterFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _BufferAdapter = _EditorAdapterFactory.CreateVsTextBufferAdapter(_OleServiceProvider,
                                                                             _BufferFactory.TextContentType);
            _BufferAdapter.InitializeContent(message, message.Length);

            _ViewAdapter = _EditorAdapterFactory.CreateVsTextViewAdapter(_OleServiceProvider);
            ((IVsWindowPane)_ViewAdapter).SetSite(_OleServiceProvider);

            var initView = new[] { new INITVIEW() };
            initView[0].fSelectionMargin = 0; // original: 0
            initView[0].fWidgetMargin = 0; // original: 0
            initView[0].fVirtualSpace = 0;
            initView[0].fDragDropMove = 1;
            initView[0].fVirtualSpace = 0;

            _ViewAdapter.Initialize(_BufferAdapter as IVsTextLines, IntPtr.Zero,
              (uint)TextViewInitFlags.VIF_HSCROLL |
              (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT, initView);
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Gets the editor wpf host that we can use as the tool windows content.
        /// </summary>
        // ----------------------------------------------------------------------------------
        public IWpfTextViewHost TextViewHost
        {
            get
            {
                if (_TextViewHost == null)
                {
                    InitializeEditor();
                    var data = _ViewAdapter as IVsUserData;
                    if (data != null)
                    {
                        var guid = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                        object obj;
                        var hr = data.GetData(ref guid, out obj);
                        if ((hr == Microsoft.VisualStudio.VSConstants.S_OK) &&
                            obj != null && obj is IWpfTextViewHost)
                        {
                            _TextViewHost = obj as IWpfTextViewHost;
                        }
                    }
                }
                return _TextViewHost;
            }
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Register key bindings to use in the editor.
        /// </summary>
        // ----------------------------------------------------------------------------------
        public override void OnToolWindowCreated()
        {
            // --- Register key bindings to use in the editor
            var windowFrame = (IVsWindowFrame)Frame;
            var cmdUi = Microsoft.VisualStudio.VSConstants.GUID_TextEditorFactory;
            windowFrame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref cmdUi);
            base.OnToolWindowCreated();
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Allow the embedded editor to handle keyboard messages before they are dispatched
        /// to the window that has the focus.
        /// </summary>
        // ----------------------------------------------------------------------------------
        protected override bool PreProcessMessage(ref Message m)
        {
            if (TextViewHost != null)
            {
                // copy the Message into a MSG[] array, so we can pass
                // it along to the active core editor's IVsWindowPane.TranslateAccelerator
                var pMsg = new MSG[1];
                pMsg[0].hwnd = m.HWnd;
                pMsg[0].message = (uint)m.Msg;
                pMsg[0].wParam = m.WParam;
                pMsg[0].lParam = m.LParam;

                var vsWindowPane = (IVsWindowPane)_ViewAdapter;
                return vsWindowPane.TranslateAccelerator(pMsg) == 0;
            }
            return base.PreProcessMessage(ref m);
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Forwards command execution messages recevived by the window pane to the embedded
        /// editor.
        /// </summary>
        // ----------------------------------------------------------------------------------
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt,
                                   IntPtr pvaIn, IntPtr pvaOut)
        {
            var hr = (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
            if (_ViewAdapter != null)
            {
                var cmdTarget = (IOleCommandTarget)_ViewAdapter;
                hr = cmdTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            return hr;
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// Forwards command status query messages recevived by the window pane to the 
        /// embedded editor.
        /// </summary>
        // ----------------------------------------------------------------------------------
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds,
                                          IntPtr pCmdText)
        {
            var hr = (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
            if (_ViewAdapter != null)
            {
                var cmdTarget = (IOleCommandTarget)_ViewAdapter;
                hr = cmdTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
            return hr;
        }

        internal void SetText(string message)
        {
            _BufferAdapter.InitializeContent(message, message.Length);
        }


        private IOleCommandTarget cachedEditorCommandTarget;
        private IVsTextView textView;
        private IVsCodeWindow codeWindow;
        private IVsInvisibleEditor invisibleEditor;
        private IVsFindTarget cachedEditorFindTarget;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider cachedOleServiceProvider;

        public Tuple<System.Windows.Controls.Control, IVsTextView> SetDisplayedFile(string filePath)
        {
            //ClearEditor();
            try
            {
                //Get an invisible editor over the file, this makes it much easier than having to manually figure out the right content type, 
                //language service, and it will automatically associate the document with its owning project, meaning we will get intellisense
                //in our editor with no extra work.
                IVsInvisibleEditorManager invisibleEditorManager = (IVsInvisibleEditorManager)GetService(typeof(SVsInvisibleEditorManager));
                ErrorHandler.ThrowOnFailure(invisibleEditorManager.RegisterInvisibleEditor(filePath,
                                                                                           pProject: null,
                                                                                           dwFlags: (uint)_EDITORREGFLAGS.RIEF_ENABLECACHING,
                                                                                           pFactory: null,
                                                                                           ppEditor: out this.invisibleEditor));

                //The doc data is the IVsTextLines that represents the in-memory version of the file we opened in our invisibe editor, we need
                //to extract that so that we can create our real (visible) editor.
                IntPtr docDataPointer = IntPtr.Zero;
                Guid guidIVSTextLines = typeof(IVsTextLines).GUID;
                ErrorHandler.ThrowOnFailure(this.invisibleEditor.GetDocData(fEnsureWritable: 1, riid: ref guidIVSTextLines, ppDocData: out docDataPointer));
                try
                {
                    IVsTextLines docData = (IVsTextLines)Marshal.GetObjectForIUnknown(docDataPointer);

                    //Get the component model so we can request the editor adapter factory which we can use to spin up an editor instance.
                    IComponentModel componentModel = (IComponentModel)GetService(typeof(SComponentModel));
                    IVsEditorAdaptersFactoryService editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

                    //Create a code window adapter.
                    this.codeWindow = editorAdapterFactoryService.CreateVsCodeWindowAdapter(OleServiceProvider);

                    //Disable the splitter control on the editor as leaving it enabled causes a crash if the user
                    //tries to use it here :(
                    IVsCodeWindowEx codeWindowEx = (IVsCodeWindowEx)this.codeWindow;
                    INITVIEW[] initView = new INITVIEW[1];
                    codeWindowEx.Initialize((uint)_codewindowbehaviorflags.CWB_DISABLESPLITTER,
                                             VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter,
                                             szNameAuxUserContext: "",
                                             szValueAuxUserContext: "",
                                             InitViewFlags: 0,
                                             pInitView: initView);

                    //docData.SetStateFlags((uint)BUFFERSTATEFLAGS.BSF_USER_READONLY); //set read only

                    //Associate our IVsTextLines with our new code window.
                    ErrorHandler.ThrowOnFailure(this.codeWindow.SetBuffer((IVsTextLines)docData));

                    //Get our text view for our editor which we will use to get the WPF control that hosts said editor.
                    ErrorHandler.ThrowOnFailure(this.codeWindow.GetPrimaryView(out this.textView));

                    //Get our WPF host from our text view (from our code window).
                    IWpfTextViewHost textViewHost = editorAdapterFactoryService.GetWpfTextViewHost(this.textView);

                    //textViewHost.TextView.Options.SetOptionValue(GitTextViewOptions.DiffMarginId, false);
                    textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.ChangeTrackingId, false);
                    textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
                    textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
                    textViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);

                    textViewHost.TextView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId, true);

                    return Tuple.Create<System.Windows.Controls.Control, IVsTextView>(textViewHost.HostControl, this.textView);

                    //Debug.Assert(contentControl != null);
                    //contentControl.Content = textViewHost.HostControl;
                }
                finally
                {
                    if (docDataPointer != IntPtr.Zero)
                    {
                        //Release the doc data from the invisible editor since it gave us a ref-counted copy.
                        Marshal.Release(docDataPointer);
                    }
                }
            }
            catch { }
            return null;
        }


        #region Private Properties

        /// <summary>
        /// The IOleCommandTarget for the editor that our tool window will forward all command requests to when it is the active tool window
        /// and the editor we are hosting has keyboard focus.
        /// </summary>
        private IOleCommandTarget EditorCommandTarget
        {
            get
            {
                return (this.cachedEditorCommandTarget ?? (this.cachedEditorCommandTarget = this.textView as IOleCommandTarget));
            }
        }

        /// <summary>
        /// The IVsFindTarget for the editor that our tool window will forward all find releated requests to when it is the active tool window
        /// and the editor we are hosting has keyboard focus.
        /// </summary>
        private IVsFindTarget EditorFindTarget
        {
            get
            {
                return (this.cachedEditorFindTarget ?? (this.cachedEditorFindTarget = this.textView as IVsFindTarget));
            }
        }

        /// <summary>
        /// The shell's service provider as an OLE service provider (needed to create the editor bits).
        /// </summary>
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider OleServiceProvider
        {
            get
            {
                if (this.cachedOleServiceProvider == null)
                {
                    //ServiceProvider.GlobalProvider is a System.IServiceProvider, but the editor pieces want an OLE.IServiceProvider, luckily the
                    //global provider is also IObjectWithSite and we can use that to extract its underlying (OLE) IServiceProvider object.
                    IObjectWithSite objWithSite = (IObjectWithSite)ServiceProvider.GlobalProvider;

                    Guid interfaceIID = typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider).GUID;
                    IntPtr rawSP;
                    objWithSite.GetSite(ref interfaceIID, out rawSP);
                    try
                    {
                        if (rawSP != IntPtr.Zero)
                        {
                            //Get an RCW over the raw OLE service provider pointer.
                            this.cachedOleServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Marshal.GetObjectForIUnknown(rawSP);
                        }
                    }
                    finally
                    {
                        if (rawSP != IntPtr.Zero)
                        {
                            //Release the raw pointer we got from IObjectWithSite so we don't cause leaks.
                            Marshal.Release(rawSP);
                        }
                    }
                }

                return this.cachedOleServiceProvider;
            }
        }

        /// <summary>
        /// Cleans up an existing editor if we are about to put a new one in place, used to close down the old editor bits as well as
        /// nulling out any cached objects that we have that came from the now dead editor.
        /// </summary>
        internal void ClearEditor()
        {
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

            this.cachedEditorCommandTarget = null;
            this.cachedEditorFindTarget = null;
            this.invisibleEditor = null;
        }
        #endregion
    }
}
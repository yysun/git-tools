using GitScc;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace F1SYS.VsGitToolsPackage
{
    [Guid("8E86F257-5F3C-4E21-B08D-926029AAEECC")]
    public class VsGitToolsService : IDisposable,
        IVsSolutionEvents, 
        IVsUpdateSolutionEvents2
        //IVsFileChangeEvents
    {

        private uint _vsSolutionEventsCookie, _vsIVsUpdateSolutionEventsCookie;

        //private uint _vsSolutionEventsCookie, _vsIVsFileChangeEventsCookie, _vsIVsUpdateSolutionEventsCookie;
        //private string lastMinotorFolder = "";

        private VsGitToolsPackagePackage package;

        private GitRepository previousRepository;

        public GitRepository Repository         
        {
            get
            {
                var repo = trackers.Count == 1 ? 
                    trackers[0]:
                    GetTracker(GetSelectFileName());

                if (repo == null) return previousRepository;
                if (repo != previousRepository)
                {
                    WatchFileChanges(repo.WorkingDirectory);
                    // NeedRefresh = true;
                }
                
                previousRepository = repo;
                return repo;
            }
        }

        private List<GitFileStatusTracker> trackers = new List<GitFileStatusTracker>();

        internal GitFileStatusTracker GetTracker(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            return trackers.Where(t => IsParentFolder(t.WorkingDirectory, fileName))
                           .OrderByDescending(t => t.WorkingDirectory.Length)
                           .FirstOrDefault();
        }

        #region get selected file
        internal string GetSelectFileName()
        {
            var selectedNodes = GetSelectedNodes();
            if (selectedNodes.Count <= 0) return null;
            return GetFileName(selectedNodes[0].pHier, selectedNodes[0].itemid);
        }

        private string GetFileName(IVsHierarchy hierHierarchy, uint itemidNode)
        {
            if (itemidNode == VSConstants.VSITEMID_ROOT)
            {
                if (hierHierarchy == null)
                    return GetSolutionFileName();
                else
                    return GetProjectFileName(hierHierarchy);
            }
            else
            {
                string fileName = null;
                if (hierHierarchy.GetCanonicalName(itemidNode, out fileName) != VSConstants.S_OK) return null;
                return GetCaseSensitiveFileName(fileName);
            }
        }

        private string GetSolutionFileName()
        {

            IVsSolution sol = package.GetServiceEx<SVsSolution>() as IVsSolution;
            string solutionDirectory, solutionFile, solutionUserOptions;
            if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) == VSConstants.S_OK)
            {
                return solutionFile;
            }
            else
            {
                return null;
            }
        }

        private string GetProjectFileName(IVsHierarchy hierHierarchy)
        {
            if (!(hierHierarchy is IVsSccProject2)) return GetSolutionFileName();

            var files = GetNodeFiles(hierHierarchy as IVsSccProject2, VSConstants.VSITEMID_ROOT);
            string fileName = files.Count <= 0 ? null : files[0];

            //try hierHierarchy.GetCanonicalName to get project name for web site
            if (fileName == null)
            {
                if (hierHierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out fileName) != VSConstants.S_OK) return null;
                return GetCaseSensitiveFileName(fileName);
            }
            return fileName;
        }

        public List<IVsSccProject2> GetLoadedControllableProjects()
        {
            var list = new List<IVsSccProject2>();

            IVsSolution sol = package.GetServiceEx<SVsSolution>() as IVsSolution;
            list.Add(sol as IVsSccProject2);

            Guid rguidEnumOnlyThisType = new Guid();
            IEnumHierarchies ppenum = null;
            ErrorHandler.ThrowOnFailure(sol.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref rguidEnumOnlyThisType, out ppenum));

            IVsHierarchy[] rgelt = new IVsHierarchy[1];
            uint pceltFetched = 0;
            while (ppenum.Next(1, rgelt, out pceltFetched) == VSConstants.S_OK &&
                   pceltFetched == 1)
            {
                IVsSccProject2 sccProject2 = rgelt[0] as IVsSccProject2;
                if (sccProject2 != null)
                {
                    list.Add(sccProject2);
                }
            }

            return list;
        }

        private IList<string> GetNodeFiles(IVsSccProject2 pscp2, uint itemid)
        {
            // NOTE: the function returns only a list of files, containing both regular files and special files
            // If you want to hide the special files (similar with solution explorer), you may need to return 
            // the special files in a hastable (key=master_file, values=special_file_list)

            // Initialize output parameters
            IList<string> sccFiles = new List<string>();
            if (pscp2 != null)
            {
                CALPOLESTR[] pathStr = new CALPOLESTR[1];
                CADWORD[] flags = new CADWORD[1];

                if (pscp2.GetSccFiles(itemid, pathStr, flags) == 0)
                {
                    for (int elemIndex = 0; elemIndex < pathStr[0].cElems; elemIndex++)
                    {
                        IntPtr pathIntPtr = Marshal.ReadIntPtr(pathStr[0].pElems, elemIndex);


                        String path = Marshal.PtrToStringAuto(pathIntPtr);
                        sccFiles.Add(path);

                        // See if there are special files
                        if (flags.Length > 0 && flags[0].cElems > 0)
                        {
                            int flag = Marshal.ReadInt32(flags[0].pElems, elemIndex);

                            if (flag != 0)
                            {
                                // We have special files
                                CALPOLESTR[] specialFiles = new CALPOLESTR[1];
                                CADWORD[] specialFlags = new CADWORD[1];

                                pscp2.GetSccSpecialFiles(itemid, path, specialFiles, specialFlags);
                                for (int i = 0; i < specialFiles[0].cElems; i++)
                                {
                                    IntPtr specialPathIntPtr = Marshal.ReadIntPtr(specialFiles[0].pElems, i * IntPtr.Size);
                                    String specialPath = Marshal.PtrToStringAuto(specialPathIntPtr);

                                    sccFiles.Add(specialPath);
                                    Marshal.FreeCoTaskMem(specialPathIntPtr);
                                }

                                if (specialFiles[0].cElems > 0)
                                {
                                    Marshal.FreeCoTaskMem(specialFiles[0].pElems);
                                }
                            }
                        }

                        Marshal.FreeCoTaskMem(pathIntPtr);

                    }
                    if (pathStr[0].cElems > 0)
                    {
                        Marshal.FreeCoTaskMem(pathStr[0].pElems);
                    }
                }
            }
            else if (itemid == VSConstants.VSITEMID_ROOT)
            {
                sccFiles.Add(GetSolutionFileName());
            }

            return sccFiles;
        }

        private IList<VSITEMSELECTION> GetSelectedNodes()
        {
            // Retrieve shell interface in order to get current selection
            IVsMonitorSelection monitorSelection = package.GetServiceEx<IVsMonitorSelection>() as IVsMonitorSelection;

            Debug.Assert(monitorSelection != null, "Could not get the IVsMonitorSelection object from the services exposed by this project");

            if (monitorSelection == null)
            {
                throw new InvalidOperationException();
            }

            List<VSITEMSELECTION> selectedNodes = new List<VSITEMSELECTION>();
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainer = IntPtr.Zero;
            try
            {
                // Get the current project hierarchy, project item, and selection container for the current selection
                // If the selection spans multiple hierachies, hierarchyPtr is Zero
                uint itemid;
                IVsMultiItemSelect multiItemSelect = null;
                ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainer));

                if (itemid != VSConstants.VSITEMID_SELECTION)
                {
                    // We only care if there are nodes selected in the tree
                    if (itemid != VSConstants.VSITEMID_NIL)
                    {
                        if (hierarchyPtr == IntPtr.Zero)
                        {
                            // Solution is selected
                            VSITEMSELECTION vsItemSelection;
                            vsItemSelection.pHier = null;
                            vsItemSelection.itemid = itemid;
                            selectedNodes.Add(vsItemSelection);
                        }
                        else
                        {
                            IVsHierarchy hierarchy = (IVsHierarchy)Marshal.GetObjectForIUnknown(hierarchyPtr);
                            // Single item selection
                            VSITEMSELECTION vsItemSelection;
                            vsItemSelection.pHier = hierarchy;
                            vsItemSelection.itemid = itemid;
                            selectedNodes.Add(vsItemSelection);
                        }
                    }
                }
                else
                {
                    if (multiItemSelect != null)
                    {
                        // This is a multiple item selection.

                        //Get number of items selected and also determine if the items are located in more than one hierarchy
                        uint numberOfSelectedItems;
                        int isSingleHierarchyInt;
                        ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectionInfo(out numberOfSelectedItems, out isSingleHierarchyInt));
                        bool isSingleHierarchy = (isSingleHierarchyInt != 0);

                        // Now loop all selected items and add them to the list 
                        Debug.Assert(numberOfSelectedItems > 0, "Bad number of selected itemd");
                        if (numberOfSelectedItems > 0)
                        {
                            VSITEMSELECTION[] vsItemSelections = new VSITEMSELECTION[numberOfSelectedItems];
                            ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectedItems(0, numberOfSelectedItems, vsItemSelections));
                            foreach (VSITEMSELECTION vsItemSelection in vsItemSelections)
                            {
                                selectedNodes.Add(vsItemSelection);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
                if (selectionContainer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainer);
                }
            }

            return selectedNodes;
        }

        private bool IsParentFolder(string folder, string fileName)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName) ||
               !Directory.Exists(folder)) return false;

            folder = folder.Replace("/", "\\");
            fileName = fileName.Replace("/", "\\");

            bool b = false;
            var dir = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(fileName)));
            while (!b && dir != null)
            {
                b = string.Compare(dir.FullName, folder, true) == 0;
                dir = dir.Parent;
            }
            return b;
        }

        private string GetCaseSensitiveFileName(string fileName)
        {
            return fileName;
        }

        private void AddProject(IVsHierarchy pHierarchy)
        {
            string projectName = GetProjectFileName(pHierarchy);

            if (string.IsNullOrEmpty(projectName)) return;
            string projectDirecotry = Path.GetDirectoryName(projectName);

            // Debug.WriteLine("==== Adding project: " + projectDirecotry);

            var tracker = new GitFileStatusTracker(projectDirecotry);

            if (string.IsNullOrEmpty(projectDirecotry) ||
                 trackers.Any(t=> t.IsGit && string.Compare(
                     t.WorkingDirectory, 
                     tracker.WorkingDirectory, true) == 0)) return;

            trackers.Add(tracker);

            // Debug.WriteLine("==== Added git tracker: " + tracker.WorkingDirectory);

        }
        #endregion

        FileSystemWatcher fileSystemWatcher;
        private void WatchFileChanges(string folder)
        {
            Debug.WriteLine("==== Monitoring: " + folder);

            UnWatchFileChanges();

            if (!GitSccOptions.Current.DisableAutoRefresh)
            {
                fileSystemWatcher = new FileSystemWatcher(folder);
                fileSystemWatcher.IncludeSubdirectories = true;
                fileSystemWatcher.Deleted += new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.Changed += new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        private void OpenRepository()
        {

            trackers.Clear();

            try
            {

                var solutionFileName = GetSolutionFileName();
                if (!string.IsNullOrEmpty(solutionFileName))
                {
                    var solutionDirectory = Path.GetDirectoryName(solutionFileName);
                    GetLoadedControllableProjects().ForEach(h => AddProject(h as IVsHierarchy));
                }
            }
            catch (Exception ex)
            {
                trackers.Clear();
                Debug.WriteLine("VS Git Tools - OpenRepository raised excpetion: ", ex.ToString());
            }
   
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine("****==== File system changed [" + e.ChangeType.ToString() + "]" + e.FullPath);
            if (!e.FullPath.EndsWith(".git") && !e.FullPath.EndsWith("index.lock") && !e.FullPath.EndsWith(".cache"))
            {
                NeedRefresh = true;
            }
        }

        private void CloseRepository()
        {
            trackers.Clear();
            previousRepository = null;
            UnWatchFileChanges();
        }

        private void UnWatchFileChanges()
        {
            if (fileSystemWatcher != null)
            {
                fileSystemWatcher.Deleted -= new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.Changed -= new FileSystemEventHandler(fileSystemWatcher_Changed);
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
                fileSystemWatcher = null;
            }
        }

        public VsGitToolsService(VsGitToolsPackagePackage package)
        {
            this.package = package;

            // Subscribe to solution events
            IVsSolution sol = package.GetServiceEx<SVsSolution>() as IVsSolution; 
            sol.AdviseSolutionEvents(this, out _vsSolutionEventsCookie);

            var sbm = package.GetServiceEx<SVsSolutionBuildManager>() as IVsSolutionBuildManager2;
            if (sbm != null)
            {
                sbm.AdviseUpdateSolutionEvents(this, out _vsIVsUpdateSolutionEventsCookie);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie)
                {
                    IVsSolution sol = package.GetServiceEx<SVsSolution>() as IVsSolution;
                    if (sol != null)
                    {
                        sol.UnadviseSolutionEvents(_vsSolutionEventsCookie);
                    }
                    _vsSolutionEventsCookie = VSConstants.VSCOOKIE_NIL;
                }

                if (VSConstants.VSCOOKIE_NIL != _vsIVsUpdateSolutionEventsCookie)
                {
                    var sbm = package.GetServiceEx<SVsSolutionBuildManager>() as IVsSolutionBuildManager2;
                    if (sbm != null) sbm.UnadviseUpdateSolutionEvents(_vsIVsUpdateSolutionEventsCookie);
                }

                if (fileSystemWatcher != null) fileSystemWatcher.Dispose();
            }
        }

        #region IVsSolutionEvents

        public int OnAfterOpenSolution([InAttribute] Object pUnkReserved, [InAttribute] int fNewSolution)
        {
            OpenRepository();
            RefreshToolWindows();
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution([InAttribute] Object pUnkReserved)
        {
            CloseRepository();
            RefreshToolWindows();
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject([InAttribute] IVsHierarchy pStubHierarchy, [InAttribute] IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject([InAttribute] IVsHierarchy pHierarchy, [InAttribute] int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject([InAttribute] IVsHierarchy pHierarchy, [InAttribute] int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution([InAttribute] Object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject([InAttribute] IVsHierarchy pRealHierarchy, [InAttribute] IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject([InAttribute] IVsHierarchy pHierarchy, [InAttribute] int fRemoving, [InAttribute] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution([InAttribute] Object pUnkReserved, [InAttribute] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject([InAttribute] IVsHierarchy pRealHierarchy, [InAttribute] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterMergeSolution([InAttribute] Object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsUpdateSolutionEvents2

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            NeedRefresh = false;
            NoRefresh = true;
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            NoRefresh = false;
            NeedRefresh = true;
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsFileChangeEvents

        public int DirectoryChanged(string pszDirectory)
        {
            NeedRefresh = true;
            return VSConstants.S_OK;
        }

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region Refresh

        internal bool needRefresh, noRefresh;

        internal bool NeedRefresh
        {
            get { return needRefresh; }
            set
            {
                needRefresh = !noRefresh && value;
                if (needRefresh) Refresh();
            }
        }

        internal bool NoRefresh
        {
            get { return noRefresh; }
            set
            {
                noRefresh = value;
                nextTimeRefresh = DateTime.Now.AddMilliseconds(600);
            }
        }

        private DateTime nextTimeRefresh = DateTime.Now;
        
        private void Refresh()
        {
            if (NeedRefresh && !NoRefresh)
            {
                double delta = DateTime.Now.Subtract(nextTimeRefresh).TotalMilliseconds;
                if (delta > 200)
                {
                    NoRefresh = true;
                    NeedRefresh = false;
                    RefreshToolWindows();
                }
            }
        }

        internal void RefreshToolWindows(bool force=false)
        {
            Debug.WriteLine("==== Refresh !!! ");

            var bgw = new BackgroundWorker();
            bgw.DoWork += (_, __) =>
            {
                CloseRepository();
                OpenRepository();
            };
            bgw.RunWorkerCompleted += (_, __) =>
            {
                var toolWindow = this.package.FindToolWindow(typeof(MyToolWindow), 0, false) as MyToolWindow;
                if (toolWindow != null) toolWindow.Refresh(force);
            };
            bgw.RunWorkerAsync();
        }

        #endregion

    }
}

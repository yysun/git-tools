using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace F1SYS.VsGitToolsPackage
{
    [Guid("8E86F257-5F3C-4E21-B08D-926029AAEECC")]
    public class VsGitToolsService : IDisposable,
        IVsSolutionEvents, 
        IVsUpdateSolutionEvents2,
        IVsFileChangeEvents
    {

        private uint _vsSolutionEventsCookie, _vsIVsFileChangeEventsCookie, _vsIVsUpdateSolutionEventsCookie;
        private string lastMinotorFolder = "";

        private VsGitToolsPackagePackage package;

        private void OpenRepository()
        {
            GitRepository repository = null;
            string solutionDirectory, solutionFile, solutionUserOptions;
            try
            {
                IVsSolution sol = package.GetServiceEx<SVsSolution>() as IVsSolution;
                if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) == VSConstants.S_OK)
                {
                    repository = new GitRepository(solutionDirectory);
                    var monitorFolder = solutionDirectory;

                    IVsFileChangeEx fileChangeService = package.GetServiceEx<SVsFileChangeEx>() as IVsFileChangeEx;
                    if (VSConstants.VSCOOKIE_NIL != _vsIVsFileChangeEventsCookie)
                    {
                        fileChangeService.UnadviseDirChange(_vsIVsFileChangeEventsCookie);
                    }
                    fileChangeService.AdviseDirChange(monitorFolder, 1, this, out _vsIVsFileChangeEventsCookie);
                    lastMinotorFolder = monitorFolder;
                    // Debug.WriteLine("==== Start Monitoring: " + monitorFolder + " " + _vsIVsFileChangeEventsCookie);
                }
            }
            catch (Exception ex)
            {
                repository = null;
                Debug.WriteLine("VS Git Tools - OpenRepository raised excpetion: ", ex.ToString());
            }
            package.repository = repository;
        }

        private void CloseRepository()
        {
            if (VSConstants.VSCOOKIE_NIL != _vsIVsFileChangeEventsCookie)
            {
                IVsFileChangeEx fileChangeService = package.GetServiceEx<SVsFileChangeEx>() as IVsFileChangeEx;
                fileChangeService.UnadviseDirChange(_vsIVsFileChangeEventsCookie);
                // Debug.WriteLine("==== Stop Monitoring: " + _vsIVsFileChangeEventsCookie.ToString());
                _vsIVsFileChangeEventsCookie = VSConstants.VSCOOKIE_NIL;
                lastMinotorFolder = "";
            }
            package.repository = null;
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
            if (VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie)
            {
                IVsSolution sol = package.GetServiceEx<SVsSolution>() as IVsSolution; 
                sol.UnadviseSolutionEvents(_vsSolutionEventsCookie);
                _vsSolutionEventsCookie = VSConstants.VSCOOKIE_NIL;
            }

            if (VSConstants.VSCOOKIE_NIL != _vsIVsUpdateSolutionEventsCookie)
            {
                var sbm = package.GetServiceEx<SVsSolutionBuildManager>() as IVsSolutionBuildManager2;
                sbm.UnadviseUpdateSolutionEvents(_vsIVsUpdateSolutionEventsCookie);
            }
        }

        #region IVsSolutionEvents

        public int OnAfterOpenSolution([InAttribute] Object pUnkReserved, [InAttribute] int fNewSolution)
        {
            OpenRepository();
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution([InAttribute] Object pUnkReserved)
        {
            CloseRepository();
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
                nextTimeRefresh = DateTime.Now.AddMilliseconds(888);
            }
        }

        private DateTime nextTimeRefresh = DateTime.Now;

        internal void Refresh()
        {
            if (NeedRefresh && !NoRefresh)
            {
                double delta = DateTime.Now.Subtract(nextTimeRefresh).TotalMilliseconds;
                if (delta > 200)
                {
                    //Debug.WriteLine("==== OnIdle: " + delta.ToString());

                    //Stopwatch stopwatch = new Stopwatch();
                    //stopwatch.Start();

                    NoRefresh = true;
                    NeedRefresh = false;

                    CloseRepository();
                    OpenRepository();

                    RefreshToolWindows();

                    //NeedRefresh = false;

                    //NoRefresh = false;
                    //nextTimeRefresh = DateTime.Now; //important !!

                    //stopwatch.Stop();
                    //Debug.WriteLine("++++ UpdateNodesGlyphs: " + stopwatch.ElapsedMilliseconds);
                }
            }
        }

        private void RefreshToolWindows()
        {
            var window = this.package.FindToolWindow(typeof(MyToolWindow), 0, false) as MyToolWindow;
            if (window != null) window.Refresh(this, package.repository);
        }

        #endregion

    }
}

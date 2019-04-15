using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        // This method is run automatically the first time the command is being executed
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await MyCommand.InitializeAsync(this);
            //await GitChangesWindowCommand.InitializeAsync(this);
        }

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
    }
}

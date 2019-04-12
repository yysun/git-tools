using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject2019
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Custom Command Sample", "Shows how to hook up a command in VS", "1.0")]       
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("9C86573C-CB62-45D0-9C1A-DAD967BBBDC4")] // must match GUID in the .vsct file
    public sealed class MyPackage : AsyncPackage
    {
        // This method is run automatically the first time the command is being executed
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await MyCommand.InitializeAsync(this);
        }
    }
}

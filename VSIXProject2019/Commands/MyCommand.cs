using System;
using System.ComponentModel.Design;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject2019
{
    internal sealed class MyCommand
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
           
            var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Assumes.Present(commandService);


            CommandID toolwndCommandID = new CommandID(GuidList.guidVsGitToolsPackageCmdSet, (int)PkgCmdIDList.cmdidGitToolsWindow);
            var menuToolWin = new MenuCommand((s, e) => Execute(package), toolwndCommandID);
            commandService.AddCommand(menuToolWin);

            //// must match the button GUID and ID specified in the .vsct file
            //var cmdId = new CommandID(Guid.Parse("31337E4B-26EB-4201-B411-80950E42165B"), 0x0100); 
            //var cmd = new MenuCommand((s, e) => Execute(package), cmdId);
            //commandService.AddCommand(cmd);
        }

        //private static void Execute(AsyncPackage package)
        //{
        //    ThreadHelper.ThrowIfNotOnUIThread();

        //    VsShellUtilities.ShowMessageBox(
        //        package,
        //        $"Inside {typeof(MyCommand).FullName}.Execute()",
        //        nameof(MyCommand),
        //        OLEMSGICON.OLEMSGICON_INFO,
        //        OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //}

        private static void Execute(AsyncPackage package)
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
    }
}

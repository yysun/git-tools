// Guids.cs
// MUST match guids.h
using System;

namespace VSIXProject2019
{
    static class GuidList
    {
        public const string guidVsGitToolsPackagePkgString = "9C86573C-CB62-45D0-9C1A-DAD967BBBDC4";
        public const string guidVsGitToolsPackageCmdSetString = "31337E4B-26EB-4201-B411-80950E42165B";
        public const string guidToolWindowPersistanceString = "e0487501-8bf2-4e94-8b35-ceb6f0010c44";

        public static readonly Guid guidVsGitToolsPackageCmdSet = new Guid(guidVsGitToolsPackageCmdSetString);
    };

    static class PkgCmdIDList
    {
        public const uint cmdidGitToolsWindow = 0x101;

        public const int icmdSccCommandGitBash = 0x102;
        public const int icmdSccCommandGitExtension = 0x103;

        public const int icmdSccCommandInit = 0x106;
        public const int icmdSccCommandPendingChanges = 0x107;
        public const int icmdSccCommandHistory = 0x108;
        public const int icmdSccCommandGitTortoise = 0x109;
        public const int icmdSccCommandEditIgnore = 0x110;

        public const int icmdPendingChangesCommit = 0x111;
        public const int icmdPendingChangesAmend = 0x112;
        public const int icmdPendingChangesCommitToBranch = 0x113;

        public const int icmdPendingChangesRefresh = 0x114;
        public const int icmdHistoryViewRefresh = 0x115;
        public const int icmdPendingChangesSettings = 0x116;
        public const int icmdSccCommandAbout = 0x119;

        public const int icmdGitExtCommand1 = 0x811;
        public const int icmdGitTorCommand1 = 0x911;

        public const int imnuGitChangesToolWindowToolbarMenu = 0x302;

    };
}
// Guids.cs
// MUST match guids.h
using System;

namespace F1SYS.VsGitToolsPackage
{
    static class GuidList
    {
        public const string guidVsGitToolsPackagePkgString = "54edbb65-f9f1-410f-b936-0ac28cfe4b1c";
        public const string guidVsGitToolsPackageCmdSetString = "75db012c-8d8d-4287-89e1-802a537f08eb";
        public const string guidToolWindowPersistanceString = "11dffb59-3169-48ac-9676-2916d06a36de";

        public static readonly Guid guidVsGitToolsPackageCmdSet = new Guid(guidVsGitToolsPackageCmdSetString);
    };
}
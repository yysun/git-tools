using GitScc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsGitToolsPackage.Tests
{
    [TestClass]
    public class DiffToolTest
    {
        DiffTool tool = new DiffTool();
        string[] diffLines = @"diff --get ...
index ...
--- ...
+++ ...
@@ -1,2 +1,2 @@ ...
 1
 2
 3
-1
+1
+2
-2
 4
 5
 6
@@ -10,10 +20,10 @@ ...
 a
 b
 c
-a
+a
+b
-b
 d
 e
 f
".Replace("\r", "").Split('\n');

        [TestMethod]
        public void DiffTool_Should_ParseHunks()
        {
            var hunks = tool.Parse(diffLines);
            Assert.AreEqual(2, hunks.Count());

            Assert.AreEqual(5, hunks[0].FirstLineIndex);
            Assert.AreEqual(14, hunks[0].LastLineIndex);
            Assert.AreEqual(1, hunks[0].OldBlock[0]);
            Assert.AreEqual(2, hunks[0].OldBlock[1]);
            Assert.AreEqual(1, hunks[0].NewBlock[0]);
            Assert.AreEqual(2, hunks[0].NewBlock[1]);
            Assert.AreEqual(16, hunks[1].FirstLineIndex);
            Assert.AreEqual(26, hunks[1].LastLineIndex);
            Assert.AreEqual(10, hunks[1].OldBlock[0]);
            Assert.AreEqual(10, hunks[1].OldBlock[1]);
            Assert.AreEqual(20, hunks[1].NewBlock[0]);
            Assert.AreEqual(10, hunks[1].NewBlock[1]);
        }

        [TestMethod]
        public void DiffTool_Should_Find_Hunk()
        {
            var hunks = tool.GetHunks(diffLines, 9, 9).ToArray();
            Assert.AreEqual(1, hunks.Length);
            hunks = tool.GetHunks(diffLines, 1, 14).ToArray();
            Assert.AreEqual(1, hunks.Length);
            hunks = tool.GetHunks(diffLines, 12, 12).ToArray();
            Assert.AreEqual(1, hunks.Length);
        }

        [TestMethod]
        public void DiffTool_Should_Find_Hunks()
        {
            var hunks = tool.GetHunks(diffLines, 10, 20).ToArray();
            Assert.AreEqual(2, hunks.Length);
            hunks = tool.GetHunks(diffLines, 1, 100).ToArray();
            Assert.AreEqual(2, hunks.Length);
        }

        [TestMethod]
        public void DiffTool_Should_Not_Find_Hunks()
        {
            var hunks = tool.GetHunks(diffLines, 1, 1).ToArray();
            Assert.AreEqual(0, hunks.Length);
            hunks = tool.GetHunks(diffLines, 2, 4).ToArray();
            Assert.AreEqual(0, hunks.Length);
            hunks = tool.GetHunks(diffLines, 5, 5).ToArray();
            Assert.AreEqual(0, hunks.Length);
            hunks = tool.GetHunks(diffLines, 5, 7).ToArray();
            Assert.AreEqual(0, hunks.Length);
            hunks = tool.GetHunks(diffLines, 13, 19).ToArray();
            Assert.AreEqual(0, hunks.Length);
            hunks = tool.GetHunks(diffLines, 24, 100).ToArray();
            Assert.AreEqual(0, hunks.Length);
        }

        [TestMethod]
        public void DiffTool_Should_Adjust_New_Block()
        {
            var hunks = tool.GetHunks(diffLines, 20, 20).ToArray();
            Assert.AreEqual(1, hunks.Length);
            Assert.AreEqual(9, hunks[0].NewBlock[1]);
            Assert.AreEqual(8, hunks[0].Lines.Count());
            hunks = tool.GetHunks(diffLines, 21, 21).ToArray();
            Assert.AreEqual(1, hunks.Length);
            Assert.AreEqual(11, hunks[0].NewBlock[1]);
            Assert.AreEqual(9, hunks[0].Lines.Count());
        }
    }
}

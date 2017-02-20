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

        //[TestMethod]
        //public void DiffTool_Should_Find_Change_Start()
        //{
        //    var lines = tool.GetChanges(diffLines, 8, 8).ToArray();
        //    Assert.IsTrue(lines[0].StartsWith("@@"));
        //}

        //[TestMethod]
        //public void DiffTool_Should_Include_Context()
        //{
        //    var lines = tool.GetChanges(diffLines, 8, 8).ToArray();
        //    Assert.IsTrue(lines[0].StartsWith("@@"));
        //    Assert.AreEqual(8, lines.Length);
        //}

        //[TestMethod]
        //public void DiffTool_Should_Exclude_NoneSelected_Within_Same_Change()
        //{
        //    var lines = tool.GetChanges(diffLines, 9, 10).ToArray();
        //    Assert.AreEqual(9, lines.Length);
        //    Assert.AreEqual("+1", lines[4]);
        //    Assert.AreEqual("+2", lines[5]);
        //}

        //[TestMethod]
        //public void DiffTool_Should_Exclude_NoneSelected_From_Previous_Changes()
        //{
        //    var lines = tool.GetChanges(diffLines, 20, 21).ToArray();
        //    Assert.AreEqual(10, lines.Length);
        //    Assert.AreEqual("+a", lines[4]);
        //    Assert.AreEqual("+b", lines[5]);
        //}

        //[TestMethod]
        //public void DiffTool_Should_Correct_Min_Max()
        //{
        //    var lines = tool.GetChanges(diffLines, 21, 20).ToArray();
        //    Assert.AreEqual(10, lines.Length);
        //    Assert.AreEqual("+a", lines[4]);
        //    Assert.AreEqual("+b", lines[5]);
        //}

        //[TestMethod]
        //public void DiffTool_Should_Include_Multiple_Changes()
        //{
        //    var lines = tool.GetChanges(diffLines, 10, 20).ToArray();
        //    Assert.AreEqual(19, lines.Length);
        //    Assert.AreEqual("+2", lines[4]);
        //    Assert.AreEqual("-2", lines[5]);
        //    Assert.AreEqual("@@", lines[9]);
        //    Assert.AreEqual("-a", lines[13]);
        //    Assert.AreEqual("+a", lines[14]);
        //}

        //[TestMethod]
        //public void DiffTool_Should_Validate_Ranges()
        //{
        //    var lines = tool.GetChanges(diffLines, 1, 200).ToArray();
        //    Assert.AreEqual(23, lines.Length);
        //}

        //[TestMethod]
        //public void DiffTool_Should_Return_Empty_List_With_Invalid_Ranges()
        //{
        //    var lines = tool.GetChanges(diffLines, 1, 1).ToArray();
        //    Assert.AreEqual(0, lines.Length);
        //}

        [TestMethod]
        public void DiffTool_Should_ParseHunks()
        {
            var hunks = tool.Parse(diffLines);
            Assert.AreEqual(2, hunks.Count());

            Assert.AreEqual(4, hunks[0].FirstLineIndex);
            Assert.AreEqual(14, hunks[0].LastLineIndex);
            Assert.AreEqual(1, hunks[0].OldBlock[0]);
            Assert.AreEqual(2, hunks[0].OldBlock[1]);
            Assert.AreEqual(1, hunks[0].NewBlock[0]);
            Assert.AreEqual(2, hunks[0].NewBlock[1]);
            Assert.AreEqual(15, hunks[1].FirstLineIndex);
            Assert.AreEqual(26, hunks[1].LastLineIndex);
            Assert.AreEqual(10, hunks[1].OldBlock[0]);
            Assert.AreEqual(10, hunks[1].OldBlock[1]);
            Assert.AreEqual(20, hunks[1].NewBlock[0]);
            Assert.AreEqual(10, hunks[1].NewBlock[1]);
        }

        [TestMethod]
        public void DiffTool_Should_Find_Hunk()
        {
            var hunks = tool.GetPatches(diffLines, 8, 8).ToArray();
            Assert.AreEqual(1, hunks.Length);
            hunks = tool.GetPatches(diffLines, 1, 14).ToArray();
            Assert.AreEqual(1, hunks.Length);
            hunks = tool.GetPatches(diffLines, 14, 14).ToArray();
            Assert.AreEqual(1, hunks.Length);
        }

        [TestMethod]
        public void DiffTool_Should_Find_Hunks()
        {
            var hunks = tool.GetPatches(diffLines, 12, 15).ToArray();
            Assert.AreEqual(2, hunks.Length);
            hunks = tool.GetPatches(diffLines, 1, 100).ToArray();
            Assert.AreEqual(2, hunks.Length);
        }
    }

}

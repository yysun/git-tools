using GitScc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace VsGitToolsPackage.Tests
{
    [TestClass]
    public class GitRepositoryTest
    {
        protected string tempFolder;
        protected string tempFile;
        protected string tempFilePath;
        protected string[] lines;

        public GitRepositoryTest()
        {
            GitBash.GitExePath = @"C:\Program Files (x86)\Git\bin\sh.exe";
            GitBash.UseUTF8FileNames = true;
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            tempFolder = Environment.CurrentDirectory + "\\" + Guid.NewGuid().ToString();
            Directory.CreateDirectory(tempFolder);
            tempFile = Guid.NewGuid().ToString();
            tempFilePath = Path.Combine(tempFolder, tempFile);
            lines = new string[] { "First line", "中文 2", "čtestč" };
        }
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion
        [TestMethod()]
        public void WorkingDirectoryTest()
        {
            GitRepository.Init(tempFolder);
            var newFolder = tempFolder + "\\t t\\a a";
            Directory.CreateDirectory(newFolder);
            GitRepository tracker = new GitRepository(newFolder);
            Assert.AreEqual(tempFolder.Replace("\\", "/"), tracker.WorkingDirectory);
        }

        [TestMethod()]
        public void HasGitRepositoryTest()
        {

            GitRepository.Init(tempFolder);
            GitRepository tracker = new GitRepository(tempFolder);

            Assert.IsTrue(tracker.IsGit);
            Assert.IsTrue(Directory.Exists(tempFolder + "\\.git"));
        }

        [TestMethod]
        public void GetFileStatusTest()
        {
            GitRepository.Init(tempFolder);
            GitRepository tracker = new GitRepository(tempFolder);

            File.WriteAllLines(tempFilePath, lines);
            Assert.AreEqual(GitFileStatus.New, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Added, tracker.GetFileStatus(tempFile));

            tracker.UnStageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.New, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Added, tracker.GetFileStatus(tempFile));

            tracker.Commit("中文 1čtestč");
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Tracked, tracker.GetFileStatus(tempFile));

            File.WriteAllText(tempFilePath, "changed text");
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Modified, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Staged, tracker.GetFileStatus(tempFile));

            tracker.UnStageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Modified, tracker.GetFileStatus(tempFile));

            File.Delete(tempFilePath);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Deleted, tracker.GetFileStatus(tempFile));

            tracker.StageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Removed, tracker.GetFileStatus(tempFile));

            tracker.UnStageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Deleted, tracker.GetFileStatus(tempFile));
        }
/*
        [TestMethod]
        public void GetFileContentTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("中文 1čtestč");

            var fileContent = tracker.GetFileContent(tempFile);

            using (var binWriter = new BinaryWriter(File.Open(tempFile + ".bk", System.IO.FileMode.Create)))
            {
                binWriter.Write(fileContent);
            }

            var newlines = File.ReadAllLines(tempFile + ".bk");
            Assert.AreEqual(lines[0], newlines[0]);
            Assert.AreEqual(lines[1], newlines[1]);
            Assert.AreEqual(lines[2], newlines[2]);
        }

        [TestMethod]
        public void GetFileContentTestNegative()
        {
            GitRepository tracker = new GitRepository(tempFolder);
            var fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);

            GitRepository.Init(tempFolder);

            File.WriteAllLines(tempFilePath, lines);
            tracker = new GitRepository(tempFolder);
            fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);

            tracker.StageFile(tempFile);
            fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);

            tracker.Commit("中文 1čtestč");

            fileContent = tracker.GetFileContent(tempFile + ".bad");
            Assert.IsNull(fileContent);
        }
*/
        [TestMethod]
        public void GetChangedFilesTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Added, tracker.GetFileStatus(tempFile));
            tracker.Commit("中文 1čtestč");
            tracker.Refresh();
            Assert.AreEqual(0, tracker.ChangedFiles.Count());
            File.WriteAllText(tempFilePath, "a");
            tracker.Refresh();
            Assert.AreEqual(GitFileStatus.Modified, tracker.GetFileStatus(tempFile));
        }

        [TestMethod]
        public void LastCommitMessageTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);
            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("中文 1čtestč");
            Assert.IsTrue(tracker.LastCommitMessage.Equals("中文 1čtestč"));
        }

        [TestMethod]
        public void AmendCommitTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);

            tracker.Commit("中文 1čtestč");
            Assert.IsTrue(tracker.LastCommitMessage.Equals("中文 1čtestč"));

            File.WriteAllText(tempFile, "changed text");
            tracker.StageFile(tempFile);
            tracker.Commit("new message", true);
            Assert.IsTrue(tracker.LastCommitMessage.Equals("new message"));
        }

        [TestMethod]
        public void DiffFileTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("test message");
            File.WriteAllText(tempFilePath, "changed text");
            var diffFile = tracker.DiffFile(tempFile);
            var diff = File.ReadAllText(diffFile);
            Assert.IsTrue(diff.Contains("@@ -1,3 +1 @@"));
        }

        [TestMethod]
        public void FileNameCaseTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);

            tracker.Commit("test message");
            Assert.IsTrue(tracker.LastCommitMessage.StartsWith("test message"));
            tempFile = tempFile.Replace("test", "TEST");
            File.WriteAllText(tempFilePath, "changed text");
            tracker.Refresh();
            //This test fails all cases because status check uses ngit, never git.exe
            //Assert.AreEqual(GitFileStatus.Modified, tracker.GetFileStatus(tempFile));

            var file = tracker.ChangedFiles.First();
            Assert.AreEqual(GitFileStatus.Modified, file.Status);
        }

        [TestMethod]
        public void GetBranchTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            Assert.AreEqual("master", tracker.CurrentBranch);

            tracker.StageFile(tempFile);
            Assert.AreEqual("master", tracker.CurrentBranch);

            tracker.Commit("test message");
            Assert.AreEqual("master", tracker.CurrentBranch);

            tempFile = tempFile.Replace("test", "TEST");
            File.WriteAllText(tempFilePath, "changed text");

            tracker.CheckOutBranch("dev", true);
            Assert.AreEqual("dev", tracker.CurrentBranch);
        }

        [TestMethod]
        public void SaveFileFromRepositoryTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("test");

            var tmp = Path.Combine(Path.GetTempPath(), tempFile) + ".bk";
            tracker.SaveFileFromLastCommit(tempFile, tmp);
            var newlines = File.ReadAllLines(tmp);
            Assert.AreEqual(lines[0], newlines[0]);
            Assert.AreEqual(lines[1], newlines[1]);
            Assert.AreEqual(lines[2], newlines[2]);
        }

        [TestMethod]
        public void CheckOutFileTest()
        {
            GitRepository.Init(tempFolder);
            File.WriteAllLines(tempFilePath, lines);

            GitRepository tracker = new GitRepository(tempFolder);
            tracker.StageFile(tempFile);
            tracker.Commit("test");

            File.WriteAllText(tempFilePath, "changed text");
            tracker.CheckOutFile(tempFile);
            var newlines = File.ReadAllLines(tempFilePath);
            Assert.AreEqual(lines[0], newlines[0]);
            Assert.AreEqual(lines[1], newlines[1]);
            Assert.AreEqual(lines[2], newlines[2]);
        }
    }
}

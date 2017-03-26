# Git History Viewer

The Git History Viewer displays commits horizontally with heads, tags and remotes annotations. You will realize that the git repository and 
history are quite easy to understand. 

## Branch

A git branch is a pointer to commit. The HEAD is where next commit will be added.

Here is a git repository, in which there are two branches, master and dev. Current branch is master.

![a](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296660)

If checkout/switch to dev branch. The HEAD point moves to dev.

![b](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296662)

Branching in git does not require creating new folders. Branches are all in one working directory. When switching between branches, git cleans up and re-creates the working folder and moves HEAD.

## Merge 

Git merge command merges current branch to another branches. Depends on which branch is current, the result will be different.

Let’s start with a repository.

![a](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296665)

Current branch is master. If merge master with dev by running _git merge dev_, the result is below.

![b](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296667)
 
Or, if checkout dev as current branch.

![c](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296675)

Then merge it with master, the result is shown next.

![d](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296677)
 
What’s the difference?

Merging dev with master, dev branch moves forward. Master branch stay where it is.
Merging master with dev, master branch moves forward. dev branch stay where it is.

That means, current branch moves forward. Target branch stays where it is.

## Fast forward merge

If merge a branch to another branch, which contains current branch, git will perform a fast forward merge. In this case git will just move the branch pointer.
Let’s start with this repository.

![a](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296656)

Current branch is dev. When merge dev with master. It is a fast forward merge.

![b](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296658)

Commits and commit IDs keep the same. Only the dev branch pointer moved.

It is possible to disable fast forward by adding –no-ff option to the merge command. E.g _git merge master –no-ff_. The result is below.

![c](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296679)

## Rebase

Git rebase re-applies commits from current branch on top of another branch. Thus creates a linear history.

Let’s start with this repository.

![a](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296650)

Current branch is dev. After running _git rebase master_. The commits are lined up.

![b](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296652)

Compare to merge, if run _git merge master_. The result will be non-linear.

![c](http://download.codeplex.com/Download?ProjectName=gitscc&DownloadId=296654)


Have fun with the Git History Viewer. 

## Tip

One more tip. Once used the Git History Viewer from the Git Tools for Visual Studio, it will generate Dragon.exe in your Documents folder. Copy %userprofile%\My Documents\Dragon.exe to "C:\Program Files\Git\mingw64\libexec\git-core\git-viewer.exe", you will be able to use the Git History Viewer for any repositories by command _git viewer_.






# Git Tools Extension for Visual Studio

## Introduction

This extension provides a few more git tools to Visual Studio, including git changes window, graphical git history viewer and menus to launch Git Bash, Git Extensions and TortoiseGit. It is the successor of [Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c)

[Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c) has been providing Git tools to Visual Studio since Visual Studio 2008. Started in Visual Studio 2013, Microsoft has built the Microsoft Git provider into Visual Studio, but Visual Studio remains allowing only one active source control provider at a time. In order to be compatible with Microsoft Git, this extension is a modification of the Git Source Control Provider. It removed the file status glyphs in solution explorer, so that it can run side by side with Microsoft Git. 

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KBCLF3PZD6C98&lc=US&item_name=Git%20Tools%20for%20Visual%20Studio&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted)

## How to Use

* Install [Git for Windows](http://code.google.com/p/msysgit), [Git Extensions](http://code.google.com/p/gitextensions) (optional) or [TortoiseGit](http://code.google.com/p/tortoisegit) (optional).
* Run Visual Studio.
* Go to Tools | Extensions and Updates..., search online gallery for Git Tools and install.
* Select Top Menu 'Git Tools' or add 'Git Tools' toolbar to the main window.

## Features

### Commit Git Changes

Using git, you commit a lot. The Git Changes Window is a Visual Studio tool window that allow you commit and run git commands without leaving Visual Studio. Just like the Error list, Output, C# interactive, Package Manager Console and etc.

The Git Changes Window detects the file changes and displays the diff in real time. You can review and commit the changes to git. It has two modes: simple mode or advanced mode.

#### Simple Mode
The simple mode hides the concept of git index / staging area. The file changes are diff-ed against the repository. You can select and commit changes to git directly.

![simplified-mode](https://cloud.githubusercontent.com/assets/170547/23336456/1c6b784a-fb9f-11e6-8136-81dc09205b6f.png)

<p class="tip">
Simple mode helps onboarding new developers when you don't want them to be distracted by the index / staging area. Simple mode also helps experienced developers who want to commit quickly and skip the step of staging files.
</p>

#### Advanced Mode

The advanced mode provides granular controls to git index. You can select files as well as well lines of files to stage, un-stage and reset. It allows fine tuning of the commits.

![advanced-mode-1](https://cloud.githubusercontent.com/assets/170547/23336458/23f1fd96-fb9f-11e6-9968-276ea3eca394.png)

<p class="tip">
The advanced mode is useful to arrange changes belong to multiple features or bugs. You can stage files or lines for one feature or one bug; commit and then move to next feature and bug. 
</p>

Both simple mode and advanced mode support amend last commit. Developers will always feel free to commit, because amend is easy.

The Git Changes Window can helps you create meaningful code history.


### View Git History

The Git history viewer is a unique tool that displays the commits horizontally to help you understand the history. Each commit is a box annotated
by refs. The refs are color coded. Local branches are light red. Current branch and HEAD are dark red. Tags are green. Remote branches are yellow.

![git-history-viewer-simplified-view-off](https://cloud.githubusercontent.com/assets/170547/23336493/f1b14098-fb9f-11e6-9319-a8f1d02ee2e0.png)

Developers often debate whether to squash commits, because on one hand, you want to commit often and to make commits small/simple so that in future you can identify bugs easily using git bisect. On the other hand, you want to squash so that the history is clean and easy to read. These two are contradicting needs. 

Fortunately, The Git history viewer solves the problem for you. It has a Simplified View toggle. When the Simplified View is off, it displays all commits. When the Simplified View is on, it displays only key commits.

#### Simplified View

The Simplified view displays only key commits. The key commits are those have refs and/or multiple parents/children. By only displaying the key commits,you can see the history structure clearly.

![git-history-viewer-simplified-view](https://cloud.githubusercontent.com/assets/170547/23336491/eeb85796-fb9f-11e6-861b-97878de280ba.png)

Now you can commit as much often as you want. The history is still easy to read.

#### Manage Commits

You can use the Git History Viewer to view the details of each commit by clicking the commit boxes. You can also create/delete branches and tag, merge, rebase, cherry-pick, push, pull and etc by right clicking the commit boxes. In the future release, we are adding the ability of drag and drop commits to merge the branches.

<p class="tip">
Git Viewer runs outside Visual Studio. It can be used for non Visual Studio projects. Node.js, Ruby, PHP, Cordova/PhoneGap and etc. You can find it as Dragon.exe in your Documents folder.
</p>


### Git Console

Git is a feature rich and complicated system. There are many Git GUI tools, yet no tool is as good as running Git commands directly. Now you can run Git commands right inside Visual Studio. It even provides auto completion of the Git commands. E.g. It shows branch names when you need it.

![git-console](https://cloud.githubusercontent.com/assets/170547/23336540/2b58ee08-fba1-11e6-8591-55aceb319124.png)

<p class="tip">
The embedded console can also run npm, grunt, gulp and etc.
</p>


### Git Tools Integration

#### Git Bash

The Git Bash is accessible through the Git Tools menu.

![main-menu](https://cloud.githubusercontent.com/assets/170547/23336421/281f2002-fb9e-11e6-9cec-77362e6a553c.png)

#### Git Extensions

If you have installed the [Git Extensions](http://code.google.com/p/gitextensions), the git extension features will list under the Git Tools menu.

![main-menu-gitext](https://cloud.githubusercontent.com/assets/170547/23336427/59259ea6-fb9e-11e6-97c8-f7d1fd321325.png)

#### TortoiseGit

If you have installed the [TortoiseGit](http://code.google.com/p/tortoisegit), the TortoiseGit features will list under the Git Tools menu.

![main-menu-gittor](https://cloud.githubusercontent.com/assets/170547/23336429/69e726ba-fb9e-11e6-8790-f460c019f9a5.png)


## Open Source

The source code of this extension is available from [GitHub](https://github.com/yysun/git-tools). Forks and pull requests are welcome.

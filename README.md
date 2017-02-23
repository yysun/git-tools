# Git Tools

## Introduction

This extension provides a git changes tool window, a graphical git history viewer and menus to launch Git Bash, Git Extenstions and TortoiseGit. It is the successor of Git Source Control Provider.

[Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c) has been providing Git tools to supports Visual Studio 2008-2015. 
Since Visual Studio 2013, Microsoft has built the Microsoft Git provider into Visual Studio. Visual Studio can have only one active source control provider at a time. 
In order to be compatible with Microsoft Git, this extension is a modification of Git Source Control Provider. It removed the source control provider part and only contains the Git tools, 
so that it can run side by side with Microsoft Git. Git Source Control Provider is still available as a separate extension.

The source code of this extension is available from [GitHub](https://github.com/yysun/git-tools). Forks and pull requests are welcome.

For more information, please visit http://yysun.github.io/git-tools

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KBCLF3PZD6C98&lc=US&item_name=Git%20Source%20Control%20Provider&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted)

# Features

* Git Tools Menu
* Git Tools Toolbar
* Git Changes Window
* Git History Viewer
* Stage/Unstage/Reset by lines

## How to Use

* Install [Git for Windows](http://code.google.com/p/msysgit), [Git Extensions](http://code.google.com/p/gitextensions) or [TortoiseGit](http://code.google.com/p/tortoisegit).
* Run Visual Studio. 
* Go to Tools | Extensions and Updates..., search online gallery for Git Tools and install. 
* Select Top Menu 'Git Tools' or add 'Git Tools'toolbar to the main window.

## Compile Source Code

* Get source code: _git clone https://github.com/yysun/git-tools.git_
* Open the solution and compile in Visual Studio 2017

## Change Logs

### 1.7.0

* Added advanced mode: displays unstaged changes and staged changes as two lists
* Added advanced mode: allows stage/unstage/reset by selecting line
* Improved Git Console auto complete

### 1.6.0

* Upgrade solution for VS 2017 RC, support VS 2017 RC

### 1.5.0

* Performance enhancement: Use background thread to refresh git changes
* Performance enhancement: Use background thread to stage mutiple files
* Performance enhancement: Not to refresh if the changed files are ignored by git
* Copy Dragon.exe to Documents folder instead of Temp folder
* Support VS community and Enterprise Editions
* Bug fixes

### 1.4.2

* Add try and catch to timer tick to prevent VS crashes

### 1.4.1

* Fix .NET framework version issue in VS2015 CTP5

### 1.4

* Add git console

### 1.3.3

* Add multiple git trackers

### 1.3.2

* Fix diff is not highlighting issue

### 1.3.1

* Fix System.IO.PathTooLongException issue when folder structure is deep

### 1.3

* Add compare menu in changed file list to launch visual studio diff viewer
* Add more context menu to git history viewer, e.g. init, stash, cherry pick, rebase, merge and etc.
* Re-work on the refresh logic

### 1.2

* Detect TortoiseGitProc.exe
* Fix exception in repository refresh

### 1.1

* Fix the Duplicate EditorFormatDefinition issue

### 1.0

* Migrated from [Git Source Control Provider](https://github.com/yysun/Git-Source-Control-Provider)

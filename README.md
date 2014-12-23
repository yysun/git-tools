# Git Tools

## Introduction

This is a Visual Studio extension provides a git changes tool window, a graphical git history viewer and menus to launch Git Bash, Git Extenstions and TortoiseGit.

The same set of Git tools were developed in [Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c) for Visual Studio 2008-2013. Since Visual Studio 2013, Microsoft has built Microsoft Git provider. There is only one source control provider can beused in Visual Studio at a time. This extension is a modification of [Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c). It removed the source control provider and contains the Git tools only, so that it makes the Git tools compatible with Microsoft Git provider.

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KBCLF3PZD6C98&lc=US&item_name=Git%20Source%20Control%20Provider&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted)

# Features

* Git Tools Menu
* Git Tools Toolbar
* Git Changes Window
* Git History Viewer

## How to Use

* Install [Git for Windows](http://code.google.com/p/msysgit), [Git Extensions](http://code.google.com/p/gitextensions) or [TortoiseGit](http://code.google.com/p/tortoisegit).
* Run Visual Studio. 
* Go to Tools | Extensions and Updates..., search online gallery for Git Tools and install. 
* Select Top Menu 'Git Tools' or add 'Git Tools'toolbar to the main window.

## Compile Source Code

* Install [Visual Studio 2013 SDK](http://www.microsoft.com/en-ca/download/details.aspx?id=40758)
* Get source code: _git clone https://github.com/yysun/git-tools.git_
* Open the solution and compile in Visual Studio 2013

## Change Logs

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

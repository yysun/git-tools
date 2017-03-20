# Git Tools

## Introduction

This extension provides a git changes tool window, a graphical git history viewer and menus to launch Git Bash, Git Extensions and TortoiseGit. It is the successor of Git Source Control Provider.

[Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c) has been providing Git tools to Visual Studio since Visual Studio 2008. 
Started in Visual Studio 2013, Microsoft has built the Microsoft Git provider into Visual Studio. Visual Studio can have only one active source control provider at a time. 
In order to be compatible with Microsoft Git, this extension is a modification of Git Source Control Provider. It removed the source control provider part and only contains the Git tools, 
so that it can run side by side with Microsoft Git. Git Source Control Provider is still available as a separate extension.

For more information, please visit http://yysun.github.io/git-tools

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KBCLF3PZD6C98&lc=US&item_name=Git%20Tools%20for%20Visual%20Studio&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted)

# Features

## Git Tools Menu
![main-menu](https://cloud.githubusercontent.com/assets/170547/23336421/281f2002-fb9e-11e6-9cec-77362e6a553c.png)

![main-menu-gitext](https://cloud.githubusercontent.com/assets/170547/23336427/59259ea6-fb9e-11e6-97c8-f7d1fd321325.png)

![main-menu-gittor](https://cloud.githubusercontent.com/assets/170547/23336429/69e726ba-fb9e-11e6-8790-f460c019f9a5.png)

# Git Tools Toolbar
![tool-bar](https://cloud.githubusercontent.com/assets/170547/23336451/fafd078c-fb9e-11e6-8000-90f6fd606034.png)

# Git Changes Window - Simple Mode - Manage Stage/Index Automatically
![simplified-mode](https://cloud.githubusercontent.com/assets/170547/23336456/1c6b784a-fb9f-11e6-8136-81dc09205b6f.png)

# Git Changes Window - Advanced Mode - Stage/Unstage/Reset by Files and Lines
![advanced-mode-1](https://cloud.githubusercontent.com/assets/170547/23336458/23f1fd96-fb9f-11e6-9968-276ea3eca394.png)

# Git History Viewer - All commits
![git-history-viewer-simplified-view-off](https://cloud.githubusercontent.com/assets/170547/23336493/f1b14098-fb9f-11e6-9319-a8f1d02ee2e0.png)

# Git History Viewer - Key commits
![git-history-viewer-simplified-view](https://cloud.githubusercontent.com/assets/170547/23336491/eeb85796-fb9f-11e6-861b-97878de280ba.png)

# Git Console with auto complete
![git-console](https://cloud.githubusercontent.com/assets/170547/23336540/2b58ee08-fba1-11e6-8591-55aceb319124.png)


## How to Use

* Install [Git for Windows](http://code.google.com/p/msysgit), [Git Extensions](http://code.google.com/p/gitextensions) (optional) or [TortoiseGit](http://code.google.com/p/tortoisegit) (optional).
* Run Visual Studio. 
* Go to Tools | Extensions and Updates..., search online gallery for Git Tools and install. 
* Select Top Menu 'Git Tools' or add 'Git Tools'toolbar to the main window.

![install](https://cloud.githubusercontent.com/assets/170547/23336552/6e7382b6-fba1-11e6-80ed-f0cefa01ee27.png)

## Compile Source Code

* Get source code: _git clone https://github.com/yysun/git-tools.git_
* Open the solution and compile in Visual Studio 2017
* Pull requests are welcome

## Change Logs

### 2.0.0

* Added advanced mode: displays unstaged changes and staged changes as two lists
* Added advanced mode: allows stage/unstage/reset by selecting line(s)
* Improved Git Console auto complete
* Support git commit.template settings

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

# Git Tools

## Introduction

This extension provides a few more git tools to Visual Studio, including git changes window, graphical git history viewer and menus to launch Git Bash, Git Extensions and TortoiseGit. It is the successor of [Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c)

[Git Source Control Provider](https://visualstudiogallery.msdn.microsoft.com/63a7e40d-4d71-4fbb-a23b-d262124b8f4c) has been providing Git tools to Visual Studio since Visual Studio 2008. Started in Visual Studio 2013, Microsoft has built the Microsoft Git provider into Visual Studio, but Visual Studio remains allowing only one active source control provider at a time. In order to be compatible with Microsoft Git, this extension is a modification of the Git Source Control Provider. It removed the file status glyphs in solution explorer, so that it can run side by side with Microsoft Git. 

![main-menu](https://cloud.githubusercontent.com/assets/170547/23336421/281f2002-fb9e-11e6-9cec-77362e6a553c.png)

For more information, please visit http://yysun.github.io/git-tools

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KBCLF3PZD6C98&lc=US&item_name=Git%20Tools%20for%20Visual%20Studio&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted)

## How to Use

* Install [Git for Windows](http://code.google.com/p/msysgit), [Git Extensions](https://gitextensions.github.io) (optional) or [TortoiseGit](http://code.google.com/p/tortoisegit) (optional).
* Run Visual Studio.
* Go to Tools | Extensions and Updates..., search online gallery for Git Tools and install.
* Select Top Menu 'Git Tools' or add 'Git Tools' toolbar to the main window.

## Compile Source Code

* Get source code: _git clone https://github.com/yysun/git-tools.git_
* Open the solution and compile in Visual Studio 2017
* Pull requests are welcomed

## Change Logs

### 2.0.0

* Added advanced mode: displays un-staged changes and staged changes as two lists
* Added advanced mode: allows stage/un-stage/reset by selecting line(s)
* Improved Git Console auto complete
* Support git commit.template settings

### 1.6.0

* Upgrade solution for VS 2017 RC, support VS 2017 RC

### 1.5.0

* Performance enhancement: Use background thread to refresh git changes
* Performance enhancement: Use background thread to stage multiple files
* Performance enhancement: Not to refresh if the changed files are ignored by git
* Copy Dragon.exe to Documents folder instead of Temp folder
* Support VS community and Enterprise Editions
* Bug fixes

### 1.4

* Add git console

### 1.3

* Add compare menu in changed file list to launch visual studio diff viewer
* Add more context menu to git history viewer, e.g. init, stash, cherry pick, rebase, merge and etc.
* Re-work on the refresh logic

### 1.0

* Migrated from [Git Source Control Provider](https://github.com/yysun/Git-Source-Control-Provider)

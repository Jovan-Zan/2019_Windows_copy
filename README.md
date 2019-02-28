# Multithread Windows Copy

## Summary
Copy/cut/paste features using Windows **robocopy** command with the same interface as traditional Windows explorer copy/paste.
Select the files you wish to copy, *right-click->robo-copy* or *right-click->robo-cut* and then paste them to destination folder using *right-click->robo-paste*. By default **8 threads** are used to copy selected files and folders.  
Advanced features for experienced users under *right-click->advanced*.  
For details about robocopy see [robocopy | Microsoft Docs.](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/robocopy)

## Languages and technologies used
Implementation is in C# with PowerShell as C# extension to execute robocopy commands.  
[Nullsoft scriptable install system (NSIS)](https://nsis.sourceforge.io/) is used to create installation wizard.

## Installation
Simple installation using installation wizard.

## Compatibility 
The feature is compatible with all Windows versions supporting robocopy (*Windows Vista* and later and *Windows Server 2008* and later).

## Authors
Marija Katic, contact: *mr16032 et alas.matf.bg.rs*  
Jovan Milenkovic, contact: *mr16006 et alas.matf.bg.ac.rs*
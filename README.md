# Contoso Data Generator

A custom Contoso sample database generator.

**IMPORTANT**: Clone the repository only if you want to compile the project with Visual Studio. If you want to use the ready-to-use script to generate new database with precompiled tool, you must download the zip file with *Contoso-Data-Generator source code and executables* from the release folder.

## Content

- DatabaseGenerator project
- Scripts to build the custom Contoso database importing constant and generated files
- CSV Files with the constant data 

## DatabaseGenerator

This project generates the orders tables as csv files to be imported into the custom Contoso database.

It's a C# project for .NET Core 3.1, included into the DatabaseGenerator.sln Visual Studio solution.

### DatabaseGenerator C# project Folders

- **DatabaseGenerator**: the project folder
- **data_folder**: the folder with simple configuration files for debugging purpose

## Scripts

Powershell scripts that  

- runs the DatabaseGenerator
- creates the new custom contoso database
- import the constant and generated data into the new databae
- exports the generated database to a compressed file

### Scripts folders

- **Scripts** and subfolders: the PowerShell scripts and SQL scripts run by it
- **Contoso Main** and subfolders: the CSV containing the constant data as csv files 
- **DataseteSqlbi**: configuration files used to run DatabaseGenerator

## Quick Run

To quickly test the tool, verify the pre-requisites and run the QuickRun.ps1 script in the script folder.

**IMPORTANT**: The executable is available in the ZIP included in the release folder. If you clone the repository, you must compile the executable with Visual Studio.

### Pre-requisite to run the tool

- 7-Zip [https://www.7-zip.org/](https://www.7-zip.org/)
- Microsoft SQL Server must be installed on the same PC, reachable through the Alias **Demo**
- The user running Microsoft SQL Server service must have access to the **SqlDataFiles** folder (default C:\TEMP) used by the PowerShell script (see following section)
- .Net Core 3.1 [https://dotnet.microsoft.com/en-us/download/dotnet/3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1)

### Running QuickRun.ps1

QuickRun.ps1 takes one optional parameter

 - **SqlDataFilesFolder** (default C:\TEMP): the folder to contain the generated files. The user of the SQL Server service must have access to this folder with the rights to read, write and create files.

QuickRun.ps1 runs GenerateDatabases.ps1 script. For further details, please refert to the documentation into the Scripts folder

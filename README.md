# Contoso Data Generator

A custom Contoso sample database generator.

**IMPORTANT**: There are 2 ways to run this custom Contoso sample database generator: 
1. If you clone the repository, you must compile the project with Visual Studio. 
2. If you want to use the ready-to-use script to generate a new database with the precompiled tool, you can download the zip file with *Contoso-Data-Generator source code and executables* from the release folder.

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
- **DatasetSqlbi**: configuration files used to run DatabaseGenerator

## Instructions

To quickly test the tool, verify the pre-requisites and run the QuickRun.ps1 script in the script folder.

**IMPORTANT**: The executable is available in the ZIP included in the release folder. If you clone the repository, you must compile the executable with Visual Studio.

### Pre-requisite to run the tool

- 7-Zip [https://www.7-zip.org/](https://www.7-zip.org/)
- Microsoft SQL Server must be installed on the same PC, reachable through the Alias **Demo**. You can download SQL Server Developer (free edition, licensed for use as a development and test database in a non-production environment) at [https://www.microsoft.com/en-us/sql-server/sql-server-downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- The user running Microsoft SQL Server service must have access to the **SqlDataFiles** folder (default C:\TEMP) used by the PowerShell script (see following section)
- .Net Core 3.1 [https://dotnet.microsoft.com/en-us/download/dotnet/3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1)

### Running QuickRun.ps1

QuickRun.ps1 takes two optional parameters

 - **SqlDataFilesFolder** (default C:\TEMP): the folder to contain the generated files. The user running Microsoft SQL Server service must have access to the SqlDataFiles folder (default C:\TEMP). For more information read [https://www.mssqltips.com/sqlservertip/6930/issues-sql-server-permissions-restore-database/](https://www.mssqltips.com/sqlservertip/6930/issues-sql-server-permissions-restore-database/)
 - **SqlServerInstance** (Default Demo): the name of the SQL server instance. The default is the alias name Demo, that can be configured using the SQL Server Configuration Manager [https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/create-or-delete-a-server-alias-for-use-by-a-client](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/create-or-delete-a-server-alias-for-use-by-a-client)

QuickRun.ps1 runs GenerateDatabases.ps1 script. For further details, please refer to the documentation into the Scripts folder.

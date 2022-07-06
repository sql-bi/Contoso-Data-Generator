# Scripts

## QuickRun.ps1

QuickRun purpose is to create the working folders and then run the **GenerateDatabase.ps1** script

it can be run in a PowerShell window in the Scripts folder with the command to accept default parameters:

.\QuickRun.ps1 

Or to specify the parameters by replacing C:\MYTEMP and MySqlInstance with the actual values to be used:

.\QuickRun.ps1 -SqlDataFilesFolder C:\MYTEMP -SqlServerInstance MySqlInstance

## GenerateDatabases.ps1

Generate several Contoso databases per run, by iterating over an array of configuration paramters to run in sequence

 - **DatabaseGenerator** to generate the csv files with the orders
 - SQL Script to create the datbase, import the data, backup the database and detach the .mdf and .ldf files
 - zip/7-Zip to compress the database files

 ## Parameters

 The parameters are documented into the script inside the **param()** section

 ## Configurations

 The configuration for the databases to be generated are specified as Tuples into the **$databases** array
 



param(
    [string]$SqlDataFilesFolder='C:\TEMP',
    [string]$SqlServerInstance='Demo'
)

Write-Host 'SqlDataFilesFolder:' $SqlDataFilesFolder
Write-Host 'SqlServerInstance:' $SqlServerInstance

if (! (Test-Path -Path $SqlDataFilesFolder))
{
    Write-Output "$SqlDataFilesFolder does not exist. Aborting"
    Exit 1
}

if (! $SqlDataFilesFolder)
{
    Write-Output "empty parameter specified"
    Exit 1
}

try 
{
    Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Query 'SELECT 1'  -ConnectionTimeout 10 | Out-Null
}
catch 
{
    Write-Host 'Error connecting to SQL Server instance' $SqlServerInstance 
    Exit 1
}

[string]$ContosoDatasetFolder="$($SqlDataFilesFolder)\ContosoDataset"
[string]$DatasetOutputFolder="$($ContosoDatasetFolder)\DatasetSqlbi"
[string]$SqlBackupFolder="$($ContosoDatasetFolder)\SqlBackup"


function CreateFolderIfNotExists ([string]$folder)
{
    if (!( Test-Path -Path $folder))
    {
        Write-Output "Creating folder $folder"
        New-Item -Path $folder -ItemType Directory
    }
}

CreateFolderIfNotExists $ContosoDatasetFolder
CreateFolderIfNotExists $DatasetOutputFolder
CreateFolderIfNotExists $SqlBackupFolder

.\GenerateDatabases.ps1 -SqlDataFilesFolder $SqlDataFilesFolder -SqlBackupFolder $SqlBackupFolder -DatasetOutputFolder $DatasetOutputFolder -SqlServerInstance $SqlServerInstance
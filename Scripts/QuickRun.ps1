param(
    [string]$SqlDataFilesFolder='C:\TEMP'
)

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

.\GenerateDatabases.ps1 -SqlDataFilesFolder $SqlDataFilesFolder -SqlBackupFolder $SqlBackupFolder -DatasetOutputFolder $DatasetOutputFolder
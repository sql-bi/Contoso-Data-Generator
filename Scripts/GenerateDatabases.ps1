#
# Instructions
#
# Create a temporary folder (like C:\Temp) and copy there the DatasetSqlbi folder
# This folder initially contains JSON/XLSX/RPT files. The script generates CSV
# files that are imported in SQL Server. These files can be large, so we prefer
# to use local folders that are not propagated to OneDrive.
#
# Once completed, copy the BAK/ZIP/7ZA files from the local backup folder to the 
# correspondent OneDrive folder when the files are final.
#
param(
    # Instance of SQL Server to process the database
    [String]$SqlServerInstance='Demo', 

    # Base Folder for Database Generator
    [String]$PathBase="$($PSScriptRoot)\..\", 

    # LOCAL Folder for the SQL Server scripts within the Database Generator base folder
    [String]$ScriptsFolder='Scripts\SqlGenScript',

    # Absolute Folder for Dataset - include XLSX/RPT/CSV file read by DatabaseGenerator.exe
    # This folder must have the following files:
    #   customers.rpt
    #   data.xlsx
    [String]$DatasetInputFolder="$($PSScriptRoot)\..\DatasetSqlbi", 

    # Absolute Folder for Dataset - write CSV file written by DatabaseGenerator.exe
    [String]$DatasetOutputFolder='C:\Temp\ContosoDatamart\DatasetSqlbi', 

    # Absolute Path for config file (JSON)
    [String]$ConfigFile="$($PSScriptRoot)\..\DatasetSqlbi\config.json", 

    # Destination Folder for SQL Server backup and ZIP files (ABSOLUTE path)
    [String]$SqlBackupFolder='C:\Temp\ContosoDatamart\SqlBackup',

    # Temporary folder to store MDF/LDF/BAK/ZIP files during processing
    # MUST BE a physical folder accessible by SQL Server, don't use user folders!
    [String]$SqlDataFilesFolder='C:\Temp',            

    #path to 7z.exe
    [string]$7zExe = "$($env:ProgramFiles)\7-Zip\7z.exe",

    #path to DatabaseGenerator executable
    [string]$databaseGeneratorExe = "$($PSScriptRoot)\..\DatabaseGenerator\bin\Release\netcoreapp3.1\DatabaseGenerator.exe"

)

# make sure the large customers csv files are uncompressed

$customersCsvPath = "$($PathBase)\Contoso Main\Customers from Fake Name Generator"
if ( ( Get-ChildItem $customersCsvPath -Filter *.csv | Measure-Object).Count -eq 0 )
{
    Get-ChildItem $customersCsvPath -Name -Filter *.csv.7z | ForEach-Object -Process { 
        & $7zExe e "$($customersCsvPath)\$($_)" "-o$($customersCsvPath)" 
    }
}


# Include the list of rows/database name for the database to generate
# Use 'TrimCustomers' to remove Customers that have no transactions in Sales
$databases = @()
$databases += [System.Tuple]::Create(16000, 'Contoso 10K', 'TrimCustomers', 'CutDateBefore=2017-05-18', 'CutDateAfter=2020-03-03', 'CustomerPercentage=0.05' )
# $databases += [System.Tuple]::Create(100000, 'Contoso 100K', 'TrimCustomers', 'CutDateBefore=2010-05-18', 'CutDateAfter=2020-03-03', 'CustomerPercentage=0.01')
# $databases += [System.Tuple]::Create(1000000, 'Contoso 1M', 'TrimCustomers', 'CutDateBefore=2010-05-18', 'CutDateAfter=2020-03-03', 'CustomerPercentage=0.05')
# $databases += [System.Tuple]::Create(4500000, 'Contoso 10M DimRatio', 'CutDateBefore=2010-05-18', 'CutDateAfter=2020-03-03', 'CustomerPercentage=0.80')
# $databases += [System.Tuple]::Create(4500000, 'Contoso 10M', 'TrimCustomers', 'CutDateBefore=2010-05-18', 'CutDateAfter=2020-03-03')
# $databases += [System.Tuple]::Create(100000000, 'Contoso 100M', 'TrimCustomers', 'CutDateBefore=2010-05-18', 'CutDateAfter=2020-03-03')
# $databases += [System.Tuple]::Create(1000000000, 'Contoso 1G', 'TrimCustomers', 'CutDateBefore=2010-05-18', 'CutDateAfter=2020-03-03')

$sqlScriptsFolder = $PathBase + $ScriptsFolder

$databases | ForEach-Object { 
    Write-Host *****************************************************************
    Write-Host Creating $_.Item2 with $_.Item1 Rows
    $GeneratorArguments = @($DatasetInputFolder,$DatasetOutputFolder,$ConfigFile)
    $GeneratorArguments += 'param:OrdersCount=' + $_.Item1
    $TrimCustomers = $false

    foreach($p in $_) {
        for ($i = 2; $i -lt $p.Length; $i++) {
            if ($p.Item($i) -eq 'TrimCustomers') {
                $TrimCustomers =$true
            }
            else {
                $GeneratorArguments += 'param:' + $p.Item($i)
            }
        }
    }
    
    Write-Host $databaseGeneratorExe $GeneratorArguments # $DatasetInputFolder $DatasetOutputFolder $ConfigFile $GeneratorArguments
    & $databaseGeneratorExe $GeneratorArguments # $DatasetInputFolder $DatasetOutputFolder $ConfigFile  param:CustomerPercentage=1 # $GeneratorArguments

    $DatabaseName = $_.Item2
    $SqlScripts = Get-ChildItem $sqlScriptsFolder -Name -Filter *.sql | Sort-Object | ForEach-Object -Process {
        Write-Host 'Executing '$_
        $Filename = $sqlScriptsFolder + '\' + $_
        $Original = Get-Content $Filename -raw
        $SqlCommand = $Original `
            -replace ('#DATABASE_NAME#',$DatabaseName) `
            -replace ('#PATH_BASE#',$PathBase) `
            -replace ('#DATASET_FOLDER#',$DatasetOutputFolder) `
            -replace ('#SQLDATA_FOLDER#',$SqlDataFilesFolder) `
            -replace ('#SQLBACKUP_FOLDER#',$SqlDataFilesFolder) `
            -replace ('#TRIM_CUSTOMERS#',$(if($TrimCustomers) {"(1=1)"} Else {"(1=0)"}) )

        Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Query $SqlCommand -QueryTimeout 60000 -ConnectionTimeout 60000
    }

    # Copy the SQL backup file
    $BakFile = $SqlDataFilesFolder + '\' + $DatabaseName + '.bak'
    Move-Item -Path $BakFile -Destination $SqlBackupFolder -Force

    # Zip the detached databases
    $SqlFilenames = $SqlDataFilesFolder + '\' + $DatabaseName + '*.?df'
    $LargeSqlFiles = Get-ChildItem $SqlFilenames | where { ($_.Length -cle 1900Mb) -and ($_.Name.EndsWith('mdf')) }
    $ZipExtension = '.7z'
    if (($LargeSqlFiles | Measure-Object).Count -gt 0) {
        $ZipExtension = '.zip'
    }
    $ZipFile = $SqlDataFilesFolder + '\DB ' + $DatabaseName + $ZipExtension
    try {
        if ($ZipExtension -eq '.zip') {
            Compress-Archive -Path $SqlFilenames -DestinationPath $ZipFile -Force
            Write-Host "Prepared ZIP " $ZipFile -ForegroundColor Green
        }
        else {
            & $7zExe a $ZipFile $SqlFilenames  
            Write-Host "Prepared 7z " $ZipFile -ForegroundColor Green
        }
        Move-Item -Path $ZipFile -Destination $SqlBackupFolder -Force

        # Remove the MDF/LDF files
        Remove-Item -Path $SqlFilenames -Recurse
    }
    catch {
        Write-Host "*** WARNING *** ZIP file not created for " + $SqlFilenames -ForegroundColor Yellow
    }

}

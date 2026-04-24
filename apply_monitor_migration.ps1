$DbPath = "fatale_core.db"
$SqliteUrl = "https://www.sqlite.org/2025/sqlite-tools-win-x64-3490000.zip"
$ZipFile = "sqlite_tools.zip"
$ExePath = "sqlite3.exe"

Write-Host "--- FATALE_CORE DB MIGRATION TOOL (POWERSHELL) ---" -ForegroundColor Cyan

# 1. Check for database
if (-not (Test-Path $DbPath)) {
    Write-Host "ERROR: $DbPath not found. Ensure you are in the project root." -ForegroundColor Red
    exit
}

# 2. Get SQLite CLI if missing
if (-not (Test-Path $ExePath)) {
    Write-Host "SQLite CLI not found. Downloading official tools from sqlite.org..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $SqliteUrl -OutFile $ZipFile -ErrorAction Stop
        Write-Host "Download complete. Extracting..."
        
        # Extract only sqlite3.exe to the current directory
        $Shell = New-Object -ComObject Shell.Application
        $ZipArchive = $Shell.NameSpace((Get-Item $ZipFile).FullName)
        $TargetFolder = (Get-Item ".").FullName
        
        # The zip usually contains a subfolder like 'sqlite-tools-win-x64-3490000/'
        $Items = $ZipArchive.Items()
        foreach ($Item in $Items) {
            if ($Item.IsFolder) {
                # Look inside the subfolder
                $SubItems = $Item.GetFolder.Items()
                foreach ($SubItem in $SubItems) {
                    if ($SubItem.Name -eq "sqlite3.exe") {
                        $SubItem.GetFolder().CopyHere($SubItem, 16) # 16 = Respond 'Yes to All'
                        # Move it to root if it was extracted into a subfolder
                        if (Test-Path "$($Item.Name)\sqlite3.exe") {
                            Move-Item "$($Item.Name)\sqlite3.exe" "." -Force
                            Remove-Item "$($Item.Name)" -Recurse -Force
                        }
                    }
                }
            } elseif ($Item.Name -eq "sqlite3.exe") {
                $ZipArchive.CopyHere($Item, 16)
            }
        }
        
        Remove-Item $ZipFile -Force
        Write-Host "SQLite CLI initialized." -ForegroundColor Green
    } catch {
        Write-Host "ERROR: Failed to download SQLite tools. Please ensure you have an internet connection." -ForegroundColor Red
        Write-Host $_
        exit
    }
}

# 3. Define SQL commands
$SqlCommands = @"
-- Add Monitor Styling columns
ALTER TABLE Users ADD COLUMN MonitorImageUrl TEXT;
ALTER TABLE Users ADD COLUMN MonitorBackgroundColor TEXT;
ALTER TABLE Users ADD COLUMN MonitorIsGlass INTEGER NOT NULL DEFAULT 0;

-- Add Status Message column
ALTER TABLE Users ADD COLUMN StatusMessage TEXT;

-- Record Migrations in EF History
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
SELECT '20260418030000_AddMonitorStyling', '10.0.6'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260418030000_AddMonitorStyling');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
SELECT '20260418040000_AddStatusMessage', '10.0.6'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260418040000_AddStatusMessage');
"@

# 4. Run Migration
Write-Host "Applying schema updates to $DbPath..." -ForegroundColor Yellow
$TmpSqlFile = "temp_migration.sql"
$SqlCommands | Out-File -FilePath $TmpSqlFile -Encoding utf8

try {
    # Run sqlite3 with the sql file. Ignore 'duplicate column' errors as it means column already exists.
    & .\$ExePath $DbPath ".read $TmpSqlFile" 2>&1 | Out-Default
    Write-Host "`nSUCCESS: Migration check complete." -ForegroundColor Green
    Write-Host "Your database is now synchronized with the latest styling and status updates." -ForegroundColor Cyan
} catch {
    Write-Host "ERROR: Failed to execute SQL commands." -ForegroundColor Red
    Write-Host $_
} finally {
    if (Test-Path $TmpSqlFile) { Remove-Item $TmpSqlFile }
}

Write-Host "`n--- PROCESS_COMPLETE ---" -ForegroundColor Cyan

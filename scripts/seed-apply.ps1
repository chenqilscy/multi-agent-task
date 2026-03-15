# CKY.MAF LLM Configuration Seed Data Apply Script (PowerShell)
# 此脚本用于应用LLM提供商配置种子数据
#
# 使用方法：
#   .\scripts\seed-apply.ps1

$ErrorActionPreference = "Stop"

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Detect database type
$DbType = $env:DB_TYPE
if ([string]::IsNullOrEmpty($DbType)) { $DbType = "sqlite" }

$ConnectionString = if ($env:CONNECTION_STRING) { $env:CONNECTION_STRING } else { "Data Source=$ProjectRoot\maf.db" }

Write-Host "=== CKY.MAF LLM Configuration Seeding ===" -ForegroundColor Green
Write-Host "Database Type: $DbType"
Write-Host "Connection String: $ConnectionString"
Write-Host ""

# Step 1: Backup existing data (if any)
Write-Host "Step 1: Backing up existing LLM configurations..." -ForegroundColor Yellow
if ($DbType -eq "sqlite") {
    $DbFile = "$ProjectRoot\maf.db"
    if (Test-Path $DbFile) {
        $BackupFile = "$ProjectRoot\maf_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"
        Copy-Item $DbFile $BackupFile
        Write-Host "Backup created: $BackupFile" -ForegroundColor Green
    } else {
        Write-Host "No existing database to backup" -ForegroundColor Yellow
    }
}
Write-Host ""

# Step 2: Check if LLM configs exist
Write-Host "Step 2: Checking existing LLM configurations..." -ForegroundColor Yellow
if ($DbType -eq "sqlite") {
    if (Test-Path $DbFile) {
        $ExistingConfigs = sqlite3 $DbFile "SELECT COUNT(*) FROM llm_provider_configs;" 2>$null
        if ($ExistingConfigs) {
            Write-Host "Existing LLM configurations: $ExistingConfigs"

            if ([int]$ExistingConfigs -gt 0) {
                Write-Host "Warning: Found existing LLM configurations" -ForegroundColor Yellow
                Write-Host "Options:"
                Write-Host "  1. Skip seeding (keep existing data)"
                Write-Host "  2. Update existing configurations (merge new values)"
                Write-Host "  3. Replace all configurations (clear and insert)"
                Write-Host ""
                $choice = Read-Host "Choose option (1/2/3) [1]"

                switch ($choice) {
                    "1" {
                        Write-Host "Skipping LLM configuration seeding..."
                        exit 0
                    }
                    "2" {
                        Write-Host "Updating existing configurations..."
                        $SqlFile = "$ScriptDir\seed-llm-configs.sql"
                    }
                    "3" {
                        Write-Host "Replacing all configurations..."
                        sqlite3 $DbFile "DELETE FROM llm_provider_configs;"
                        $SqlFile = "$ScriptDir\seed-llm-configs.sql"
                    }
                    default {
                        Write-Host "Invalid choice. Exiting..." -ForegroundColor Red
                        exit 1
                    }
                }
            } else {
                $SqlFile = "$ScriptDir\seed-llm-configs.sql"
            }
        } else {
            $SqlFile = "$ScriptDir\seed-llm-configs.sql"
        }
    } else {
        Write-Host "Error: Database file not found: $DbFile" -ForegroundColor Red
        Write-Host "Please run database migrations first:"
        Write-Host "  .\scripts\migrate-apply.ps1"
        exit 1
    }
} elseif ($DbType -eq "postgresql") {
    $SqlFile = "$ScriptDir\seed-llm-configs-postgresql.sql"
}
Write-Host ""

# Step 3: Check if SQL file exists
if (-not (Test-Path $SqlFile)) {
    Write-Host "Error: SQL file not found: $SqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "SQL file: $SqlFile" -ForegroundColor Blue
Write-Host ""

# Step 4: Warning about API keys
Write-Host "Step 3: IMPORTANT - API Key Configuration" -ForegroundColor Yellow
Write-Host "==================================="
Write-Host "Before proceeding, you MUST update the API keys in the SQL file!" -ForegroundColor Red
Write-Host ""
Write-Host "Please edit the SQL file and replace:"
Write-Host "  - YOUR_ZHIPUAI_API_KEY_HERE"
Write-Host "  - YOUR_TONGYI_API_KEY_HERE"
Write-Host "  - YOUR_WENXIN_API_KEY_HERE"
Write-Host "  - YOUR_XUNFEI_API_KEY_HERE"
Write-Host "  - YOUR_BAICHUAN_API_KEY_HERE"
Write-Host "  - YOUR_MINIMAX_API_KEY_HERE"
Write-Host ""
Write-Host "With your actual API keys from each provider."
Write-Host ""
$apiKeysUpdated = Read-Host "Have you updated the API keys? (yes/no) [no]"

if ($apiKeysUpdated -ne "yes") {
    Write-Host "Please update the API keys first, then run this script again." -ForegroundColor Red
    Write-Host ""
    Write-Host "Edit command:"
    Write-Host "  notepad $SqlFile"
    Write-Host "  or"
    Write-Host "  code $SqlFile"
    exit 1
}
Write-Host ""

# Step 5: Apply SQL seed data
Write-Host "Step 4: Applying LLM configuration seed data..." -ForegroundColor Yellow
if ($DbType -eq "sqlite") {
    sqlite3 $DbFile < $SqlFile
} elseif ($DbType -eq "postgresql") {
    # Extract connection parameters
    $parts = $ConnectionString.Split(';')
    $dbHost = "localhost"
    $dbPort = "5432"
    $dbName = "maf_db"
    $dbUser = "maf_user"

    foreach ($part in $parts) {
        $kv = $part.Split('=')
        if ($kv.Length -eq 2) {
            switch ($kv[0].Trim()) {
                "Host" { $dbHost = $kv[1].Trim() }
                "Port" { $dbPort = $kv[1].Trim() }
                "Database" { $dbName = $kv[1].Trim() }
                "User Id" { $dbUser = $kv[1].Trim() }
                "Username" { $dbUser = $kv[1].Trim() }
            }
        }
    }

    psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f $SqlFile
} else {
    Write-Host "Error: Unsupported database type: $DbType" -ForegroundColor Red
    Write-Host "Supported types: sqlite, postgresql"
    exit 1
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "=== LLM configuration seeding completed successfully ===" -ForegroundColor Green
} else {
    Write-Host "=== LLM configuration seeding failed ===" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 6: Verification
Write-Host "Step 5: Verifying seeded data..." -ForegroundColor Yellow
Write-Host "Configured LLM providers:"
if ($DbType -eq "sqlite") {
    sqlite3 $DbFile "SELECT provider_name, provider_display_name, model_id, is_enabled, priority FROM llm_provider_configs;"
} elseif ($DbType -eq "postgresql") {
    psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c "SELECT provider_name, provider_display_name, model_id, is_enabled, priority FROM llm_provider_configs;"
}
Write-Host ""

# Step 7: Enable providers reminder
Write-Host "Step 6: Next Steps" -ForegroundColor Yellow
Write-Host "==================================="
Write-Host "LLM configurations have been seeded but are currently DISABLED."
Write-Host ""
Write-Host "To enable a provider, run:"
if ($DbType -eq "sqlite") {
    Write-Host "  sqlite3 $DbFile"
    Write-Host ""
    Write-Host "Then execute:"
    Write-Host "  UPDATE llm_provider_configs SET is_enabled = 1 WHERE provider_name = 'zhipuai';"
} elseif ($DbType -eq "postgresql") {
    Write-Host "  psql -h $dbHost -p $dbPort -U $dbUser -d $dbName"
    Write-Host ""
    Write-Host "Then execute:"
    Write-Host "  UPDATE llm_provider_configs SET is_enabled = true WHERE provider_name = 'zhipuai';"
}
Write-Host ""
Write-Host "Or use the application's LLM configuration management interface."
Write-Host ""

Write-Host "=== Seeding process completed ===" -ForegroundColor Green

# CKY.MAF Database Migration Apply Script (PowerShell)
# This script applies all pending EF Core migrations to the database

$ErrorActionPreference = "Stop"

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$RepositoryProject = "$ProjectRoot\src\Infrastructure\Repository\CKY.MAF.Repository.csproj"
$ServicesProject = "$ProjectRoot\src\Services\CKY.MAF.Services.csproj"

# Default connection string (can be overridden via environment variable)
$ConnectionString = if ($env:CONNECTION_STRING) { $env:CONNECTION_STRING } else { "Data Source=maf.db" }
$MigrationOutputDir = if ($env:MIGRATION_OUTPUT_DIR) { $env:MIGRATION_OUTPUT_DIR } else { "Data/Migrations" }

Write-Host "=== CKY.MAF Database Migration Apply ===" -ForegroundColor Green
Write-Host "Repository Project: $RepositoryProject"
Write-Host "Services Project: $ServicesProject"
Write-Host "Connection String: $ConnectionString"
Write-Host ""

# Check if dotnet ef is installed
if (-not (Get-Command "dotnet-ef" -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet-ef not found, installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef --version 9.0.0
}

# Show pending migrations
Write-Host "Checking for pending migrations..." -ForegroundColor Yellow
try {
    dotnet ef migrations list --project "$RepositoryProject" --startup-project "$ServicesProject" --output-dir "$MigrationOutputDir" --connection "$ConnectionString" 2>$null
} catch {
    Write-Host "Note: Could not list migrations (this is okay for new databases)"
}
Write-Host ""

# Apply migrations
Write-Host "Applying migrations..." -ForegroundColor Green
dotnet ef database update --project "$RepositoryProject" --startup-project "$ServicesProject" --output-dir "$MigrationOutputDir" --connection "$ConnectionString"

if ($LASTEXITCODE -eq 0) {
    Write-Host "=== Migrations applied successfully ===" -ForegroundColor Green
    exit 0
} else {
    Write-Host "=== Migration failed ===" -ForegroundColor Red
    exit 1
}

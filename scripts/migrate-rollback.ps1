# CKY.MAF Database Migration Rollback Script (PowerShell)
# This script rolls back the last applied EF Core migration

$ErrorActionPreference = "Stop"

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$RepositoryProject = "$ProjectRoot\src\Infrastructure\Repository\CKY.MAF.Infrastructure.Repository.csproj"
$ServicesProject = "$ProjectRoot\src\Services\CKY.MAF.Services.csproj"

# Default connection string (can be overridden via environment variable)
$ConnectionString = if ($env:CONNECTION_STRING) { $env:CONNECTION_STRING } else { "Data Source=maf.db" }
$MigrationOutputDir = if ($env:MIGRATION_OUTPUT_DIR) { $env:MIGRATION_OUTPUT_DIR } else { "Data/Migrations" }

# Get target migration from command line argument (default: roll back to initial state)
$TargetMigration = if ($args.Count -gt 0) { $args[0] } else { "0" }

Write-Host "=== CKY.MAF Database Migration Rollback ===" -ForegroundColor Yellow
Write-Host "Repository Project: $RepositoryProject"
Write-Host "Services Project: $ServicesProject"
Write-Host "Connection String: $ConnectionString"
Write-Host "Target Migration: $TargetMigration"
Write-Host ""

# Check if dotnet ef is installed
if (-not (Get-Command "dotnet-ef" -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet-ef not found, installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef --version 9.0.0
}

# Rollback migration
Write-Host "Rolling back migration..." -ForegroundColor Green
dotnet ef database update $TargetMigration --project "$RepositoryProject" --startup-project "$ServicesProject"

if ($LASTEXITCODE -eq 0) {
    Write-Host "=== Migration rolled back successfully ===" -ForegroundColor Green
    exit 0
} else {
    Write-Host "=== Migration rollback failed ===" -ForegroundColor Red
    exit 1
}

#!/bin/bash
# CKY.MAF Database Migration Rollback Script
# This script rolls back the last applied EF Core migration

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
REPOSITORY_PROJECT="$PROJECT_ROOT/src/Infrastructure/Repository/CKY.MAF.Repository.csproj"
SERVICES_PROJECT="$PROJECT_ROOT/src/Services/CKY.MAF.Services.csproj"

# Default connection string (can be overridden via environment variable)
: "${CONNECTION_STRING:="Data Source=maf.db"}"
: "${MIGRATION_OUTPUT_DIR:="Data/Migrations"}"

# Allow specifying target migration (default: roll back one migration)
TARGET_MIGRATION="${1:-}"

echo -e "${YELLOW}=== CKY.MAF Database Migration Rollback ===${NC}"
echo "Repository Project: $REPOSITORY_PROJECT"
echo "Services Project: $SERVICES_PROJECT"
echo "Connection String: $CONNECTION_STRING"
if [ -n "$TARGET_MIGRATION" ]; then
    echo "Target Migration: $TARGET_MIGRATION"
else
    echo "Rolling back one migration..."
fi
echo ""

# Check if dotnet ef is installed
if ! command -v dotnet-ef &> /dev/null; then
    echo -e "${YELLOW}dotnet-ef not found, installing...${NC}"
    dotnet tool install --global dotnet-ef --version 9.0.0
fi

# Rollback migration
echo -e "${GREEN}Rolling back migration...${NC}"
if [ -n "$TARGET_MIGRATION" ]; then
    dotnet ef database update "$TARGET_MIGRATION" --project "$REPOSITORY_PROJECT" --startup-project "$SERVICES_PROJECT" --output-dir "$MIGRATION_OUTPUT_DIR" --connection "$CONNECTION_STRING"
else
    dotnet ef database update 0 --project "$REPOSITORY_PROJECT" --startup-project "$SERVICES_PROJECT" --output-dir "$MIGRATION_OUTPUT_DIR" --connection "$CONNECTION_STRING"
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}=== Migration rolled back successfully ===${NC}"
    exit 0
else
    echo -e "${RED}=== Migration rollback failed ===${NC}"
    exit 1
fi

#!/bin/bash
# CKY.MAF Database Migration Apply Script
# This script applies all pending EF Core migrations to the database

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
REPOSITORY_PROJECT="$PROJECT_ROOT/src/Infrastructure/Repository/CKY.MAF.Infrastructure.Repository.csproj"
SERVICES_PROJECT="$PROJECT_ROOT/src/Services/CKY.MAF.Services.csproj"

# Default connection string (can be overridden via environment variable)
: "${CONNECTION_STRING:="Data Source=maf.db"}"
: "${MIGRATION_OUTPUT_DIR:="Data/Migrations"}"

echo -e "${GREEN}=== CKY.MAF Database Migration Apply ===${NC}"
echo "Repository Project: $REPOSITORY_PROJECT"
echo "Services Project: $SERVICES_PROJECT"
echo "Connection String: $CONNECTION_STRING"
echo ""

# Check if dotnet ef is installed
if ! command -v dotnet-ef &> /dev/null; then
    echo -e "${YELLOW}dotnet-ef not found, installing...${NC}"
    dotnet tool install --global dotnet-ef --version 9.0.0
fi

# Show pending migrations
echo -e "${YELLOW}Checking for pending migrations...${NC}"
dotnet ef migrations list --project "$REPOSITORY_PROJECT" --startup-project "$SERVICES_PROJECT" || true
echo ""

# Apply migrations
echo -e "${GREEN}Applying migrations...${NC}"
dotnet ef database update --project "$REPOSITORY_PROJECT" --startup-project "$SERVICES_PROJECT"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}=== Migrations applied successfully ===${NC}"
    exit 0
else
    echo -e "${RED}=== Migration failed ===${NC}"
    exit 1
fi

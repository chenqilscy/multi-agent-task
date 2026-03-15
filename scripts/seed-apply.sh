#!/bin/bash
# CKY.MAF LLM Configuration Seed Data Apply Script
# 此脚本用于应用LLM提供商配置种子数据
#
# 使用方法：
#   bash scripts/seed-apply.sh

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Detect database type
DB_TYPE="${DB_TYPE:-sqlite}"
CONNECTION_STRING="${CONNECTION_STRING:-Data Source=$PROJECT_ROOT/maf.db}"

echo -e "${GREEN}=== CKY.MAF LLM Configuration Seeding ===${NC}"
echo "Database Type: $DB_TYPE"
echo "Connection String: $CONNECTION_STRING"
echo ""

# Check if database file exists (SQLite)
if [ "$DB_TYPE" = "sqlite" ]; then
    DB_FILE="$PROJECT_ROOT/maf.db"
    if [ ! -f "$DB_FILE" ]; then
        echo -e "${RED}Error: Database file not found: $DB_FILE${NC}"
        echo "Please run database migrations first:"
        echo "  bash scripts/migrate-apply.sh"
        exit 1
    fi
    echo -e "${BLUE}Database file found: $DB_FILE${NC}"
fi

# Step 1: Backup existing data (if any)
echo -e "${YELLOW}Step 1: Backing up existing LLM configurations...${NC}"
BACKUP_FILE="$PROJECT_ROOT/maf_backup_$(date +%Y%m%d_%H%M%S).db"
if [ "$DB_TYPE" = "sqlite" ]; then
    cp "$DB_FILE" "$BACKUP_FILE" 2>/dev/null || echo "No existing database to backup"
    echo "Backup created: $BACKUP_FILE"
fi
echo ""

# Step 2: Check if LLM configs exist
echo -e "${YELLOW}Step 2: Checking existing LLM configurations...${NC}"
if [ "$DB_TYPE" = "sqlite" ]; then
    EXISTING_CONFIGS=$(sqlite3 "$DB_FILE" "SELECT COUNT(*) FROM llm_provider_configs;" 2>/dev/null || echo "0")
    echo "Existing LLM configurations: $EXISTING_CONFIGS"

    if [ "$EXISTING_CONFIGS" -gt 0 ]; then
        echo -e "${YELLOW}Warning: Found existing LLM configurations${NC}"
        echo "Options:"
        echo "  1. Skip seeding (keep existing data)"
        echo "  2. Update existing configurations (merge new values)"
        echo "  3. Replace all configurations (clear and insert)"
        echo ""
        read -p "Choose option (1/2/3) [1]: " choice
        choice=${choice:-1}

        case $choice in
            1)
                echo "Skipping LLM configuration seeding..."
                exit 0
                ;;
            2)
                echo "Updating existing configurations..."
                SQL_FILE="$SCRIPT_DIR/seed-llm-configs.sql"
                ;;
            3)
                echo "Replacing all configurations..."
                sqlite3 "$DB_FILE" "DELETE FROM llm_provider_configs;"
                SQL_FILE="$SCRIPT_DIR/seed-llm-configs.sql"
                ;;
            *)
                echo "Invalid choice. Exiting..."
                exit 1
                ;;
        esac
    else
        SQL_FILE="$SCRIPT_DIR/seed-llm-configs.sql"
    fi
elif [ "$DB_TYPE" = "postgresql" ]; then
    SQL_FILE="$SCRIPT_DIR/seed-llm-configs-postgresql.sql"
fi
echo ""

# Step 3: Check if SQL file exists
if [ ! -f "$SQL_FILE" ]; then
    echo -e "${RED}Error: SQL file not found: $SQL_FILE${NC}"
    exit 1
fi

echo -e "${BLUE}SQL file: $SQL_FILE${NC}"
echo ""

# Step 4: Warning about API keys
echo -e "${YELLOW}Step 3: IMPORTANT - API Key Configuration${NC}"
echo "==================================="
echo -e "${RED}Before proceeding, you MUST update the API keys in the SQL file!${NC}"
echo ""
echo "Please edit the SQL file and replace:"
echo "  - YOUR_ZHIPUAI_API_KEY_HERE"
echo "  - YOUR_TONGYI_API_KEY_HERE"
echo "  - YOUR_WENXIN_API_KEY_HERE"
echo "  - YOUR_XUNFEI_API_KEY_HERE"
echo "  - YOUR_BAICHUAN_API_KEY_HERE"
echo "  - YOUR_MINIMAX_API_KEY_HERE"
echo ""
echo "With your actual API keys from each provider."
echo ""
read -p "Have you updated the API keys? (yes/no) [no]: " api_keys_updated
api_keys_updated=${api_keys_updated:-no}

if [ "$api_keys_updated" != "yes" ]; then
    echo -e "${RED}Please update the API keys first, then run this script again.${NC}"
    echo ""
    echo "Edit commands:"
    if [ "$DB_TYPE" = "sqlite" ]; then
        echo "  nano $SQL_FILE"
        echo "  vim $SQL_FILE"
    elif [ "$DB_TYPE" = "postgresql" ]; then
        echo "  nano $SQL_FILE"
        echo "  vim $SQL_FILE"
    fi
    exit 1
fi
echo ""

# Step 5: Apply SQL seed data
echo -e "${YELLOW}Step 4: Applying LLM configuration seed data...${NC}"
if [ "$DB_TYPE" = "sqlite" ]; then
    sqlite3 "$DB_FILE" < "$SQL_FILE"
elif [ "$DB_TYPE" = "postgresql" ]; then
    # Extract connection parameters
    IFS=':' read -ra ADDR <<< "$CONNECTION_STRING"
    DB_HOST=${ADDR[0]:-localhost}
    DB_PORT=${ADDR[1]:-5432}
    DB_NAME=${ADDR[2]:-maf_db}
    DB_USER=${ADDR[3]:-maf_user}

    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$SQL_FILE"
else
    echo -e "${RED}Error: Unsupported database type: $DB_TYPE${NC}"
    echo "Supported types: sqlite, postgresql"
    exit 1
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}=== LLM configuration seeding completed successfully ===${NC}"
else
    echo -e "${RED}=== LLM configuration seeding failed ===${NC}"
    exit 1
fi
echo ""

# Step 6: Verification
echo -e "${YELLOW}Step 5: Verifying seeded data...${NC}"
if [ "$DB_TYPE" = "sqlite" ]; then
    echo "Configured LLM providers:"
    sqlite3 "$DB_FILE" "SELECT provider_name, provider_display_name, model_id, is_enabled, priority FROM llm_provider_configs;"
elif [ "$DB_TYPE" = "postgresql" ]; then
    echo "Configured LLM providers:"
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT provider_name, provider_display_name, model_id, is_enabled, priority FROM llm_provider_configs;"
fi
echo ""

# Step 7: Enable providers reminder
echo -e "${YELLOW}Step 6: Next Steps${NC}"
echo "==================================="
echo "LLM configurations have been seeded but are currently DISABLED."
echo ""
echo "To enable a provider, run:"
if [ "$DB_TYPE" = "sqlite" ]; then
    echo "  sqlite3 $DB_FILE"
    echo ""
    echo "Then execute:"
    echo "  UPDATE llm_provider_configs SET is_enabled = 1 WHERE provider_name = 'zhipuai';"
elif [ "$DB_TYPE" = "postgresql" ]; then
    echo "  psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME"
    echo ""
    echo "Then execute:"
    echo "  UPDATE llm_provider_configs SET is_enabled = true WHERE provider_name = 'zhipuai';"
fi
echo ""
echo "Or use the application's LLM configuration management interface."
echo ""

echo -e "${GREEN}=== Seeding process completed ===${NC}"

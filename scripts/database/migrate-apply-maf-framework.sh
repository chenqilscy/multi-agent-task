#!/bin/bash
# ============================================================================
-- CKY.MAF 框架数据库初始化脚本
-- ============================================================================
-- 功能: 应用 MAF 框架的所有数据库表结构
-- 使用: ./migrate-apply-maf-framework.sh
-- ============================================================================

set -e  # 遇到错误立即退出

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 配置
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")/../.."
DB_PATH="${PROJECT_ROOT}/maf.db"

echo -e "${GREEN}=== CKY.MAF 框架数据库初始化 ===${NC}"
echo "数据库路径: $DB_PATH"
echo ""

# 检查数据库文件是否存在
if [ -f "$DB_PATH" ]; then
    echo -e "${YELLOW}警告: 数据库文件已存在！${NC}"
    read -p "是否继续？这可能会覆盖现有数据 (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "操作已取消"
        exit 1
    fi
    echo ""
fi

# 应用表结构
echo -e "${GREEN}正在应用 MAF 框架表结构...${NC}"
sqlite3 "$DB_PATH" < "${SCRIPT_DIR}/maf-framework/001_create_maf_framework_tables.sql"

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}=== 数据库初始化完成 ===${NC}"
    echo ""
    echo "验证安装:"
    echo "  MainTasks: $(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM MainTasks;")"
    echo "  SubTasks: $(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM SubTasks;")"
    echo "  MafAiSessions: $(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM MafAiSessions;")"
    echo "  ChatMessages: $(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM ChatMessages;")"
    echo ""
    echo -e "${GREEN}数据库已就绪！${NC}"
else
    echo -e "${RED}=== 数据库初始化失败 ===${NC}"
    exit 1
fi

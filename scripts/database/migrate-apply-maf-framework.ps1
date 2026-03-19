# ============================================================================
# CKY.MAF 框架数据库初始化脚本 (PowerShell)
# ============================================================================
# 功能: 应用 MAF 框架的所有数据库表结构
# 使用: .\migrate-apply-maf-framework.ps1
# ============================================================================

$ErrorActionPreference = "Stop"

# 配置
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$DbPath = Join-Path $ProjectRoot "maf.db"
$SqlScript = Join-Path $ScriptDir "maf-framework\001_create_maf_framework_tables.sql"

Write-Host "=== CKY.MAF 框架数据库初始化 ===" -ForegroundColor Green
Write-Host "数据库路径: $DbPath"
Write-Host ""

# 检查 SQL 脚本是否存在
if (-not (Test-Path $SqlScript)) {
    Write-Host "错误: SQL 脚本文件不存在: $SqlScript" -ForegroundColor Red
    exit 1
}

# 检查数据库文件是否存在
if (Test-Path $DbPath) {
    Write-Host "警告: 数据库文件已存在！" -ForegroundColor Yellow
    $response = Read-Host "是否继续？这可能会覆盖现有数据 (y/N)"
    if ($response -ne "y" -and $response -ne "Y") {
        Write-Host "操作已取消"
        exit 1
    }
    Write-Host ""
}

# 检查 SQLite 是否可用
try {
    $null = sqlite3 --version
} catch {
    Write-Host "错误: 未找到 SQLite3 命令" -ForegroundColor Red
    Write-Host ""
    Write-Host "请安装 SQLite3："
    Write-Host "  Windows: 下载 https://www.sqlite.org/download.html"
    Write-Host "  Linux:   sudo apt-get install sqlite3"
    Write-Host "  Mac:     brew install sqlite3"
    exit 1
}

# 应用表结构
Write-Host "正在应用 MAF 框架表结构..." -ForegroundColor Green
sqlite3 "$DbPath" < "$SqlScript"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== 数据库初始化完成 ===" -ForegroundColor Green
    Write-Host ""

    # 验证安装
    Write-Host "验证安装:" -ForegroundColor Cyan
    $tables = @("MainTasks", "SubTasks", "MafAiSessions", "ChatMessages", "SchemaVersion")
    foreach ($table in $tables) {
        $count = sqlite3 "$DbPath" "SELECT COUNT(*) FROM $table;"
        Write-Host "  $table : $count"
    }
    Write-Host ""
    Write-Host "数据库已就绪！" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=== 数据库初始化失败 ===" -ForegroundColor Red
    exit 1
}

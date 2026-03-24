# LLM 提供商配置初始化指南

本文档说明如何初始化和配置CKY.MAF的LLM提供商配置。

---

## 📋 概述

CKY.MAF使用数据库存储所有LLM提供商配置，包括：
- API密钥
- 模型ID和参数
- 支持的场景类型
- 优先级和启用状态

**关键设计**：
- ✅ 配置存储在数据库的 `llm_provider_configs` 表中
- ✅ 支持动态配置，无需重启应用
- ✅ 支持多个提供商和自动降级（fallback）
- ❌ 不支持环境变量或配置文件方式（设计决策）

---

## 🚀 快速开始

### 步骤1: 应用数据库迁移

首先确保数据库架构已创建：

```bash
# Linux/Mac
bash scripts/migrate-apply.sh

# Windows PowerShell
.\scripts\migrate-apply.ps1
```

### 步骤2: 配置LLM提供商

#### 方式1: 使用种子数据脚本（推荐）

```bash
# 2.1 编辑SQL脚本，替换API密钥
nano scripts/seed-llm-configs.sql        # SQLite
# 或
nano scripts/seed-llm-configs-postgresql.sql # PostgreSQL

# 2.2 执行种子数据脚本
bash scripts/seed-apply.sh    # Linux/Mac
.\scripts\seed-apply.ps1      # Windows PowerShell
```

#### 方式2: 手动插入配置

```sql
-- SQLite
sqlite3 maf.db

INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, is_enabled, priority, created_at)
VALUES
('zhipuai', '智谱AI', 'https://open.bigmodel.cn/api/paas/v4/', 'YOUR_API_KEY', 'glm-4', 'GLM-4', '[1,2,3,4,5]', 0, 1, datetime('now'));
```

### 步骤3: 启用提供商

```sql
-- 启用智谱AI
UPDATE llm_provider_configs SET is_enabled = 1 WHERE provider_name = 'zhipuai';

-- 验证配置
SELECT provider_name, provider_display_name, model_id, is_enabled, priority
FROM llm_provider_configs
WHERE is_enabled = 1
ORDER BY priority;
```

---

## 🔧 配置详解

### 支持的LLM提供商

| 提供商 | provider_name | 推荐模型 | 官网 |
|--------|--------------|---------|------|
| 智谱AI | `zhipuai` | glm-4 | https://open.bigmodel.cn/ |
| 通义千问 | `tongyi` | qwen-max | https://dashscope.aliyun.com/ |
| 文心一言 | `wenxin` | ernie-bot-4 | https://cloud.baidu.com/product/wenxinworkshop |
| 讯飞星火 | `xunfei` | spark-3.5 | https://www.xfyun.cn/ |
| 百川 | `baichuan` | Baichuan2-Turbo | https://www.baichuan-ai.com/ |
| MiniMax | `minimax` | abab6.5s-chat | https://www.minimax.chat/ |

### 场景类型说明

supported_scenarios_json 数组对应：
- `1` = Chat (聊天对话)
- `2` = Completion (文本补全)
- `3` = Embedding (向量化)
- `4` = Summarization (摘要)
- `5` = Translation (翻译)

### 优先级和降级

- `priority` 数字越小优先级越高
- 系统会自动选择优先级最高的可用提供商
- 如果主提供商失败，自动降级到次优提供商

---

## 📝 配置管理

### 查看所有配置

```sql
-- 查看所有提供商配置
SELECT
    provider_name,
    provider_display_name,
    model_id,
    is_enabled,
    priority,
    created_at
FROM llm_provider_configs
ORDER BY priority;
```

### 更新API密钥

```sql
-- 更新智谱AI的API密钥
UPDATE llm_provider_configs
SET api_key = 'YOUR_NEW_API_KEY',
    updated_at = datetime('now')
WHERE provider_name = 'zhipuai';
```

### 切换模型

```sql
-- 切换智谱AI到GLM-4-Plus
UPDATE llm_provider_configs
SET model_id = 'glm-4-plus',
    model_display_name = 'GLM-4-Plus',
    updated_at = datetime('now')
WHERE provider_name = 'zhipuai';
```

### 启用/禁用提供商

```sql
-- 启用提供商
UPDATE llm_provider_configs SET is_enabled = 1 WHERE provider_name = 'zhipuai';

-- 禁用提供商
UPDATE llm_provider_configs SET is_enabled = 0 WHERE provider_name = 'zhipuai';
```

### 调整优先级

```sql
-- 将通义千问设为最高优先级（优先于智谱AI）
UPDATE llm_provider_configs SET priority = 0 WHERE provider_name = 'tongyi';

-- 将智谱AI设为次优先级
UPDATE llm_provider_configs SET priority = 2 WHERE provider_name = 'zhipuai';
```

---

## 🛠️ 高级配置

### 自定义API端点

```sql
-- 使用自定义API端点（如代理服务器）
UPDATE llm_provider_configs
SET api_base_url = 'https://your-proxy.com/api/',
    additional_parameters_json = '{"timeout": 60, "retry": 3}'
WHERE provider_name = 'zhipuai';
```

### 调整模型参数

```sql
-- 调整温度参数（控制随机性）
UPDATE llm_provider_configs
SET temperature = 0.9  -- 0.0-2.0，越高越随机
WHERE provider_name = 'zhipuai';

-- 调整最大Token数
UPDATE llm_provider_configs
SET max_tokens = 200000  -- 根据模型限制调整
WHERE provider_name = 'zhipuai';
```

---

## 🔒 安全建议

### API密钥安全

1. **生产环境**: 使用密钥管理服务（如 Azure Key Vault）
2. **开发环境**: 使用测试密钥，限制配额
3. **权限控制**: 设置数据库访问权限，限制API密钥查看

### 密钥轮换

```sql
-- 定期轮换API密钥
UPDATE llm_provider_configs
SET api_key = 'YOUR_NEW_API_KEY',
    notes = '密钥已轮换 - 2026-03-16'
WHERE provider_name = 'zhipuai';
```

---

## 🧪 测试配置

### 验证配置是否生效

```bash
# 1. 启动应用
dotnet run --project src/Demos/SmartHome

# 2. 查看日志，确认LLM配置已加载
# 日志应显示: "Loaded LLM provider: zhipuai (glm-4)"

# 3. 测试LLM调用
# 在应用界面发送测试消息
```

### 查看使用统计

```sql
-- 查看最后使用时间
SELECT
    provider_name,
    provider_display_name,
    last_used_at,
    created_at
FROM llm_provider_configs
WHERE is_enabled = 1
ORDER BY last_used_at DESC;
```

---

## ❓ 常见问题

### Q: 应用启动时提示"没有可用的LLM配置"

**A**: 检查以下几点：
1. 数据库迁移是否已应用：`sqlite3 maf.db "SELECT COUNT(*) FROM llm_provider_configs;"`
2. 是否有启用的提供商：`sqlite3 maf.db "SELECT * FROM llm_provider_configs WHERE is_enabled = 1;"`
3. API密钥是否正确配置

### Q: 如何添加新的LLM提供商？

**A**:
1. 在数据库中插入新配置
2. 或使用应用的管理界面（如果已实现）
3. 确保 provider_name 唯一

### Q: 如何在多个提供商之间切换？

**A**:
1. 调整 priority 值
2. 或启用/禁用特定的提供商
3. 系统会自动选择优先级最高的可用提供商

### Q: 数据库迁移会删除我的配置吗？

**A**:
- ✅ 正常情况下不会。EF Core迁移会保留数据。
- ⚠️ 如果迁移删除了表，需要重新配置。
- 建议：迁移前备份数据库。

---

## 📚 相关文档

- [架构设计规范](../design-docs/core-architecture.md)
- [接口设计规范](../design-docs/implementation-guide.md)
- [错误处理指南](../design-docs/error-handling.md)

---

**最后更新**: 2026-03-16
**维护者**: CKY.MAF团队

# 📁 LLM配置模板文件说明

此目录包含LLM提供商配置的模板文件。

## 🔒 安全警告

**⚠️ 重要安全须知**：

- 模板文件包含占位符（YOUR_*_API_KEY_HERE）
- **绝对不要**在模板文件中填写真实API密钥
- **绝对不要**将包含真实密钥的文件提交到Git

## 📋 文件说明

### seed-llm-configs-template.sql
- **作用**: SQL模板文件
- **用途**: 作为配置示例和本地使用的模板
- **安全**: 仅包含占位符，可以安全提交

### seed-llm-configs-local.sql (本地文件)
- **生成方式**: `cp seed-llm-configs-template.sql seed-llm-configs-local.sql`
- **用途**: 本地开发/测试使用，包含真实API密钥
- **安全**: 已在 `.gitignore` 中排除，不会被提交

## 🚀 使用步骤

### 步骤1: 创建本地配置文件

```bash
# Linux/Mac
cp scripts/seed-llm-configs-template.sql scripts/seed-llm-configs-local.sql

# Windows
copy scripts\seed-llm-configs-template.sql scripts\seed-llm-configs-local.sql
```

### 步骤2: 编辑本地配置文件

```bash
# 使用您喜欢的编辑器
nano scripts/seed-llm-configs-local.sql
# 或
vim scripts/seed-llm-configs-local.sql
```

**替换以下占位符**：
- `YOUR_ZHIPUAI_API_KEY_HERE` → 您的智谱AI API密钥
- `YOUR_TONGYI_API_KEY_HERE` → 您的通义千问 API密钥
- `YOUR_WENXIN_API_KEY_HERE` → 您的文心一言 API密钥
- `YOUR_XUNFEI_API_KEY_HERE` → 您的讯飞星火 API密钥
- `YOUR_BAICHUAN_API_KEY_HERE` → 您的百川 API密钥
- `YOUR_MINIMAX_API_KEY_HERE` → 您的MiniMax API密钥

### 步骤3: 应用配置到数据库

```bash
# 执行种子数据脚本
bash scripts/seed-apply.sh
```

## ✅ 安全检查清单

在提交代码前，确保：

- [ ] 没有在 `seed-llm-configs-template.sql` 中填写真实密钥
- [ ] 没有意外提交 `seed-llm-configs-local.sql`
- [ ] `.gitignore` 包含 `scripts/seed-llm-configs-local.sql`
- [ ] 本地配置文件的权限设置正确（仅用户可读写）

## 🔒 权限设置

```bash
# 设置本地配置文件权限（仅用户可读写）
chmod 600 scripts/seed-llm-configs-local.sql
```

## 📝 最佳实践

1. **生产环境**: 使用密钥管理服务（如 Azure Key Vault、HashiCorp Vault）
2. **开发环境**: 使用测试密钥，限制配额
3. **团队协作**: 通过安全渠道分享配置模板，不共享真实密钥

---

**如有安全问题，请立即报告给团队安全负责人。**
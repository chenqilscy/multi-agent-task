# CKY.MAF LLM Provider Configuration Seed Data Template
# 此文件是模板，请复制并重命名为 seed-llm-configs-local.sql
#
# 安全警告：
# ⚠️ 此文件包含敏感的API密钥信息，绝对不要提交到Git仓库！
# ⚠️ .gitignore 已配置忽略此文件
# ⚠️ 请妥善保管包含真实API密钥的本地版本
#
# 使用方法：
# 1. 复制此模板：cp scripts/seed-llm-configs-template.sql scripts/seed-llm-configs-local.sql
# 2. 编辑本地版本，替换API密钥：nano scripts/seed-llm-configs-local.sql
# 3. 应用种子数据：sqlite3 maf.db < scripts/seed-llm-configs-local.sql

-- ============================================
-- 智谱AI (GLM-4/GLM-4-Plus)
-- ============================================
INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, max_tokens, temperature, is_enabled, priority, cost_per_1k_tokens, created_at)
VALUES
('zhipuai', '智谱AI', 'https://open.bigmodel.cn/api/paas/v4/', 'YOUR_ZHIPUAI_API_KEY_HERE', 'glm-4', 'GLM-4', '[1,2,3,4,5]', 128000, 0.7, 0, 1, 0.012, datetime('now'))
ON CONFLICT (provider_name) DO UPDATE SET
  api_key = excluded.api_key,
  is_enabled = excluded.is_enabled,
  priority = excluded.priority,
  updated_at = datetime('now');

-- ============================================
-- 通义千问 (Qwen)
-- ============================================
INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, max_tokens, temperature, is_enabled, priority, cost_per_1k_tokens, created_at)
VALUES
('tongyi', '通义千问', 'https://dashscope.aliyuncs.com/compatible-mode/v1', 'YOUR_TONGYI_API_KEY_HERE', 'qwen-max', 'Qwen-Max', '[1,2,3,4,5]', 30000, 0.7, 0, 2, 0.008, datetime('now'))
ON CONFLICT (provider_name) DO UPDATE SET
  api_key = excluded.api_key,
  is_enabled = excluded.is_enabled,
  priority = excluded.priority,
  updated_at = datetime('now');

-- ============================================
-- 文心一言 (ERNIE)
-- ============================================
INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, max_tokens, temperature, is_enabled, priority, cost_per_1k_tokens, created_at)
VALUES
('wenxin', '文心一言', 'https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat', 'YOUR_WENXIN_API_KEY_HERE', 'ernie-bot-4', 'ERNIE-Bot 4', '[1,2,3,4,5]', 128000, 0.7, 0, 3, 0.015, datetime('now'))
ON CONFLICT (provider_name) DO UPDATE SET
  api_key = excluded.api_key,
  is_enabled = excluded.is_enabled,
  priority = excluded.priority,
  updated_at = datetime('now');

-- ============================================
-- 讯飞星火 (Spark)
-- ============================================
INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, max_tokens, temperature, is_enabled, priority, cost_per_1k_tokens, created_at)
VALUES
('xunfei', '讯飞星火', 'https://spark-api.xf-yun.com/v3.5/chat', 'YOUR_XUNFEI_API_KEY_HERE', 'spark-3.5', 'Spark 3.5', '[1,2,3,4,5]', 16000, 0.7, 0, 4, 0.018, datetime('now'))
ON CONFLICT (provider_name) DO UPDATE SET
  api_key = excluded.api_key,
  is_enabled = excluded.is_enabled,
  priority = excluded.priority,
  updated_at = datetime('now');

-- ============================================
-- 百川 (Baichuan)
-- ============================================
INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, max_tokens, temperature, is_enabled, priority, cost_per_1k_tokens, created_at)
VALUES
('baichuan', '百川', 'https://api.baichuan-ai.com/v1', 'YOUR_BAICHUAN_API_KEY_HERE', 'Baichuan2-Turbo', 'Baichuan2-Turbo', '[1,2,3,4,5]', 32000, 0.7, 0, 5, 0.010, datetime('now'))
ON CONFLICT (provider_name) DO UPDATE SET
  api_key = excluded.api_key,
  is_enabled = excluded.is_enabled,
  priority = excluded.priority,
  updated_at = datetime('now');

-- ============================================
-- MiniMax
-- ============================================
INSERT INTO llm_provider_configs
(provider_name, provider_display_name, api_base_url, api_key, model_id, model_display_name, supported_scenarios_json, max_tokens, temperature, is_enabled, priority, cost_per_1k_tokens, created_at)
VALUES
('minimax', 'MiniMax', 'https://api.minimax.chat/v1', 'YOUR_MINIMAX_API_KEY_HERE', 'abab6.5s-chat', 'abab6.5s-chat', '[1,2,3,4,5]', 32000, 0.7, 0, 6, 0.015, datetime('now'))
ON CONFLICT (provider_name) DO UPDATE SET
  api_key = excluded.api_key,
  is_enabled = excluded.is_enabled,
  priority = excluded.priority,
  updated_at = datetime('now');

-- ============================================
-- 启用说明
-- ============================================
-- 1. 将上述 YOUR_*_API_KEY_HERE 替换为您的实际API密钥
-- 2. 根据需要启用提供商（设置 is_enabled = 1）
-- 3. 执行脚本：sqlite3 maf.db < scripts/seed-llm-configs-local.sql
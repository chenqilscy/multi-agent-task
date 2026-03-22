using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services
{
    /// <summary>
    /// 知识库种子数据服务
    /// 在应用启动时将预置的智能家居知识文档摄入RAG管线
    /// </summary>
    public class KnowledgeBaseSeedService : IHostedService
    {
        private readonly IRagPipeline _ragPipeline;
        private readonly ILogger<KnowledgeBaseSeedService> _logger;

        public KnowledgeBaseSeedService(
            IRagPipeline ragPipeline,
            ILogger<KnowledgeBaseSeedService> logger)
        {
            _ragPipeline = ragPipeline;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _logger.LogInformation("开始初始化智能家居知识库...");

            try
            {
                foreach (var doc in GetSeedDocuments())
                {
                    await _ragPipeline.IngestAsync(
                        doc.Id,
                        doc.Content,
                        KnowledgeBaseAgent.CollectionName,
                        new ChunkingConfig { MaxChunkSize = 300, OverlapRatio = 0.15 },
                        ct);
                }

                _logger.LogInformation("知识库初始化完成，已摄入 {Count} 篇文档", GetSeedDocuments().Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "知识库初始化失败，知识库查询功能可能不可用");
            }
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

        private static List<SeedDocument> GetSeedDocuments() =>
        [
            new("lighting-guide", """
                智能照明控制使用指南

                功能概述：
                智能照明系统支持对家中各房间的灯光进行精确控制，包括开关灯、调节亮度和设置颜色。

                支持的房间：
                客厅、卧室、厨房、浴室、书房、餐厅、阳台。

                基本命令：
                - 打开灯 / 开灯：打开指定房间的灯光
                - 关闭灯 / 关灯：关闭指定房间的灯光
                - 调亮：将灯光亮度调到100%
                - 调暗：将灯光亮度调到30%

                使用示例：
                "打开客厅的灯" —— 打开客厅灯光
                "关闭卧室的灯" —— 关闭卧室灯光
                "把书房的灯调暗" —— 将书房灯光调暗至30%
                """),

            new("climate-guide", """
                空调和气候控制使用指南

                功能概述：
                气候控制系统支持温度设定、模式切换等功能，让您随时调整室内温度。

                支持的模式：
                - 制冷模式（cooling）：夏季降温使用
                - 制热模式（heating）：冬季取暖使用
                - 送风模式（fan）：仅通风不调温
                - 自动模式（auto）：系统自动根据室温调节

                基本命令：
                - 设置温度：将空调调到指定温度
                - 切换模式：设置为制冷/制热/送风/自动模式
                - 打开/关闭空调

                使用示例：
                "把空调温度调到26度" —— 将空调设定为26°C
                "空调切换到制热模式" —— 切换到制热模式
                "客厅空调调到22度" —— 设置客厅温度为22°C
                """),

            new("music-guide", """
                音乐播放控制使用指南

                功能概述：
                智能音乐系统支持播放、暂停和切换歌曲等基本操作。

                基本命令：
                - 播放音乐 / 播放歌曲：开始播放音乐
                - 暂停音乐：暂停当前播放
                - 播放轻音乐 / 播放摇滚 等：播放指定类型的音乐

                使用示例：
                "播放轻音乐" —— 播放一些轻柔的音乐
                "暂停音乐" —— 暂停当前正在播放的音乐
                "播放古典音乐" —— 播放古典音乐
                """),

            new("weather-guide", """
                天气查询功能使用指南

                功能概述：
                天气查询功能支持查询国内主要城市的实时天气信息，包括温度、天气状况和穿衣建议。

                支持的城市：
                北京、上海、广州、深圳、成都、杭州、南京、武汉、重庆、西安、苏州、天津等。

                支持的时间：
                今天、明天、后天的天气查询。

                基本命令：
                - 查询天气 / 天气怎么样：查询天气信息
                - 是否下雨：判断当前或未来天气
                - 穿什么衣服：获取穿衣建议

                使用示例：
                "今天北京天气怎么样" —— 查询北京今日天气
                "明天上海会下雨吗" —— 查询上海明日是否下雨
                "成都今天穿什么" —— 获取成都今日穿衣建议
                """),

            new("security-guide", """
                安防系统使用指南

                功能概述：
                安防系统提供门锁控制、摄像头监控、外出模式和入侵警报等功能，全面保障家庭安全。

                主要功能：
                1. 门锁控制：远程锁门/解锁
                2. 摄像头监控：查看实时画面
                3. 外出模式：启用离家安防策略
                4. 模拟有人在家：在外出时模拟有人在家
                5. 入侵警报：异常情况自动告警

                基本命令：
                - 锁门 / 上锁：锁定门锁
                - 解锁 / 开锁：解锁门锁
                - 查看摄像头 / 监控：查看监控画面
                - 外出模式 / 离家模式：启动外出安防
                - 模拟有人 / 模拟在家：启动在家模拟

                使用示例：
                "锁门" —— 锁定所有门锁
                "开启外出模式" —— 启动离家安防策略
                "查看摄像头" —— 查看实时监控画面
                """),

            new("system-overview", """
                CKY.MAF 智能家居系统概述

                系统简介：
                CKY.MAF 智能家居系统是基于多Agent架构的智能家居控制平台。系统采用自然语言交互方式，用户可以通过语音或文字命令控制家中的各种设备。

                核心功能模块：
                1. 照明控制：支持各房间灯光的开关、亮度和颜色调节
                2. 气候控制：空调温度设定、模式切换
                3. 音乐播放：音乐的播放、暂停和切换
                4. 天气查询：主要城市天气信息查询
                5. 安防系统：门锁控制、摄像头监控、外出模式
                6. 温度历史：传感器数据查看和历史温度趋势
                7. 知识库：使用说明和常见问题查询

                Agent 架构：
                系统由多个专业Agent协同工作，每个Agent负责特定领域的任务：
                - LightingAgent：照明控制
                - ClimateAgent：气候控制
                - MusicAgent：音乐播放
                - WeatherAgent：天气查询
                - SecurityAgent：安防控制
                - TemperatureHistoryAgent：温度历史
                - KnowledgeBaseAgent：知识库查询

                技术特性：
                - 基于 Microsoft Agent Framework 构建
                - 支持 RAG（检索增强生成）知识库
                - Prometheus 监控和 OpenTelemetry 分布式追踪
                - SignalR 实时通信
                - 5级降级策略确保高可用
                """)
        ];

        private record SeedDocument(string Id, string Content);
    }
}

using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Agents
{
    /// <summary>
    /// 智能客服主控 Agent
    /// 负责意图识别、路由、多轮对话管理和结果聚合
    /// </summary>
    public class CustomerServiceLeaderAgent : MafBusinessAgentBase
    {
        private readonly IIntentRecognizer _intentRecognizer;
        private readonly IEntityExtractor _entityExtractor;
        private readonly KnowledgeBaseAgent _knowledgeBaseAgent;
        private readonly OrderAgent _orderAgent;
        private readonly TicketAgent _ticketAgent;
        private readonly IUserBehaviorService _userBehaviorService;
        private readonly IDegradationManager _degradationManager;
        private readonly IRuleEngine _ruleEngine;
        private readonly IEscalationService? _escalationService;

        public override string AgentId => "cs:leader-agent:001";
        public override string Name => "CustomerServiceLeaderAgent";
        public override string Description => "智能客服主控 Agent，协调所有子 Agent 为用户提供服务";
        public override IReadOnlyList<string> Capabilities =>
        [
            "customer-service:coordination",
            "customer-service:routing",
            "customer-service:multi-turn",
        ];

        public CustomerServiceLeaderAgent(
            IIntentRecognizer intentRecognizer,
            IEntityExtractor entityExtractor,
            KnowledgeBaseAgent knowledgeBaseAgent,
            OrderAgent orderAgent,
            TicketAgent ticketAgent,
            IUserBehaviorService userBehaviorService,
            IDegradationManager degradationManager,
            IRuleEngine ruleEngine,
            IMafAiAgentRegistry llmRegistry,
            ILogger<CustomerServiceLeaderAgent> logger,
            IEscalationService? escalationService = null)
            : base(llmRegistry, logger)
        {
            _intentRecognizer = intentRecognizer
                ?? throw new ArgumentNullException(nameof(intentRecognizer));
            _entityExtractor = entityExtractor
                ?? throw new ArgumentNullException(nameof(entityExtractor));
            _knowledgeBaseAgent = knowledgeBaseAgent
                ?? throw new ArgumentNullException(nameof(knowledgeBaseAgent));
            _orderAgent = orderAgent
                ?? throw new ArgumentNullException(nameof(orderAgent));
            _ticketAgent = ticketAgent
                ?? throw new ArgumentNullException(nameof(ticketAgent));
            _userBehaviorService = userBehaviorService
                ?? throw new ArgumentNullException(nameof(userBehaviorService));
            _degradationManager = degradationManager
                ?? throw new ArgumentNullException(nameof(degradationManager));
            _ruleEngine = ruleEngine
                ?? throw new ArgumentNullException(nameof(ruleEngine));
            _escalationService = escalationService;
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            using var activity = MafActivitySource.Agent.StartActivity("cs.main_agent.execute");
            activity?.SetTag("agent.name", Name);
            activity?.SetTag("user_input.length", request.UserInput.Length);

            var startTime = DateTime.UtcNow;
            Logger.LogInformation("CustomerServiceLeaderAgent processing: {UserInput}", request.UserInput);

            try
            {
                // Level 5 降级：完全禁用 LLM，使用规则引擎处理
                if (!_degradationManager.IsFeatureEnabled("llm") && _ruleEngine.CanHandle(request.UserInput))
                {
                    Logger.LogWarning("LLM 已降级至 Level 5，使用规则引擎处理: {UserInput}", request.UserInput);
                    activity?.SetTag("routing.degradation", true);
                    activity?.SetTag("routing.degradation_level", _degradationManager.CurrentLevel.ToString());

                    var ruleResponse = await _ruleEngine.ProcessAsync(request, ct);
                    activity?.SetTag("response.success", ruleResponse.Success);
                    return ruleResponse;
                }

                // 1. 意图识别
                var intent = await _intentRecognizer.RecognizeAsync(request.UserInput, ct);
                activity?.SetTag("intent.primary", intent.PrimaryIntent);
                activity?.SetTag("intent.confidence", intent.Confidence);

                // 2. 实体提取
                var extractionResult = await _entityExtractor.ExtractAsync(request.UserInput, ct);
                foreach (var entity in extractionResult.ExtractedEntities)
                {
                    request.Parameters.TryAdd(entity.EntityType, (object)entity.EntityValue);
                }
                activity?.SetTag("entities.count", extractionResult.ExtractedEntities.Count);

                // 3. 根据意图路由到对应 Agent
                var response = await RouteToAgentAsync(intent, request, ct);

                // 4. 记录用户行为
                await RecordBehaviorAsync(request, intent, response, startTime, ct);

                activity?.SetTag("response.success", response.Success);
                return response;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                Logger.LogError(ex, "CustomerServiceLeaderAgent failed");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = "非常抱歉，系统暂时遇到问题，请稍后重试或联系人工客服。",
                    Error = ex.Message,
                };
            }
        }

        private async Task<MafTaskResponse> RouteToAgentAsync(
            IntentRecognitionResult intent,
            MafTaskRequest request,
            CancellationToken ct)
        {
            var userInput = request.UserInput;

            // 0. 情绪检测：识别用户负面情绪
            var emotionLevel = DetectEmotion(userInput);

            // CS-04: 投诉+订单场景 → OrderAgent + TicketAgent 协作 + 自动升级
            if (emotionLevel >= EmotionLevel.Angry
                && ContainsAny(userInput, ["投诉", "举报", "曝光", "太过分"])
                && ContainsAny(userInput, ["订单", "退款", "退货", "快递", "物流"]))
            {
                Logger.LogInformation("Complaint with order context detected, coordinating OrderAgent + TicketAgent");
                return await HandleComplaintCoordinationAsync(request, emotionLevel, ct);
            }

            // 订单相关意图
            if (intent.PrimaryIntent is "QueryOrder" or "CancelOrder" or "TrackShipping" or "RequestRefund"
                || ContainsAny(userInput, ["订单", "快递", "物流", "退款", "退货"]))
            {
                Logger.LogInformation("Routing to OrderAgent (intent: {Intent})", intent.PrimaryIntent);
                using var orderActivity = MafActivitySource.Agent.StartActivity("cs.agent.order");
                orderActivity?.SetTag("intent", intent.PrimaryIntent);
                var response = await _orderAgent.ExecuteBusinessLogicAsync(request, ct);
                orderActivity?.SetTag("success", response.Success);
                return ApplyEmotionResponse(response, emotionLevel);
            }

            // 工单相关意图
            if (intent.PrimaryIntent is "CreateTicket" or "QueryTicket"
                || ContainsAny(userInput, ["投诉", "工单", "反馈", "建议", "举报", "人工"]))
            {
                Logger.LogInformation("Routing to TicketAgent (intent: {Intent})", intent.PrimaryIntent);
                using var ticketActivity = MafActivitySource.Agent.StartActivity("cs.agent.ticket");
                ticketActivity?.SetTag("intent", intent.PrimaryIntent);
                ticketActivity?.SetTag("emotion_level", emotionLevel.ToString());
                // 情绪升级时自动提升工单优先级
                if (emotionLevel >= EmotionLevel.Angry)
                {
                    request.Parameters["emotionEscalation"] = "true";
                }
                var response = await _ticketAgent.ExecuteBusinessLogicAsync(request, ct);
                ticketActivity?.SetTag("success", response.Success);
                return ApplyEmotionResponse(response, emotionLevel);
            }

            // 默认：知识库查询
            Logger.LogInformation("Routing to KnowledgeBaseAgent (intent: {Intent})", intent.PrimaryIntent);
            using var kbActivity = MafActivitySource.Agent.StartActivity("cs.agent.knowledge_base");
            kbActivity?.SetTag("intent", intent.PrimaryIntent);
            var kbResponse = await _knowledgeBaseAgent.ExecuteBusinessLogicAsync(request, ct);
            kbActivity?.SetTag("success", kbResponse.Success);

            // 知识库无答案时，主动提示用户可以提交工单
            if (!kbResponse.Success && kbResponse.Data is IDictionary<string, object> data
                && data.TryGetValue("ShouldEscalate", out var escalate)
                && escalate is true)
            {
                kbActivity?.SetTag("escalation", true);
                kbResponse.Result += "\n\n💡 您也可以说「提交工单」，我们会安排专属客服为您解决。";
            }

            return ApplyEmotionResponse(kbResponse, emotionLevel);
        }

        /// <summary>
        /// CS-04: 投诉建议协作处理
        /// 当用户情绪激动且投诉涉及订单时，OrderAgent 和 TicketAgent 协作，同时自动升级
        /// </summary>
        private async Task<MafTaskResponse> HandleComplaintCoordinationAsync(
            MafTaskRequest request,
            EmotionLevel emotionLevel,
            CancellationToken ct)
        {
            using var activity = MafActivitySource.Agent.StartActivity("cs.complaint_coordination");
            activity?.SetTag("emotion_level", emotionLevel.ToString());

            var results = new List<string>();

            // 1. OrderAgent: 查询订单信息（获取上下文）
            try
            {
                var orderResponse = await _orderAgent.ExecuteBusinessLogicAsync(request, ct);
                if (orderResponse.Success)
                {
                    results.Add($"📦 订单信息：{orderResponse.Result}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "OrderAgent failed during complaint coordination");
            }

            // 2. TicketAgent: 自动创建紧急工单
            request.Parameters["emotionEscalation"] = "true";
            request.Parameters["priority"] = "urgent";
            try
            {
                var ticketResponse = await _ticketAgent.ExecuteBusinessLogicAsync(request, ct);
                results.Add($"📋 {ticketResponse.Result}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "TicketAgent failed during complaint coordination");
                results.Add("📋 工单创建失败，请稍后重试");
            }

            // 3. EscalationService: 自动升级到人工客服
            if (_escalationService != null)
            {
                try
                {
                    var escalation = await _escalationService.EscalateToHumanAsync(
                        request.UserId, "投诉升级：用户情绪激动", "urgent", ct);
                    if (escalation.Success)
                    {
                        results.Add($"👤 {escalation.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "EscalationService failed during complaint coordination");
                }
            }

            var combinedResult = string.Join("\n\n", results);
            var response = new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = combinedResult,
            };

            return ApplyEmotionResponse(response, emotionLevel);
        }

        /// <summary>
        /// 检测用户情绪等级
        /// </summary>
        public static EmotionLevel DetectEmotion(string input)
        {
            // 强烈负面情绪关键词
            if (ContainsAny(input, ["愤怒", "气死", "太过分", "垃圾", "骗子", "投诉你们", "要举报", "曝光"]))
                return EmotionLevel.Angry;

            // 中度负面情绪关键词
            if (ContainsAny(input, ["不满意", "很失望", "差劲", "太差", "受不了", "忍无可忍", "什么破", "坑人"]))
                return EmotionLevel.Frustrated;

            // 轻度负面情绪关键词
            if (ContainsAny(input, ["不太好", "有点慢", "不开心", "不高兴", "烦", "郁闷"]))
                return EmotionLevel.Upset;

            return EmotionLevel.Neutral;
        }

        /// <summary>
        /// 根据情绪等级在响应前添加安抚话语
        /// </summary>
        private static MafTaskResponse ApplyEmotionResponse(MafTaskResponse response, EmotionLevel level)
        {
            if (level == EmotionLevel.Neutral)
                return response;

            var empathy = level switch
            {
                EmotionLevel.Angry => "非常抱歉给您带来了如此糟糕的体验，我理解您的愤怒。我们会优先处理您的问题。\n\n",
                EmotionLevel.Frustrated => "很抱歉让您感到失望，我理解您的感受。我会尽力帮您解决问题。\n\n",
                EmotionLevel.Upset => "抱歉给您带来了不便，我来帮您看看。\n\n",
                _ => "",
            };

            response.Result = empathy + response.Result;
            return response;
        }

        /// <summary>用户情绪等级</summary>
        public enum EmotionLevel
        {
            Neutral = 0,
            Upset = 1,
            Frustrated = 2,
            Angry = 3,
        }

        private async Task RecordBehaviorAsync(
            MafTaskRequest request,
            IntentRecognitionResult intent,
            MafTaskResponse response,
            DateTime startTime,
            CancellationToken ct)
        {
            try
            {
                var entities = request.Parameters
                    .Where(p => p.Value is string)
                    .ToDictionary(p => p.Key, p => p.Value?.ToString() ?? string.Empty);

                await _userBehaviorService.RecordAsync(new UserBehaviorRecord
                {
                    UserId = request.UserId,
                    SessionId = request.ConversationId,
                    Intent = intent.PrimaryIntent,
                    TaskSucceeded = response.Success,
                    ClarificationRoundsNeeded = response.NeedsClarification ? 1 : 0,
                    ResponseTime = DateTime.UtcNow - startTime,
                    Entities = entities,
                }, ct);
            }
            catch (Exception ex)
            {
                // 行为记录失败不影响主流程
                Logger.LogWarning(ex, "Failed to record user behavior");
            }
        }

        private static bool ContainsAny(string input, string[] keywords)
        {
            return keywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
    }
}

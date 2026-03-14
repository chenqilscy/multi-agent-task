using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Agents
{
    /// <summary>
    /// 智能客服主控 Agent
    /// 负责意图识别、路由、多轮对话管理和结果聚合
    /// </summary>
    public class CustomerServiceMainAgent : MafBusinessAgentBase
    {
        private readonly IIntentRecognizer _intentRecognizer;
        private readonly IEntityExtractor _entityExtractor;
        private readonly KnowledgeBaseAgent _knowledgeBaseAgent;
        private readonly OrderAgent _orderAgent;
        private readonly TicketAgent _ticketAgent;
        private readonly IUserBehaviorService _userBehaviorService;

        public override string AgentId => "cs:main-agent:001";
        public override string Name => "CustomerServiceMainAgent";
        public override string Description => "智能客服主控 Agent，协调所有子 Agent 为用户提供服务";
        public override IReadOnlyList<string> Capabilities =>
        [
            "customer-service:coordination",
            "customer-service:routing",
            "customer-service:multi-turn",
        ];

        public CustomerServiceMainAgent(
            IIntentRecognizer intentRecognizer,
            IEntityExtractor entityExtractor,
            KnowledgeBaseAgent knowledgeBaseAgent,
            OrderAgent orderAgent,
            TicketAgent ticketAgent,
            IUserBehaviorService userBehaviorService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<CustomerServiceMainAgent> logger)
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
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            var startTime = DateTime.UtcNow;
            Logger.LogInformation("CustomerServiceMainAgent processing: {UserInput}", request.UserInput);

            try
            {
                // 1. 意图识别
                var intent = await _intentRecognizer.RecognizeAsync(request.UserInput, ct);

                // 2. 实体提取
                var extractionResult = await _entityExtractor.ExtractAsync(request.UserInput, ct);
                foreach (var entity in extractionResult.ExtractedEntities)
                {
                    request.Parameters.TryAdd(entity.EntityType, (object)entity.EntityValue);
                }

                // 3. 根据意图路由到对应 Agent
                var response = await RouteToAgentAsync(intent, request, ct);

                // 4. 记录用户行为
                await RecordBehaviorAsync(request, intent, response, startTime, ct);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CustomerServiceMainAgent failed");
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

            // 订单相关意图
            if (intent.PrimaryIntent is "QueryOrder" or "CancelOrder" or "TrackShipping" or "RequestRefund"
                || ContainsAny(userInput, ["订单", "快递", "物流", "退款", "退货"]))
            {
                Logger.LogInformation("Routing to OrderAgent (intent: {Intent})", intent.PrimaryIntent);
                return await _orderAgent.ExecuteBusinessLogicAsync(request, ct);
            }

            // 工单相关意图
            if (intent.PrimaryIntent is "CreateTicket" or "QueryTicket"
                || ContainsAny(userInput, ["投诉", "工单", "反馈", "建议", "举报", "人工"]))
            {
                Logger.LogInformation("Routing to TicketAgent (intent: {Intent})", intent.PrimaryIntent);
                return await _ticketAgent.ExecuteBusinessLogicAsync(request, ct);
            }

            // 默认：知识库查询
            Logger.LogInformation("Routing to KnowledgeBaseAgent (intent: {Intent})", intent.PrimaryIntent);
            var kbResponse = await _knowledgeBaseAgent.ExecuteBusinessLogicAsync(request, ct);

            // 知识库无答案时，主动提示用户可以提交工单
            if (!kbResponse.Success && kbResponse.Data is IDictionary<string, object> data
                && data.TryGetValue("ShouldEscalate", out var escalate)
                && escalate is true)
            {
                kbResponse.Result += "\n\n💡 您也可以说「提交工单」，我们会安排专属客服为您解决。";
            }

            return kbResponse;
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

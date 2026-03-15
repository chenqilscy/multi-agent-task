using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services
{
    /// <summary>
    /// 客服规则引擎（Level 5 降级时替代 LLM）
    /// 使用关键词匹配处理高频客服场景
    /// </summary>
    public class CustomerServiceRuleEngine : IRuleEngine
    {
        private static readonly string[] SupportedKeywords =
        [
            "订单", "物流", "快递", "退款", "退货", "取消",
            "投诉", "工单", "反馈",
            "营业时间", "地址", "电话", "联系方式",
            "退货政策", "售后", "保修",
        ];

        public bool CanHandle(string userInput)
        {
            return SupportedKeywords.Any(k =>
                userInput.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        public Task<MafTaskResponse> ProcessAsync(MafTaskRequest request, CancellationToken ct = default)
        {
            var input = request.UserInput;

            // 订单查询
            if (Contains(input, "查询订单", "查看订单", "查一下订单", "我的订单"))
            {
                return Ok(request, "📦 [规则引擎] 请提供您的订单号（格式如 ORD-2024-001），我将为您查询。\n当前LLM服务暂时不可用，已使用本地规则处理。");
            }

            // 物流查询
            if (Contains(input, "物流", "快递", "到哪了", "什么时候到"))
            {
                return Ok(request, "🚚 [规则引擎] 请提供订单号，我将为您查询物流状态。");
            }

            // FAQ：退货政策/售后/保修（放在退款前，避免"退货政策"被"退款"误匹配）
            if (Contains(input, "退货政策", "售后", "保修"))
            {
                return Ok(request,
                    "📋 [规则引擎] 退货政策：\n" +
                    "• 7天无理由退货（未拆封）\n" +
                    "• 15天质量问题换货\n" +
                    "• 1年保修服务");
            }

            // 退款/退货
            if (Contains(input, "退款", "退钱"))
            {
                return Ok(request,
                    "💰 [规则引擎] 退款/退货流程：\n" +
                    "1. 提供订单号\n" +
                    "2. 选择退款原因\n" +
                    "3. 审核通过后3-5个工作日退款到账\n\n" +
                    "请提供您的订单号开始办理。");
            }

            // 取消订单
            if (Contains(input, "取消订单", "不要了"))
            {
                return Ok(request, "❌ [规则引擎] 请提供您要取消的订单号，未发货的订单可直接取消。");
            }

            // 投诉/工单
            if (Contains(input, "投诉", "反馈", "建议", "工单"))
            {
                return Ok(request,
                    "📝 [规则引擎] 已记录您的反馈。\n" +
                    "当前LLM服务暂不可用，工单将在服务恢复后由专属客服跟进。\n" +
                    "如需紧急处理，请拨打客服热线：400-XXX-XXXX");
            }

            // FAQ：营业时间
            if (Contains(input, "营业时间", "几点开门", "几点关门"))
            {
                return Ok(request, "🕐 [规则引擎] 营业时间：周一至周日 9:00-21:00");
            }

            // FAQ：联系方式
            if (Contains(input, "地址", "电话", "联系方式"))
            {
                return Ok(request, "📞 [规则引擎] 客服热线：400-XXX-XXXX\n📧 邮箱：support@example.com");
            }

            // 兜底
            return Task.FromResult(new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Result = "⚠️ [规则引擎] 当前LLM服务不可用，仅支持订单查询、物流追踪、退款等常见操作。\n如需人工帮助，请拨打客服热线：400-XXX-XXXX",
            });
        }

        private static bool Contains(string input, params string[] keywords)
        {
            return keywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static Task<MafTaskResponse> Ok(MafTaskRequest request, string result)
        {
            return Task.FromResult(new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = result,
            });
        }
    }
}

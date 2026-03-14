using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.CustomerService
{
    /// <summary>
    /// 智能客服意图关键词提供者
    /// </summary>
    public class CustomerServiceIntentKeywordProvider : IIntentKeywordProvider
    {
        private readonly Dictionary<string, string[]> _intentKeywordMap;

        public CustomerServiceIntentKeywordProvider()
        {
            _intentKeywordMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["QueryOrder"] = ["查询订单", "查一下订单", "订单状态", "我的订单", "查单", "ORD-"],
                ["CancelOrder"] = ["取消订单", "取消", "不要了", "撤单", "退单"],
                ["TrackShipping"] = ["快递", "物流", "到哪了", "发货了吗", "派送", "追踪"],
                ["RequestRefund"] = ["退款", "退货", "退钱", "申请退款", "退一下"],
                ["CreateTicket"] = ["投诉", "反馈", "提交工单", "创建工单", "报问题", "建议"],
                ["QueryTicket"] = ["工单状态", "处理进度", "我的工单", "查询工单"],
                ["ProductQuery"] = ["产品", "商品", "规格", "参数", "功能", "使用方法", "说明书"],
                ["PaymentQuery"] = ["付款", "支付", "账单", "发票", "优惠券", "积分"],
                ["GeneralFaq"] = ["怎么", "如何", "是什么", "帮我", "我想"],
            };
        }

        public string?[]? GetKeywords(string intent)
        {
            _intentKeywordMap.TryGetValue(intent, out var keywords);
            return keywords;
        }

        public IEnumerable<string> GetSupportedIntents()
        {
            return _intentKeywordMap.Keys;
        }
    }
}

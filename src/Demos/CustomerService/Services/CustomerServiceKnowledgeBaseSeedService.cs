using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services;

/// <summary>
/// 客服知识库RAG种子数据服务
/// 在应用启动时将预置FAQ摄入RAG管线实现语义检索
/// </summary>
public class CustomerServiceKnowledgeBaseSeedService : IHostedService
{
    private readonly IRagPipeline _ragPipeline;
    private readonly ILogger<CustomerServiceKnowledgeBaseSeedService> _logger;

    public CustomerServiceKnowledgeBaseSeedService(
        IRagPipeline ragPipeline,
        ILogger<CustomerServiceKnowledgeBaseSeedService> logger)
    {
        _ragPipeline = ragPipeline;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("开始初始化客服知识库RAG...");

        try
        {
            foreach (var doc in GetSeedDocuments())
            {
                await _ragPipeline.IngestAsync(
                    doc.Id,
                    doc.Content,
                    RagEnhancedKnowledgeBaseService.CollectionName,
                    new ChunkingConfig { MaxChunkSize = 400, OverlapRatio = 0.15 },
                    ct);
            }

            _logger.LogInformation("客服知识库RAG初始化完成，已摄入 {Count} 篇文档", GetSeedDocuments().Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "客服知识库RAG初始化失败，RAG语义检索可能不可用");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static List<SeedDocument> GetSeedDocuments() =>
    [
        new("cs-faq-order-query", """
            如何查询我的订单？

            您可以通过以下方式查询订单：
            1. 直接说"查询订单"，系统会为您查找最近的订单
            2. 提供订单号进行精确查询，例如"查询订单 ORD-001"
            3. 查看所有历史订单，说"我的所有订单"

            订单状态说明：
            - 待处理（Pending）：订单已提交，等待确认
            - 已确认（Confirmed）：订单已确认，准备发货
            - 已发货（Shipped）：商品已发出，可查询物流
            - 已送达（Delivered）：商品已送达
            - 已取消（Cancelled）：订单已取消
            """),

        new("cs-faq-refund", """
            如何申请退款？

            退款申请流程：
            1. 说"申请退款"并提供订单号
            2. 选择退款原因（商品质量、不满意、错发、其他）
            3. 系统将自动提交退款申请
            4. 退款审核时间：1-3个工作日
            5. 退款到账时间：审核通过后3-15个工作日

            退款条件：
            - 订单状态为"已确认"或"已发货"的可申请退款
            - "已送达"的订单需在7天内申请
            - "已取消"的订单会自动退款

            注意事项：
            - 退款金额将原路返回至您的支付账户
            - 部分商品可能需要先退货再退款
            - 如有疑问可提交工单由专属客服处理
            """),

        new("cs-faq-cancel-order", """
            如何取消订单？

            取消订单步骤：
            1. 说"取消订单"并提供订单号
            2. 系统会检查订单状态
            3. 仅"待处理"状态的订单可以直接取消
            4. "已确认"的订单取消需审核

            取消限制：
            - 已发货的订单无法取消，请申请退货退款
            - 已送达的订单无法取消
            - 已取消的订单不能重复取消

            取消后的退款：
            - 取消成功后，已支付的金额会自动发起退款
            - 退款到账时间为3-15个工作日
            """),

        new("cs-faq-shipping", """
            快递物流查询

            查询物流信息：
            1. 说"查询物流"并提供订单号
            2. 系统会显示详细的物流轨迹
            3. 包括每个节点的时间和状态

            物流时效说明：
            - 标准配送：3-5个工作日
            - 加急配送：1-2个工作日
            - 偏远地区可能延长1-3天

            物流异常处理：
            - 如果超过预计到达时间仍未收到，请联系客服
            - 说"提交工单"可以创建物流问题工单
            - 物流延迟系统会主动通知您
            """),

        new("cs-faq-quality", """
            产品质量问题处理

            质量问题处理流程：
            1. 拍照或描述质量问题
            2. 说"产品质量问题"提交反馈
            3. 系统会自动创建高优先级工单
            4. 专属客服将在24小时内联系您

            常见质量问题：
            - 商品破损或变形
            - 功能异常或无法使用
            - 与描述不符
            - 缺少配件

            处理方案：
            - 换货：免费更换同款商品
            - 退货退款：全额退款并承担退货运费
            - 补发配件：缺少配件免费补发
            - 维修：在保修期内免费维修
            """),

        new("cs-faq-ticket", """
            工单系统使用指南

            创建工单：
            1. 说"提交工单"或"我要投诉"
            2. 描述您遇到的问题
            3. 系统会自动分配优先级和分类

            工单优先级：
            - 低（Low）：一般咨询
            - 普通（Normal）：常规问题
            - 高（High）：影响使用的问题
            - 紧急（Urgent）：重大问题、涉及资金安全

            工单处理时效：
            - 紧急：2小时内响应
            - 高优先级：4小时内响应
            - 普通：24小时内响应
            - 低优先级：48小时内响应

            查询工单状态：
            - 说"查询工单"可以查看您的工单处理进度
            """),

        new("cs-faq-membership", """
            会员服务说明

            会员等级：
            - 普通会员：注册即可获得
            - 银卡会员：年消费满2000元
            - 金卡会员：年消费满5000元
            - 钻石会员：年消费满10000元

            会员权益：
            - 普通会员：积分累计、生日祝福
            - 银卡会员：95折优惠、免费包邮
            - 金卡会员：9折优惠、专属客服、优先发货
            - 钻石会员：85折优惠、专属客服、优先发货、免费上门取件

            会员到期提醒：
            - 系统会在会员到期前30天发送提醒
            - 可以说"我的会员"查看会员状态
            """)
    ];

    private record SeedDocument(string Id, string Content);
}

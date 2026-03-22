using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.SeedData;

/// <summary>
/// 客服系统种子数据初始化
/// 为开发环境提供开箱可用的初始数据
/// </summary>
public static class CustomerServiceSeedData
{
    /// <summary>
    /// 初始化种子数据（仅在数据库为空时执行）
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomerServiceDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomerServiceDbContext>>();

        // 确保数据库已创建
        await dbContext.Database.EnsureCreatedAsync();

        // 如果已有数据则跳过
        if (await dbContext.Customers.AnyAsync())
        {
            logger.LogInformation("客服系统种子数据已存在，跳过初始化");
            return;
        }

        logger.LogInformation("开始初始化客服系统种子数据...");

        // === 客户数据 ===
        var customers = CreateCustomers();
        dbContext.Customers.AddRange(customers);
        await dbContext.SaveChangesAsync();

        // === 订单数据 ===
        var orders = CreateOrders(customers);
        dbContext.Orders.AddRange(orders);
        await dbContext.SaveChangesAsync();

        // === 物流追踪数据 ===
        var trackingEvents = CreateTrackingEvents(orders);
        dbContext.TrackingEvents.AddRange(trackingEvents);

        // === 工单数据 ===
        var tickets = CreateTickets(customers);
        dbContext.Tickets.AddRange(tickets);
        await dbContext.SaveChangesAsync();

        // === FAQ 知识库数据 ===
        var faqs = CreateFaqEntries();
        dbContext.FaqEntries.AddRange(faqs);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "客服系统种子数据初始化完成：{CustomerCount} 客户, {OrderCount} 订单, {TicketCount} 工单, {FaqCount} FAQ",
            customers.Count, orders.Count, tickets.Count, faqs.Count);
    }

    private static List<CustomerEntity> CreateCustomers() =>
    [
        new()
        {
            CustomerId = "CUST-001",
            Name = "张三",
            Email = "zhangsan@example.com",
            Phone = "13800138001",
            PreferredLanguage = "zh-CN",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            LastActiveAt = DateTime.UtcNow.AddDays(-1)
        },
        new()
        {
            CustomerId = "CUST-002",
            Name = "李四",
            Email = "lisi@example.com",
            Phone = "13900139002",
            PreferredLanguage = "zh-CN",
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            LastActiveAt = DateTime.UtcNow.AddDays(-5)
        },
        new()
        {
            CustomerId = "CUST-003",
            Name = "王五",
            Email = "wangwu@example.com",
            Phone = "13700137003",
            PreferredLanguage = "zh-CN",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            LastActiveAt = DateTime.UtcNow
        }
    ];

    private static List<OrderEntity> CreateOrders(List<CustomerEntity> customers)
    {
        var customer1 = customers[0]; // 张三
        var customer2 = customers[1]; // 李四
        var customer3 = customers[2]; // 王五

        return
        [
            // 张三的订单
            new()
            {
                OrderId = "ORD-2024-001",
                Customer = customer1,
                Status = "delivered",
                TotalAmount = 299.00m,
                TrackingNumber = "SF1234567890",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                Items =
                [
                    new() { ProductId = "PROD-001", ProductName = "无线蓝牙耳机", Quantity = 1, UnitPrice = 199.00m },
                    new() { ProductId = "PROD-002", ProductName = "耳机保护套", Quantity = 1, UnitPrice = 49.00m },
                    new() { ProductId = "PROD-003", ProductName = "USB-C充电线", Quantity = 1, UnitPrice = 51.00m }
                ]
            },
            new()
            {
                OrderId = "ORD-2024-002",
                Customer = customer1,
                Status = "shipped",
                TotalAmount = 1599.00m,
                TrackingNumber = "YT9876543210",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Items =
                [
                    new() { ProductId = "PROD-010", ProductName = "机械键盘 Cherry轴", Quantity = 1, UnitPrice = 1599.00m }
                ]
            },
            new()
            {
                OrderId = "ORD-2024-003",
                Customer = customer1,
                Status = "pending",
                TotalAmount = 89.00m,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Items =
                [
                    new() { ProductId = "PROD-020", ProductName = "鼠标垫 超大号", Quantity = 1, UnitPrice = 59.00m },
                    new() { ProductId = "PROD-021", ProductName = "屏幕清洁套装", Quantity = 1, UnitPrice = 30.00m }
                ]
            },

            // 李四的订单
            new()
            {
                OrderId = "ORD-2024-004",
                Customer = customer2,
                Status = "paid",
                TotalAmount = 4999.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items =
                [
                    new() { ProductId = "PROD-050", ProductName = "4K显示器 27英寸", Quantity = 1, UnitPrice = 4999.00m }
                ]
            },
            new()
            {
                OrderId = "ORD-2024-005",
                Customer = customer2,
                Status = "cancelled",
                TotalAmount = 399.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-9),
                Items =
                [
                    new() { ProductId = "PROD-030", ProductName = "蓝牙音箱", Quantity = 1, UnitPrice = 399.00m }
                ]
            },

            // 王五的订单
            new()
            {
                OrderId = "ORD-2024-006",
                Customer = customer3,
                Status = "shipped",
                TotalAmount = 2399.00m,
                TrackingNumber = "ZT1122334455",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Items =
                [
                    new() { ProductId = "PROD-060", ProductName = "降噪耳机 旗舰版", Quantity = 1, UnitPrice = 2399.00m }
                ]
            },
            new()
            {
                OrderId = "ORD-2024-007",
                Customer = customer3,
                Status = "delivered",
                TotalAmount = 168.00m,
                TrackingNumber = "JD5566778899",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-17),
                Items =
                [
                    new() { ProductId = "PROD-070", ProductName = "手机支架", Quantity = 2, UnitPrice = 39.00m },
                    new() { ProductId = "PROD-071", ProductName = "数据线三合一", Quantity = 3, UnitPrice = 30.00m }
                ]
            },
            new()
            {
                OrderId = "ORD-2024-008",
                Customer = customer3,
                Status = "paid",
                TotalAmount = 799.00m,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                Items =
                [
                    new() { ProductId = "PROD-080", ProductName = "智能手环", Quantity = 1, UnitPrice = 799.00m }
                ]
            }
        ];
    }

    private static List<TrackingEventEntity> CreateTrackingEvents(List<OrderEntity> orders)
    {
        var events = new List<TrackingEventEntity>();

        // ORD-2024-001 已签收
        var order1 = orders.First(o => o.OrderId == "ORD-2024-001");
        events.AddRange(
        [
            new() { Order = order1, TrackingNumber = "SF1234567890", Location = "深圳集散中心", Description = "快件已发出", Timestamp = DateTime.UtcNow.AddDays(-14) },
            new() { Order = order1, TrackingNumber = "SF1234567890", Location = "武汉转运中心", Description = "快件到达", Timestamp = DateTime.UtcNow.AddDays(-13) },
            new() { Order = order1, TrackingNumber = "SF1234567890", Location = "北京朝阳区网点", Description = "快件派送中", Timestamp = DateTime.UtcNow.AddDays(-11) },
            new() { Order = order1, TrackingNumber = "SF1234567890", Location = "北京朝阳区", Description = "已签收 本人签收", Timestamp = DateTime.UtcNow.AddDays(-10) }
        ]);

        // ORD-2024-002 运输中
        var order2 = orders.First(o => o.OrderId == "ORD-2024-002");
        events.AddRange(
        [
            new() { Order = order2, TrackingNumber = "YT9876543210", Location = "广州仓库", Description = "商品已出库", Timestamp = DateTime.UtcNow.AddDays(-2) },
            new() { Order = order2, TrackingNumber = "YT9876543210", Location = "长沙转运中心", Description = "快件到达", Timestamp = DateTime.UtcNow.AddDays(-1) },
            new() { Order = order2, TrackingNumber = "YT9876543210", Location = "武汉分拨中心", Description = "快件到达 预计明天送达", Timestamp = DateTime.UtcNow.AddHours(-6) }
        ]);

        // ORD-2024-006 运输中
        var order6 = orders.First(o => o.OrderId == "ORD-2024-006");
        events.AddRange(
        [
            new() { Order = order6, TrackingNumber = "ZT1122334455", Location = "上海仓库", Description = "快件已揽收", Timestamp = DateTime.UtcNow.AddDays(-4) },
            new() { Order = order6, TrackingNumber = "ZT1122334455", Location = "杭州中转站", Description = "快件到达", Timestamp = DateTime.UtcNow.AddDays(-3) }
        ]);

        return events;
    }

    private static List<TicketEntity> CreateTickets(List<CustomerEntity> customers)
    {
        var customer1 = customers[0]; // 张三
        var customer2 = customers[1]; // 李四
        var customer3 = customers[2]; // 王五

        return
        [
            new()
            {
                TicketId = "TKT-20260315-001",
                Customer = customer1,
                Title = "耳机右侧没有声音",
                Description = "购买的无线蓝牙耳机使用三天后右侧没有声音了，怀疑是质量问题。订单号 ORD-2024-001",
                Category = "product",
                Priority = "high",
                Status = "in_progress",
                RelatedOrderId = "ORD-2024-001",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Comments =
                [
                    new() { Author = "张三", Content = "耳机右侧完全没声音，重新配对也不行", IsStaff = false, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new() { Author = "客服小王", Content = "您好，已收到您的反馈。请尝试重置耳机：长按两侧触控区域10秒。如仍无法解决，我们将安排换货。", IsStaff = true, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                    new() { Author = "张三", Content = "试了还是不行", IsStaff = false, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                    new() { Author = "客服小王", Content = "已为您创建换货申请，新耳机将在2个工作日内发出。旧耳机由快递上门取件。", IsStaff = true, CreatedAt = DateTime.UtcNow.AddDays(-3) }
                ]
            },
            new()
            {
                TicketId = "TKT-20260316-001",
                Customer = customer2,
                Title = "取消订单后退款未到账",
                Description = "3月6日取消的订单 ORD-2024-005，退款金额399元一直没有到账",
                Category = "payment",
                Priority = "normal",
                Status = "resolved",
                RelatedOrderId = "ORD-2024-005",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                ResolvedAt = DateTime.UtcNow.AddDays(-1),
                Comments =
                [
                    new() { Author = "李四", Content = "取消订单已经3天了，退款还没到账", IsStaff = false, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new() { Author = "客服小李", Content = "已查询，退款已于3月7日提交至支付系统，通常3-5个工作日到账。您使用的是信用卡支付，请留意银行账单。", IsStaff = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new() { Author = "李四", Content = "收到了，谢谢", IsStaff = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
                ]
            },
            new()
            {
                TicketId = "TKT-20260317-001",
                Customer = customer2,
                Title = "显示器配送地址修改",
                Description = "刚下单的4K显示器（ORD-2024-004），需要修改配送地址",
                Category = "shipping",
                Priority = "normal",
                Status = "open",
                RelatedOrderId = "ORD-2024-004",
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                Comments =
                [
                    new() { Author = "李四", Content = "请把配送地址改为：北京市海淀区中关村大街1号", IsStaff = false, CreatedAt = DateTime.UtcNow.AddHours(-12) }
                ]
            },
            new()
            {
                TicketId = "TKT-20260318-001",
                Customer = customer3,
                Title = "降噪耳机物流一直不更新",
                Description = "订单 ORD-2024-006 的物流信息两天没有更新了，请帮忙查一下",
                Category = "shipping",
                Priority = "normal",
                Status = "open",
                RelatedOrderId = "ORD-2024-006",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Comments =
                [
                    new() { Author = "王五", Content = "物流卡在杭州中转站两天了", IsStaff = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
                ]
            },
            new()
            {
                TicketId = "TKT-20260319-001",
                Customer = customer1,
                Title = "咨询积分兑换规则",
                Description = "想了解一下积分商城的兑换规则和可兑换的商品",
                Category = "other",
                Priority = "low",
                Status = "open",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        ];
    }

    private static List<FaqEntryEntity> CreateFaqEntries() =>
    [
        // 订单相关
        new()
        {
            Question = "如何查询我的订单？",
            Answer = "您可以通过以下方式查询订单：1) 在App/网站「我的订单」页面查看；2) 直接告诉我您的订单号（如ORD-2024-001）或手机号。",
            Category = "order",
            KeywordsJson = JsonSerializer.Serialize(new[] { "查询", "订单", "查看", "在哪里看" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new()
        {
            Question = "如何取消订单？",
            Answer = "未发货的订单可以直接取消：1) 在「我的订单」找到对应订单点击「取消」；2) 联系客服提供订单号。已发货的订单需要等签收后走退货退款流程。",
            Category = "order",
            KeywordsJson = JsonSerializer.Serialize(new[] { "取消", "订单", "取消订单", "不想要了" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new()
        {
            Question = "如何修改订单地址？",
            Answer = "未发货的订单可以修改收货地址：联系客服提供订单号和新地址即可。已发货的订单无法修改地址，建议拒签后重新下单。",
            Category = "order",
            KeywordsJson = JsonSerializer.Serialize(new[] { "修改", "地址", "收货地址", "改地址" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        },

        // 退款退货相关
        new()
        {
            Question = "如何申请退款？",
            Answer = "退款流程：1) 在「我的订单」中选择需要退款的订单；2) 点击「申请退款」；3) 选择退款原因并提交。未发货订单通常1-3个工作日退款；已发货订单需先退货再退款。",
            Category = "refund",
            KeywordsJson = JsonSerializer.Serialize(new[] { "退款", "退货", "退钱", "申请退款", "不想要" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new()
        {
            Question = "退款多久能到账？",
            Answer = "退款到账时间取决于支付方式：1) 支付宝/微信：1-3个工作日；2) 信用卡：3-7个工作日；3) 银行卡：3-5个工作日。具体以银行处理时间为准。",
            Category = "refund",
            KeywordsJson = JsonSerializer.Serialize(new[] { "退款", "到账", "多久", "几天", "什么时候退" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        },
        new()
        {
            Question = "退货运费谁承担？",
            Answer = "如果是商品质量问题导致的退货，运费由我们承担（系统自动生成退货单）。如果是个人原因退货（如不喜欢、尺寸不合适），运费由您自行承担。",
            Category = "refund",
            KeywordsJson = JsonSerializer.Serialize(new[] { "退货", "运费", "谁出", "承担", "包邮" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        },

        // 物流相关
        new()
        {
            Question = "如何查询物流信息？",
            Answer = "您可以通过以下方式查询物流：1) 在「我的订单」中点击对应订单查看物流轨迹；2) 提供订单号或快递单号给客服查询；3) 前往快递公司官网/App用快递单号查询。",
            Category = "shipping",
            KeywordsJson = JsonSerializer.Serialize(new[] { "物流", "快递", "查询", "到哪了", "什么时候到" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new()
        {
            Question = "物流信息长时间不更新怎么办？",
            Answer = "物流信息超过48小时未更新，可能原因：1) 快件在中转途中，部分路段无扫描点；2) 快递公司系统延迟。建议先等待1-2天，如仍无更新请联系客服，我们会帮您联系快递公司核实。",
            Category = "shipping",
            KeywordsJson = JsonSerializer.Serialize(new[] { "物流", "不更新", "停了", "卡住", "没动" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        },
        new()
        {
            Question = "可以指定快递公司吗？",
            Answer = "目前我们的合作快递为顺丰、圆通、中通、京东快递。如果您有特殊需求，下单时可在备注中注明偏好的快递公司，我们会尽量满足。",
            Category = "shipping",
            KeywordsJson = JsonSerializer.Serialize(new[] { "快递公司", "指定", "顺丰", "想用", "能选" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        },

        // 支付相关
        new()
        {
            Question = "支持哪些支付方式？",
            Answer = "我们支持以下支付方式：1) 支付宝；2) 微信支付；3) 银联卡（信用卡/借记卡）；4) 花呗分期（部分商品支持）；5) 货到付款（部分区域支持）。",
            Category = "payment",
            KeywordsJson = JsonSerializer.Serialize(new[] { "支付", "付款", "方式", "怎么付", "能用什么" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new()
        {
            Question = "如何开具发票？",
            Answer = "订单完成后可以申请电子发票：1) 在「我的订单」找到需要开票的订单；2) 点击「申请发票」；3) 填写发票抬头和税号。电子发票通常在1个工作日内发送到您的邮箱。",
            Category = "payment",
            KeywordsJson = JsonSerializer.Serialize(new[] { "发票", "开票", "电子发票", "报销", "抬头" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        },

        // 账户相关
        new()
        {
            Question = "如何修改绑定手机号？",
            Answer = "修改手机号流程：1) 进入「账户设置」→「安全设置」；2) 点击「更换手机号」；3) 通过旧手机验证后设置新手机号。如果旧手机号已无法使用，请联系客服进行人工验证。",
            Category = "account",
            KeywordsJson = JsonSerializer.Serialize(new[] { "手机号", "修改", "换", "绑定", "更换" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new()
        {
            Question = "忘记密码怎么办？",
            Answer = "找回密码：1) 在登录页点击「忘记密码」；2) 输入绑定的手机号获取验证码；3) 验证后设置新密码。也可以直接使用手机号+验证码方式登录。",
            Category = "account",
            KeywordsJson = JsonSerializer.Serialize(new[] { "密码", "忘记", "找回", "登录不了", "重置" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },

        // 售后相关
        new()
        {
            Question = "商品保修政策是什么？",
            Answer = "我们的保修政策：1) 电子产品：自签收日起享受1年保修（人为损坏除外）；2) 7天无理由退货（未使用、不影响二次销售）；3) 15天质量问题可换货。具体保修规则以商品详情页说明为准。",
            Category = "product",
            KeywordsJson = JsonSerializer.Serialize(new[] { "保修", "质保", "售后", "坏了", "保修期" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        },
        new()
        {
            Question = "收到商品有破损怎么办？",
            Answer = "如果收到的商品有破损：1) 请先拍照留证（商品、包装、快递面单）；2) 联系客服提供订单号和照片；3) 我们会在24小时内为您安排补发或退款。请勿丢弃原始包装。",
            Category = "product",
            KeywordsJson = JsonSerializer.Serialize(new[] { "破损", "损坏", "碎了", "裂了", "收到坏的" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        },

        // 会员/积分
        new()
        {
            Question = "积分如何获取和使用？",
            Answer = "积分规则：1) 每消费1元获得1积分；2) 每日签到可获得5积分；3) 评价晒单获得10积分。使用：100积分=1元，可在下单时抵扣部分金额（最多抵扣订单总额的10%）。",
            Category = "membership",
            KeywordsJson = JsonSerializer.Serialize(new[] { "积分", "兑换", "获取", "怎么用", "规则", "签到" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        },
        new()
        {
            Question = "会员等级有什么权益？",
            Answer = "会员等级权益：1) 银卡会员：免费包邮、生日礼券；2) 金卡会员：专属客服、优先发货、双倍积分；3) 钻石会员：年度礼包、新品试用、免费退货包运费。消费每满1000元升一级。",
            Category = "membership",
            KeywordsJson = JsonSerializer.Serialize(new[] { "会员", "等级", "权益", "VIP", "升级" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        },

        // 优惠/促销
        new()
        {
            Question = "优惠券在哪里领取？",
            Answer = "领取优惠券：1) 首页「领券中心」每日更新；2) 「我的」→「优惠券」查看已有券；3) 关注店铺可领新客专属券；4) 特定活动页面发放限时优惠券。优惠券使用时请注意有效期和使用条件。",
            Category = "promotion",
            KeywordsJson = JsonSerializer.Serialize(new[] { "优惠券", "领取", "折扣", "便宜", "满减", "促销" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        },

        // 技术支持
        new()
        {
            Question = "App无法正常使用怎么办？",
            Answer = "请尝试以下步骤：1) 检查网络连接是否正常；2) 清除App缓存并重启；3) 检查是否为最新版本，如不是请更新；4) 卸载后重新安装。如问题仍未解决，请联系客服并提供手机型号和系统版本。",
            Category = "tech",
            KeywordsJson = JsonSerializer.Serialize(new[] { "App", "打不开", "闪退", "卡", "用不了", "bug" }),
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        }
    ];
}

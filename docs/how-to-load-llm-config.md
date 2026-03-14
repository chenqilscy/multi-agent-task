# LLM 服务配置加载指南

本文档说明如何加载和配置 LLM 服务（如 ZhipuAIConfig）。

## 配置方式概述

推荐使用 `IConfiguration` 模式从配置文件加载配置信息，然后在依赖注入时注册到容器中。

## 方法 1：从 appsettings.json 加载（推荐）

### 1.1 定义配置文件

**appsettings.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "LLM": {
    "Providers": {
      "ZhipuAI": {
        "ApiKey": "your-zhipuai-api-key-here",
        "Model": "glm-4",
        "Temperature": 0.7,
        "MaxTokens": 2000,
        "BaseUrl": "https://open.bigmodel.cn/api/"
      },
      "TongyiQianwen": {
        "ApiKey": "your-tongyi-api-key",
        "Model": "qwen-max"
      }
    }
  }
}
```

**appsettings.Development.json**
```json
{
  "LLM": {
    "Providers": {
      "ZhipuAI": {
        "ApiKey": "dev-api-key",
        "Model": "glm-4-air",
        "Temperature": 0.5
      }
    }
  }
}
```

**appsettings.Production.json**
```json
{
  "LLM": {
    "Providers": {
      "ZhipuAI": {
        "ApiKey": "${ZHIPUAI_API_KEY}",  // 从环境变量读取
        "Model": "glm-4-plus",
        "Temperature": 0.7
      }
    }
  }
}
```

### 1.2 创建配置选项类

```csharp
// src/Core/Configuration/LlmOptions.cs
namespace CKY.MultiAgentFramework.Core.Configuration
{
    public class LlmOptions
    {
        public const string SectionName = "LLM:Providers";

        public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
    }

    public class ProviderConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
        public string? BaseUrl { get; set; }
    }
}
```

### 1.3 修改 ZhipuAIConfig 使用配置绑定

```csharp
// src/Repository/LLM/ZhipuAIMafAiAgent.cs
namespace CKY.MultiAgentFramework.Repository.LLM
{
    public class ZhipuAIMafAiAgent : MafAgentBase, ILlmService
    {
        private readonly ZhipuAIConfig _config;

        public ZhipuAIMafAiAgent(
            ZhipuAIConfig config,
            IMafSessionStorage sessionStorage,
            IPriorityCalculator priorityCalculator,
            IMetricsCollector metricsCollector,
            ILogger<ZhipuAIMafAiAgent> logger)
            : base(sessionStorage, priorityCalculator, metricsCollector, logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ... 其他代码保持不变
    }

    // 配置类（简化为数据容器）
    public class ZhipuAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "glm-4";
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
    }
}
```

### 1.4 在 Program.cs 中配置

```csharp
// src/Demos/SmartHome/Program.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 添加配置
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
                          optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(); // 支持环境变量覆盖

        // 绑定配置选项
        builder.Services.Configure<LlmOptions>(
            builder.Configuration.GetSection(LlmOptions.SectionName));

        // 注册 LLM 服务
        RegisterLlmServices(builder.Services, builder.Configuration);

        // 其他服务注册...
        builder.Services.AddRazorComponents();
        builder.Services.AddServerSideBlazor();

        var app = builder.Build();

        // ...
        app.Run();
    }

    private static void RegisterLlmServices(IServiceCollection services, IConfiguration configuration)
    {
        // 方式1：直接从配置文件读取并注册
        var zhipuAISection = configuration.GetSection("LLM:Providers:ZhipuAI");
        var zhipuAIConfig = new ZhipuAIConfig
        {
            ApiKey = zhipuAISection["ApiKey"] ?? throw new InvalidOperationException("ZhipuAI:ApiKey is required"),
            Model = zhipuAISection["Model"] ?? "glm-4",
            Temperature = double.Parse(zhipuAISection["Temperature"] ?? "0.7"),
            MaxTokens = int.Parse(zhipuAISection["MaxTokens"] ?? "2000")
        };

        // 注册 ZhipuAI Agent（同时作为 ILlmService）
        services.AddSingleton<ZhipuAIMafAiAgent>();
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<ZhipuAIMafAiAgent>());
        services.AddSingleton(zhipuAIConfig);
    }
}
```

## 方法 2：使用 IOptions 模式（更推荐）

### 2.1 创建强类型配置类

```csharp
// src/Core/Configuration/ZhipuAIOptions.cs
namespace CKY.MultiAgentFramework.Core.Configuration
{
    public class ZhipuAIOptions
    {
        public const string SectionName = "LLM:Providers:ZhipuAI";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "glm-4";
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
        public string BaseUrl { get; set; } = "https://open.bigmodel.cn/api/";

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new InvalidOperationException("ZhipuAI:ApiKey is required");
            if (string.IsNullOrWhiteSpace(Model))
                throw new InvalidOperationException("ZhipuAI:Model is required");
            if (Temperature < 0 || Temperature > 2)
                throw new InvalidOperationException("ZhipuAI:Temperature must be between 0 and 2");
            if (MaxTokens < 1 || MaxTokens > 128000)
                throw new InvalidOperationException("ZhipuAI:MaxTokens must be between 1 and 128000");
        }
    }
}
```

### 2.2 修改 ZhipuAIMafAiAgent 使用 IOptions

```csharp
// src/Repository/LLM/ZhipuAIMafAiAgent.cs
using CKY.MultiAgentFramework.Core.Configuration;
using Microsoft.Extensions.Options;

namespace CKY.MultiAgentFramework.Repository.LLM
{
    public class ZhipuAIMafAiAgent : MafAgentBase, ILlmService
    {
        private readonly ZhipuAIOptions _options;

        public ZhipuAIMafAiAgent(
            IOptions<ZhipuAIOptions> options,
            IMafSessionStorage sessionStorage,
            IPriorityCalculator priorityCalculator,
            IMetricsCollector metricsCollector,
            ILogger<ZhipuAIMafAiAgent> logger)
            : base(sessionStorage, priorityCalculator, metricsCollector, logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _options.Validate(); // 验证配置
        }

        protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
        {
            // 使用 _options.ApiKey, _options.Model 等
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(_options.BaseUrl)
            };
            // ...
        }
    }
}
```

### 2.3 在 Program.cs 中注册

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置 ZhipuAI 选项
        builder.Services.Configure<ZhipuAIOptions>(
            builder.Configuration.GetSection(ZhipuAIOptions.SectionName));

        // 注册 ZhipuAI Agent
        builder.Services.AddSingleton<ZhipuAIMafAiAgent>();
        builder.Services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<ZhipuAIMafAiAgent>());

        var app = builder.Build();
        app.Run();
    }
}
```

## 方法 3：使用工厂模式（支持多提供商）

### 3.1 创建 LLM 服务工厂

```csharp
// src/Services/Configuration/LlmServiceFactory.cs
using CKY.MultiAgentFramework.Core.Configuration;
using CKY.MultiAgentFramework.Repository.LLM;
using Microsoft.Extensions.Options;

namespace CKY.MultiAgentFramework.Services.Configuration
{
    /// <summary>
    /// LLM 服务工厂
    /// 根据配置动态创建和注册 LLM 服务
    /// </summary>
    public static class LlmServiceFactory
    {
        /// <summary>
        /// 从配置注册所有 LLM 服务
        /// </summary>
        public static IServiceCollection AddLlmServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 读取主提供商配置
            var primaryProvider = configuration["LLM:PrimaryProvider"] ?? "ZhipuAI";

            // 注册智谱AI
            var zhipuAIEnabled = configuration.GetValue<bool>("LLM:Providers:ZhipuAI:Enabled", true);
            if (zhipuAIEnabled)
            {
                services.Configure<ZhipuAIOptions>(
                    configuration.GetSection("LLM:Providers:ZhipuAI"));

                services.AddSingleton<ZhipuAIMafAiAgent>();

                // 如果是主提供商，注册为 ILlmService
                if (primaryProvider.Equals("ZhipuAI", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<ZhipuAIMafAiAgent>());
                }
            }

            // 注册通义千问（备用）
            var tongyiEnabled = configuration.GetValue<bool>("LLM:Providers:TongyiQianwen:Enabled", false);
            if (tongyiEnabled)
            {
                services.Configure<TongyiQianwenOptions>(
                    configuration.GetSection("LLM:Providers:TongyiQianwen"));

                services.AddSingleton<TongyiQianwenMafAiAgent>();

                // 如果是主提供商，注册为 ILlmService
                if (primaryProvider.Equals("TongyiQianwen", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<TongyiQianwenMafAiAgent>());
                }
            }

            return services;
        }
    }
}
```

### 3.2 在 Program.cs 中使用工厂

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 使用工厂注册所有 LLM 服务
        builder.Services.AddLlmServices(builder.Configuration);

        var app = builder.Build();
        app.Run();
    }
}
```

### 3.3 配置文件示例

**appsettings.json**
```json
{
  "LLM": {
    "PrimaryProvider": "ZhipuAI",
    "Providers": {
      "ZhipuAI": {
        "Enabled": true,
        "ApiKey": "${ZHIPUAI_API_KEY}",
        "Model": "glm-4",
        "Temperature": 0.7,
        "MaxTokens": 2000
      },
      "TongyiQianwen": {
        "Enabled": false,
        "ApiKey": "${TONGYI_API_KEY}",
        "Model": "qwen-max"
      }
    }
  }
}
```

## 方法 4：支持配置热更新

```csharp
// src/Repository/LLM/ZhipuAIMafAiAgent.cs
public class ZhipuAIMafAiAgent : MafAgentBase, ILlmService
{
    private readonly IOptionsMonitor<ZhipuAIOptions> _optionsMonitor;

    public ZhipuAIMafAiAgent(
        IOptionsMonitor<ZhipuAIOptions> optionsMonitor,  // 使用 Monitor 支持热更新
        IMafSessionStorage sessionStorage,
        IPriorityCalculator priorityCalculator,
        IMetricsCollector metricsCollector,
        ILogger<ZhipuAIMafAiAgent> logger)
        : base(sessionStorage, priorityCalculator, metricsCollector, logger)
    {
        _optionsMonitor = optionsMonitor;

        // 监听配置变化
        _optionsMonitor.OnChange(newOptions =>
        {
            Logger.LogInformation("ZhipuAI 配置已更新: {Model}", newOptions.Model);
            newOptions.Validate();
        });
    }

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
    {
        // 每次都获取最新配置
        var options = _optionsMonitor.CurrentValue;
        var apiKey = options.ApiKey;

        // ...
    }
}
```

## 环境变量配置

**开发环境**
```bash
# .env 文件
ZHIPUAI_API_KEY=your-dev-api-key
TONGYI_API_KEY=your-dev-tongyi-key
```

**生产环境**
```bash
# 服务器环境变量
export ZHIPUAI_API_KEY=your-prod-api-key
export TONGYI_API_KEY=your-prod-tongyi-key
```

**Docker Compose**
```yaml
services:
  app:
    environment:
      - ZHIPUAI_API_KEY=${ZHIPUAI_API_KEY}
      - TONGYI_API_KEY=${TONGYI_API_KEY}
```

**Kubernetes Secret**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: llm-secrets
type: Opaque
stringData:
  zhipuai-api-key: your-api-key
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  LLM__Providers__ZhipuAI__ApiKey: "$(ZHIPUAI_API_KEY)"
```

## 配置验证最佳实践

```csharp
// Program.cs
builder.Services.AddOptions<ZhipuAIOptions>()
    .Bind(builder.Configuration.GetSection(ZhipuAIOptions.SectionName))
    .Validate(options =>
    {
        return !string.IsNullOrEmpty(options.ApiKey);
    }, "ApiKey 不能为空")
    .ValidateOnStart(); // 应用启动时验证
```

## 总结

| 方法 | 适用场景 | 优点 | 缺点 |
|------|---------|------|------|
| **IOptions** | 生产环境 | 类型安全、支持验证、热更新 | 需要额外代码 |
| **直接读取** | 快速原型 | 简单直接 | 无验证、无热更新 |
| **工厂模式** | 多提供商 | 灵活切换 | 复杂度较高 |
| **环境变量** | 容器部署 | 安全、灵活 | 需要配置管理 |

**推荐方案**：
- 开发环境：`appsettings.Development.json`
- 生产环境：环境变量 + `IOptions` 模式
- 多提供商：工厂模式 + `IOptionsMonitor` 支持热更新

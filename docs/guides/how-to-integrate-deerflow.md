# .NET / Java 系统集成 DeerFlow 指南

## 概述

[DeerFlow](https://github.com/bytedance/deer-flow)（Deep Exploration and Efficient Research Flow）是字节跳动开源的超级 Agent 运行时框架（Super Agent Harness），基于 Python + LangGraph + LangChain 构建。它提供沙箱执行、持久记忆、Skills 技能系统、子任务编排等企业级 Agent 能力。

对于已有的 .NET 或 Java 系统，DeerFlow 提供了 HTTP Gateway API，可通过标准 HTTP/SSE 协议跨语言集成。

> **何时选择集成 DeerFlow vs 使用 CKY.MAF？**
>
> - 如果你的系统是 .NET 生态 → 优先使用 CKY.MAF SDK-First 模式（进程内调用、零网络开销）
> - 如果你需要 DeerFlow 独有的能力（Docker 沙箱、Skills 渐进加载、MCP 生态） → 通过 HTTP API 集成 DeerFlow
> - 两者可以共存：CKY.MAF 管理 .NET Agent，DeerFlow 处理需要沙箱执行的研究类任务

---

## 架构选型

### 方案一：HTTP Gateway API 集成（推荐）

DeerFlow 通过 Nginx 反向代理统一暴露 API，所有请求经由 `:2026` 端口路由：

```
┌──────────────────────────┐          HTTP / SSE          ┌──────────────────────┐
│  .NET / Java 业务系统     │ ◄═══════════════════════════► │  DeerFlow Backend    │
│                          │                               │  (Nginx :2026)       │
│  HttpClient / WebClient  │    /api/langgraph/*           │  ├── LangGraph :2024 │
│  或 RestTemplate         │    /api/*                     │  └── Gateway  :8001  │
└──────────────────────────┘                               └──────────────────────┘
```

### 方案二：Docker Sidecar 模式

在容器化环境中，DeerFlow 作为 Sidecar 容器与业务系统共享网络。

### 方案三：MCP Server 桥接

将业务系统的能力包装为 MCP Server，DeerFlow Agent 主动调用业务 API（双向集成）。

---

## 前置条件

### 部署 DeerFlow

```bash
# 1. 克隆仓库
git clone https://github.com/bytedance/deer-flow.git
cd deer-flow

# 2. 生成配置文件
make config

# 3. 编辑 config.yaml，配置 LLM 模型
# 至少配置一个模型（如智谱 GLM、DeepSeek、OpenAI 等）

# 4. 设置 API Key
# 编辑 .env 文件
OPENAI_API_KEY=your-key
# 或其他 LLM 提供商的 Key

# 5. Docker 启动（推荐）
make docker-init    # 首次运行
make docker-start   # 启动服务

# 访问 http://localhost:2026 验证运行正常
```

---

## 方案一：HTTP Gateway API 集成

### 核心 API 端点

| 端点 | 方法 | 用途 |
|------|------|------|
| `/api/langgraph/threads` | POST | 创建对话线程 |
| `/api/langgraph/threads/{id}/runs` | POST | 发送任务（阻塞等待结果） |
| `/api/langgraph/threads/{id}/runs/stream` | POST | 发送任务（SSE 流式返回） |
| `/api/models` | GET | 列出可用 LLM 模型 |
| `/api/skills` | GET | 列出已注册技能 |
| `/api/skills` | PUT | 启用/禁用技能 |
| `/api/memory` | GET | 获取持久记忆数据 |
| `/api/memory/reload` | POST | 强制重新加载记忆 |
| `/api/threads/{id}/uploads` | POST | 上传文件（支持 PDF/PPT/Excel/Word 自动转 Markdown） |
| `/api/threads/{id}/uploads/list` | GET | 列出已上传文件 |
| `/api/threads/{id}/artifacts/{path}` | GET | 获取 Agent 生成的产物 |
| `/api/threads/{id}` | DELETE | 删除线程数据 |

### .NET 集成代码

#### 1. 定义响应模型

```csharp
public record DeerFlowThread(string ThreadId);

public record DeerFlowMessage(string Role, string Content);

public record DeerFlowRunRequest
{
    public string AssistantId { get; init; } = "lead_agent";
    public DeerFlowInput Input { get; init; } = new();
    public string[] StreamMode { get; init; } = ["messages-tuple"];
    public DeerFlowConfig? Config { get; init; }
}

public record DeerFlowInput
{
    public DeerFlowMessage[] Messages { get; init; } = [];
}

public record DeerFlowConfig
{
    public int RecursionLimit { get; init; } = 100;
    public DeerFlowContext? Context { get; init; }
}

public record DeerFlowContext
{
    public bool ThinkingEnabled { get; init; }
    public bool IsPlanMode { get; init; }
    public bool SubagentEnabled { get; init; }
}
```

#### 2. 实现客户端

```csharp
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

public sealed class DeerFlowClient : IDisposable
{
    private readonly HttpClient _http;

    public DeerFlowClient(string baseUrl = "http://localhost:2026")
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(10) // Agent 任务可能耗时较长
        };
    }

    /// <summary>创建新的对话线程</summary>
    public async Task<string> CreateThreadAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("/api/langgraph/threads", null, ct);
        response.EnsureSuccessStatusCode();
        var thread = await response.Content.ReadFromJsonAsync<DeerFlowThread>(ct);
        return thread?.ThreadId ?? throw new InvalidOperationException("Failed to create thread");
    }

    /// <summary>发送消息并流式接收回复（SSE）</summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string threadId,
        string message,
        bool planMode = false,
        bool subagentEnabled = true,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/langgraph/threads/{Uri.EscapeDataString(threadId)}/runs/stream")
        {
            Content = JsonContent.Create(new DeerFlowRunRequest
            {
                Input = new DeerFlowInput
                {
                    Messages = [new DeerFlowMessage("user", message)]
                },
                Config = new DeerFlowConfig
                {
                    Context = new DeerFlowContext
                    {
                        IsPlanMode = planMode,
                        SubagentEnabled = subagentEnabled
                    }
                }
            })
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;              // 流结束
            if (!line.StartsWith("data: ")) continue; // 跳过非数据行

            var data = line[6..];
            if (data == "[DONE]") break;

            yield return data;
        }
    }

    /// <summary>发送消息并等待完整回复</summary>
    public async Task<string> ChatAsync(string threadId, string message, CancellationToken ct = default)
    {
        var request = new DeerFlowRunRequest
        {
            AssistantId = "lead_agent",
            Input = new DeerFlowInput
            {
                Messages = [new DeerFlowMessage("user", message)]
            },
            StreamMode = ["values"]
        };

        var response = await _http.PostAsJsonAsync(
            $"/api/langgraph/threads/{Uri.EscapeDataString(threadId)}/runs/wait",
            request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(ct);
        return result;
    }

    /// <summary>列出可用模型</summary>
    public async Task<JsonDocument> ListModelsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/models", ct);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    /// <summary>上传文件到线程</summary>
    public async Task UploadFileAsync(string threadId, string filePath, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(filePath);
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

        var response = await _http.PostAsync(
            $"/api/threads/{Uri.EscapeDataString(threadId)}/uploads", content, ct);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}
```

#### 3. 使用示例

```csharp
// ASP.NET Core 中注册
builder.Services.AddSingleton(new DeerFlowClient("http://localhost:2026"));

// Controller 中使用
[ApiController]
[Route("api/[controller]")]
public class ResearchController : ControllerBase
{
    private readonly DeerFlowClient _deerFlow;

    public ResearchController(DeerFlowClient deerFlow) => _deerFlow = deerFlow;

    [HttpPost("research")]
    public async IAsyncEnumerable<string> Research(
        [FromBody] string query,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var threadId = await _deerFlow.CreateThreadAsync(ct);

        await foreach (var chunk in _deerFlow.StreamAsync(threadId, query,
            planMode: true, subagentEnabled: true, ct: ct))
        {
            yield return chunk;
        }
    }
}
```

### Java 集成代码

#### 使用 Spring WebFlux（响应式）

```java
import org.springframework.web.reactive.function.client.WebClient;
import reactor.core.publisher.Flux;

public class DeerFlowClient {
    private final WebClient webClient;

    public DeerFlowClient(String baseUrl) {
        this.webClient = WebClient.builder()
            .baseUrl(baseUrl)
            .build();
    }

    /**
     * 流式发送消息
     */
    public Flux<String> stream(String threadId, String message) {
        var body = Map.of(
            "assistant_id", "lead_agent",
            "input", Map.of(
                "messages", List.of(Map.of("role", "user", "content", message))
            ),
            "stream_mode", List.of("messages-tuple")
        );

        return webClient.post()
            .uri("/api/langgraph/threads/{id}/runs/stream", threadId)
            .bodyValue(body)
            .retrieve()
            .bodyToFlux(String.class);
    }

    /**
     * 创建对话线程
     */
    public Mono<String> createThread() {
        return webClient.post()
            .uri("/api/langgraph/threads")
            .retrieve()
            .bodyToMono(JsonNode.class)
            .map(node -> node.get("thread_id").asText());
    }
}
```

---

## 方案二：Docker Sidecar 模式

适用于 Kubernetes 或 Docker Compose 容器化部署场景。

```yaml
# docker-compose.yml
version: '3.8'

services:
  # 你的 .NET 业务系统
  your-app:
    build: ./your-app
    ports:
      - "8080:8080"
    environment:
      DEERFLOW_URL: http://deerflow:2026
    depends_on:
      - deerflow

  # DeerFlow Agent 引擎
  deerflow:
    build:
      context: ./deer-flow
      dockerfile: docker/Dockerfile
    volumes:
      - ./config.yaml:/app/config.yaml
      - ./skills:/app/skills/custom    # 挂载自定义技能
      - deerflow-data:/mnt/user-data   # 持久化工作数据
    environment:
      OPENAI_API_KEY: ${OPENAI_API_KEY}
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:2026/api/models"]
      interval: 30s
      timeout: 5s
      retries: 3

volumes:
  deerflow-data:
```

---

## 方案三：MCP Server 桥接（双向集成）

将 .NET/Java 业务能力暴露为 MCP Server，让 DeerFlow Agent 主动调用。

### .NET 端：暴露 MCP Server

```csharp
// 使用 MCP .NET SDK（社区实现）
// 将业务能力注册为 MCP Tool

[McpTool("query_order", "查询订单信息")]
public async Task<string> QueryOrder(
    [McpParameter("order_id", "订单ID")] string orderId)
{
    var order = await _orderService.GetByIdAsync(orderId);
    return JsonSerializer.Serialize(order);
}
```

### DeerFlow 端：注册 MCP Server

编辑 `extensions_config.json`：

```json
{
  "mcpServers": {
    "your-business-tools": {
      "enabled": true,
      "type": "http",
      "url": "http://your-app:9090/mcp",
      "description": "业务系统工具集 - 订单查询、库存管理等"
    }
  }
}
```

注册后，DeerFlow 的 Lead Agent 会自动发现这些工具，并在需要时调用。

---

## 执行模式

DeerFlow 支持多种执行模式，通过请求参数控制：

| 模式 | 请求参数 | 说明 |
|------|---------|------|
| Flash（快速） | `is_plan_mode: false, subagent_enabled: false` | 单次直答，最快响应 |
| Standard（标准） | `is_plan_mode: false, subagent_enabled: false` | 带工具调用的标准对话 |
| Pro（规划） | `is_plan_mode: true, subagent_enabled: false` | 先规划再执行 |
| Ultra（子任务） | `is_plan_mode: true, subagent_enabled: true` | 分解为子任务并行执行 |

建议：
- 简单问答 → Flash
- 需要搜索/工具 → Standard
- 复杂研究 → Pro
- 大规模多步任务 → Ultra

---

## DeerFlow 独特能力利用

### Skills 技能系统

DeerFlow 的技能以 Markdown 文件定义，支持渐进式加载（按需注入上下文）。你可以创建自定义技能：

```markdown
# skills/custom/your-domain-skill/SKILL.md

## Your Domain Skill

When the user asks about [your domain], follow these steps:
1. First gather context using web_search
2. Then analyze the data
3. Generate a structured report

### Output Format
- Summary section
- Key findings
- Recommendations
```

### 沙箱代码执行

DeerFlow 的 Agent 在隔离的 Docker 容器中执行代码，对于需要动态代码生成和执行的场景（数据分析、报告生成）非常有用。

### 长期记忆

DeerFlow 自动从对话中提取用户偏好和事实，跨 session 持久化。这在企业场景中可积累领域知识。

---

## 监控与运维

### 健康检查

```bash
curl http://localhost:2026/api/models
# 返回 200 且包含模型列表 = 服务正常
```

### IM 渠道集成

DeerFlow 原生支持以下即时通讯渠道（无需公网 IP）：

| 渠道 | 协议 | 适用场景 |
|------|------|---------|
| 飞书 | WebSocket | 国内企业 |
| Slack | Socket Mode | 海外团队 |
| Telegram | Bot API (长轮询) | 个人/小团队 |

在 `config.yaml` 中启用对应渠道即可。

---

## 常见问题

### Q: DeerFlow 支持中国大模型吗？

支持。通过 OpenAI 兼容接口即可接入智谱 GLM、DeepSeek、Kimi 等模型。在 `config.yaml` 中配置 `base_url` 指向对应提供商的 API 端点即可。

### Q: 与 CKY.MAF 如何共存？

推荐架构：CKY.MAF 负责 .NET 原生 Agent 编排（进程内高性能调用），DeerFlow 负责需要沙箱执行或深度研究的任务（通过 HTTP API 调用）。两者通过 HTTP 解耦，互不干扰。

### Q: 网络延迟如何优化？

- 使用 Docker Sidecar 模式减少网络跳转
- 对非实时任务使用异步轮询而非 SSE 流式
- 合理设置 `recursion_limit` 避免 Agent 过度递归

### Q: 安全性如何保障？

- DeerFlow 的沙箱在隔离容器中执行代码，不影响宿主
- API 默认无认证，生产环境应在 Nginx 层添加认证中间件
- 文件上传支持格式校验，自动拒绝可疑文件

---

## 参考链接

- [DeerFlow GitHub 仓库](https://github.com/bytedance/deer-flow)
- [DeerFlow 官网](https://deerflow.tech/)
- [DeerFlow 后端架构文档](https://github.com/bytedance/deer-flow/blob/main/backend/README.md)
- [DeerFlow 配置指南](https://github.com/bytedance/deer-flow/blob/main/backend/docs/CONFIGURATION.md)
- [DeerFlow API 参考](https://github.com/bytedance/deer-flow/blob/main/backend/docs/API.md)

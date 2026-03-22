using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.Factory;
using Microsoft.Extensions.DependencyInjection;

namespace CKY.MultiAgentFramework.Services.DependencyInjection;

/// <summary>
/// LLM Agent 工厂服务注册扩展方法
/// </summary>
public static class LlmAgentFactoryServiceExtensions
{
    /// <summary>
    /// 注册 LLM Agent 工厂及其依赖的 HttpClient
    /// </summary>
    /// <remarks>
    /// 为每个 LLM 提供商配置独立的 Named HttpClient，
    /// 避免 socket 耗尽问题并支持提供商级别的策略配置。
    /// </remarks>
    public static IServiceCollection AddLlmAgentFactory(
        this IServiceCollection services,
        Action<LlmHttpClientOptions>? configureOptions = null)
    {
        var options = new LlmHttpClientOptions();
        configureOptions?.Invoke(options);

        // 注册 IHttpClientFactory 并为各提供商配置 Named HttpClient
        services.AddHttpClient("ZhipuAIAgent", client =>
        {
            client.Timeout = options.DefaultTimeout;
        });

        services.AddHttpClient("TongyiLlmAgent", client =>
        {
            client.Timeout = options.DefaultTimeout;
        });

        services.AddHttpClient("WenxinLlmAgent", client =>
        {
            client.Timeout = options.DefaultTimeout;
        });

        services.AddHttpClient("XunfeiLlmAgent", client =>
        {
            client.Timeout = options.DefaultTimeout;
        });

        services.AddHttpClient("BaichuanLlmAgent", client =>
        {
            client.Timeout = options.DefaultTimeout;
        });

        services.AddHttpClient("MiniMaxLlmAgent", client =>
        {
            client.Timeout = options.DefaultTimeout;
        });

        // 注册 LlmAgentFactory
        services.AddScoped<ILlmAgentFactory, LlmAgentFactory>();

        return services;
    }
}

/// <summary>
/// LLM HttpClient 配置选项
/// </summary>
public class LlmHttpClientOptions
{
    /// <summary>默认请求超时时间（默认 60 秒）</summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(60);
}

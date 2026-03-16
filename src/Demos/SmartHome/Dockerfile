# CKY.MAF SmartHome Demo - Multi-stage Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# 复制 solution 和 项目文件以利用 Docker 缓存层
COPY CKY.MAF.slnx .
COPY Core/CKY.MAF.Core.csproj Core/
COPY Services/CKY.MAF.Services.csproj Services/
COPY Infrastructure/Repository/CKY.MAF.Infrastructure.Repository.csproj Infrastructure/Repository/
COPY Infrastructure/Caching/CKY.MAF.Infrastructure.Caching.csproj Infrastructure/Caching/
COPY Infrastructure/Vectorization/CKY.MAF.Infrastructure.Vectorization.csproj Infrastructure/Vectorization/
COPY Infrastructure/DependencyInjection/CKY.MAF.Infrastructure.DependencyInjection.csproj Infrastructure/DependencyInjection/
COPY Demos/SmartHome/CKY.MAF.Demos.SmartHome.csproj Demos/SmartHome/
COPY Demos/CustomerService/CKY.MAF.Demos.CustomerService.csproj Demos/CustomerService/

# 还原 NuGet 包
RUN dotnet restore Demos/SmartHome/CKY.MAF.Demos.SmartHome.csproj

# 复制全部源码并构建
COPY . .

# 清理可能的本地 NuGet 配置，设置干净的构建环境
ENV NUGET_FALLBACK_PACKAGES=""
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN dotnet publish Demos/SmartHome/CKY.MAF.Demos.SmartHome.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# 创建 SQLite 数据目录
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CKY.MAF.Demos.SmartHome.dll"]

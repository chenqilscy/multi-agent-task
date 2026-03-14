# 批量更新 Repository 文件的命名空间
$files = @(
    "src\Infrastructure\Repository\Data\EntityTypeConfigurations\LlmProviderConfigConfiguration.cs",
    "src\Infrastructure\Repository\Data\EntityTypeConfigurations\MainTaskConfiguration.cs",
    "src\Infrastructure\Repository\Data\EntityTypeConfigurations\SchedulePlanConfiguration.cs",
    "src\Infrastructure\Repository\Data\EntityTypeConfigurations\SessionConfiguration.cs",
    "src\Infrastructure\Repository\Data\EntityTypeConfigurations\SubTaskConfiguration.cs",
    "src\Infrastructure\Repository\Relational\EfCoreRelationalDatabase.cs",
    "src\Infrastructure\Repository\Repositories\LlmProviderConfigRepository.cs",
    "src\Infrastructure\Repository\Repositories\MainTaskRepository.cs",
    "src\Infrastructure\Repository\Repositories\SchedulePlanRepository.cs",
    "src\Infrastructure\Repository\Repositories\SubTaskRepository.cs",
    "src\Infrastructure\Repository\Repositories\UnitOfWork.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        (Get-Content $file) -replace 'namespace CKY\.MultiAgentFramework\.Repository', 'namespace CKY.MultiAgentFramework.Infrastructure.Repository' | Set-Content $file
        (Get-Content $file) -replace 'using CKY\.MultiAgentFramework\.Repository', 'using CKY.MultiAgentFramework.Infrastructure.Repository' | Set-Content $file
        Write-Host "Updated: $file"
    }
}

Write-Host "Namespace update completed!"

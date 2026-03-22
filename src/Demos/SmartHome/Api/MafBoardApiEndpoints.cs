using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MAF.Demos.SmartHome.Api;

public static class MafBoardApiEndpoints
{
    public static WebApplication MapMafBoardApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks")
            .RequireCors("MafBoard");

        group.MapGet("/", GetAllTasks);
        group.MapGet("/{id:int}", GetTaskById);
        group.MapPost("/{id:int}/cancel", CancelTask);

        return app;
    }

    private static async Task<IResult> GetAllTasks(
        IMainTaskRepository repo,
        string? status,
        string? search,
        CancellationToken ct)
    {
        List<MainTask> tasks;

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MafTaskStatus>(status, true, out var parsed))
        {
            tasks = await repo.GetByStatusAsync(parsed, ct);
        }
        else
        {
            tasks = await repo.GetAllAsync(ct);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var q = search.ToLowerInvariant();
            tasks = tasks.Where(t =>
                (t.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                t.SubTasks.Any(s =>
                    s.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (s.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            ).ToList();
        }

        var dtos = tasks.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetTaskById(
        int id,
        IMainTaskRepository repo,
        CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(id, ct);
        if (task is null)
            return Results.NotFound();

        return Results.Ok(MapToDto(task));
    }

    private static async Task<IResult> CancelTask(
        int id,
        IMainTaskRepository repo,
        CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(id, ct);
        if (task is null)
            return Results.NotFound();

        if (task.Status is MafTaskStatus.Completed or MafTaskStatus.Cancelled)
            return Results.Conflict(new { error = "任务已完成或已取消，无法取消" });

        task.Status = MafTaskStatus.Cancelled;
        task.UpdatedAt = DateTime.UtcNow;

        foreach (var sub in task.SubTasks)
        {
            if (sub.Status is MafTaskStatus.Pending or MafTaskStatus.Running or MafTaskStatus.Ready or MafTaskStatus.Scheduled)
            {
                sub.Status = MafTaskStatus.Cancelled;
            }
        }

        await repo.UpdateAsync(task, ct);

        return Results.Ok(MapToDto(task));
    }

    private static MafBoardTaskDto MapToDto(MainTask task)
    {
        return new MafBoardTaskDto
        {
            Id = task.Id.ToString(),
            Name = task.Title,
            Description = task.Description ?? string.Empty,
            Status = MapStatus(task.Status),
            LeaderAgentId = "smart-home-controller",
            LeaderAgentName = "SmartHomeControlService",
            UserInput = task.Description ?? task.Title,
            CreatedAt = task.CreatedAt,
            StartedAt = task.Status >= MafTaskStatus.Running ? task.CreatedAt : null,
            CompletedAt = task.Status is MafTaskStatus.Completed or MafTaskStatus.Failed or MafTaskStatus.Cancelled
                ? task.UpdatedAt
                : null,
            SubTasks = task.SubTasks.Select(MapSubTaskToDto).ToList(),
            Logs = []
        };
    }

    private static MafBoardSubTaskDto MapSubTaskToDto(SubTask sub)
    {
        return new MafBoardSubTaskDto
        {
            Id = sub.Id.ToString(),
            ParentTaskId = sub.MainTaskId.ToString(),
            Name = sub.Title,
            Description = sub.Description ?? string.Empty,
            Status = MapStatus(sub.Status),
            AgentId = $"agent-{sub.ExecutionOrder}",
            AgentName = sub.Title,
            Logs = []
        };
    }

    private static string MapStatus(MafTaskStatus status) => status switch
    {
        MafTaskStatus.Pending or MafTaskStatus.Ready or MafTaskStatus.Scheduled => "pending",
        MafTaskStatus.Running => "running",
        MafTaskStatus.Completed => "completed",
        MafTaskStatus.Failed or MafTaskStatus.Timeout => "failed",
        MafTaskStatus.Cancelled => "cancelled",
        _ => "pending"
    };
}

// DTOs matching maf-board TypeScript types
public record MafBoardTaskDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public required string LeaderAgentId { get; init; }
    public required string LeaderAgentName { get; init; }
    public required string UserInput { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required List<MafBoardSubTaskDto> SubTasks { get; init; }
    public required List<MafBoardLogDto> Logs { get; init; }
}

public record MafBoardSubTaskDto
{
    public required string Id { get; init; }
    public required string ParentTaskId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public required string AgentId { get; init; }
    public required string AgentName { get; init; }
    public string? StartedAt { get; init; }
    public string? CompletedAt { get; init; }
    public string? Result { get; init; }
    public string? Error { get; init; }
    public required List<MafBoardLogDto> Logs { get; init; }
}

public record MafBoardLogDto
{
    public required string Id { get; init; }
    public required string Timestamp { get; init; }
    public required string AgentId { get; init; }
    public required string AgentName { get; init; }
    public required string Role { get; init; }
    public required string Action { get; init; }
    public required string Message { get; init; }
    public int? Duration { get; init; }
    public required string Status { get; init; }
}

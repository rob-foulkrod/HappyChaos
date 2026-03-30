using HappyChaos.Todo.Models;

namespace HappyChaos.Todo.Services;

public class TodoService
{
    private readonly ITodoRepository _repository;

    public TodoService(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoTask>> GetAllAsync()
        => await _repository.GetAllAsync();

    public async Task<IEnumerable<TodoTask>> GetByStatusAsync(Models.TaskStatus status)
        => await _repository.GetByStatusAsync(status);

    public async Task<TodoTask?> GetByIdAsync(int id)
        => await _repository.GetByIdAsync(id);

    public async Task<TodoTask> AddAsync(TodoTask task)
        => await _repository.AddAsync(task);

    public async Task<bool> UpdateAsync(TodoTask task)
        => await _repository.UpdateAsync(task);

    public async Task<bool> DeleteAsync(int id)
        => await _repository.DeleteAsync(id);

    public async Task<bool> ToggleCompleteAsync(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null) return false;

        task.Status = task.Status == Models.TaskStatus.Completed
            ? Models.TaskStatus.InProgress
            : Models.TaskStatus.Completed;
        task.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(task);
    }

    public async Task<TaskSummary> GetSummaryAsync()
    {
        var tasks = (await _repository.GetAllAsync()).ToList();
        return new TaskSummary
        {
            Total = tasks.Count,
            NotStarted = tasks.Count(t => t.Status == Models.TaskStatus.NotStarted),
            InProgress = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
            OnHold = tasks.Count(t => t.Status == Models.TaskStatus.OnHold),
            Completed = tasks.Count(t => t.Status == Models.TaskStatus.Completed),
            Overdue = tasks.Count(t => t.IsOverdue),
            DueSoon = tasks.Count(t => t.IsDueSoon)
        };
    }

    public async Task<TodoBackup> ExportAsync()
    {
        var tasks = (await _repository.GetAllAsync()).ToList();
        var categories = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.Category))
            .Select(t => t.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return new TodoBackup
        {
            Tasks = tasks,
            Categories = categories
        };
    }

    public async Task ImportAsync(TodoBackup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);
        await _repository.ReplaceAllAsync(backup.Tasks);
    }
}

public class TaskSummary
{
    public int Total { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int OnHold { get; set; }
    public int Completed { get; set; }
    public int Overdue { get; set; }
    public int DueSoon { get; set; }
}

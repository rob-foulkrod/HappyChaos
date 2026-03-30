using HappyChaos.Todo.Models;

namespace HappyChaos.Todo.Services;

public class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<TodoTask> _tasks = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public InMemoryTodoRepository()
    {
        SeedData();
    }

    private void SeedData()
    {
        var now = DateTime.UtcNow;
        _tasks.AddRange(new[]
        {
            new TodoTask
            {
                Id = _nextId++,
                Title = "Set up Azure DevOps Pipeline",
                Description = "Configure CI/CD pipeline for automatic deployments to Azure App Service.",
                Priority = TaskPriority.High,
                Status = Models.TaskStatus.InProgress,
                DueDate = now.AddDays(5),
                AssignedTo = "Team",
                Category = "DevOps",
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1)
            },
            new TodoTask
            {
                Id = _nextId++,
                Title = "Design database schema",
                Description = "Create Entity Framework models and migrations for multi-user support.",
                Priority = TaskPriority.High,
                Status = Models.TaskStatus.NotStarted,
                DueDate = now.AddDays(7),
                AssignedTo = "Team",
                Category = "Backend",
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            },
            new TodoTask
            {
                Id = _nextId++,
                Title = "Implement user authentication",
                Description = "Add ASP.NET Core Identity for multi-user sign-in support.",
                Priority = TaskPriority.Medium,
                Status = Models.TaskStatus.NotStarted,
                DueDate = now.AddDays(14),
                AssignedTo = "Team",
                Category = "Security",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new TodoTask
            {
                Id = _nextId++,
                Title = "Write unit tests",
                Description = "Add xUnit test project and write tests for all controllers and services.",
                Priority = TaskPriority.Medium,
                Status = Models.TaskStatus.NotStarted,
                DueDate = now.AddDays(10),
                AssignedTo = "Team",
                Category = "Testing",
                CreatedAt = now,
                UpdatedAt = now
            },
            new TodoTask
            {
                Id = _nextId++,
                Title = "Review project requirements",
                Description = "Walk through the project requirements with the class and confirm scope.",
                Priority = TaskPriority.Low,
                Status = Models.TaskStatus.Completed,
                DueDate = now.AddDays(-2),
                AssignedTo = "Team",
                Category = "Planning",
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-2)
            }
        });
    }

    public Task<IEnumerable<TodoTask>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<TodoTask>>(_tasks.ToList());
        }
    }

    public Task<IEnumerable<TodoTask>> GetByStatusAsync(Models.TaskStatus status)
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<TodoTask>>(_tasks.Where(t => t.Status == status).ToList());
        }
    }

    public Task<TodoTask?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));
        }
    }

    public Task<TodoTask> AddAsync(TodoTask task)
    {
        lock (_lock)
        {
            task.Id = _nextId++;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            _tasks.Add(task);
            return Task.FromResult(task);
        }
    }

    public Task<bool> UpdateAsync(TodoTask task)
    {
        lock (_lock)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing == null) return Task.FromResult(false);

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Priority = task.Priority;
            existing.Status = task.Status;
            existing.DueDate = task.DueDate;
            existing.AssignedTo = task.AssignedTo;
            existing.Category = task.Category;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return Task.FromResult(false);
            _tasks.Remove(task);
            return Task.FromResult(true);
        }
    }

    public Task ReplaceAllAsync(IEnumerable<TodoTask> tasks)
    {
        lock (_lock)
        {
            _tasks.Clear();
            var taskList = tasks.ToList();

            if (taskList.Count > 0)
            {
                _tasks.AddRange(taskList);
                _nextId = _tasks.Max(t => t.Id) + 1;
            }
            else
            {
                _nextId = 1;
            }

            return Task.CompletedTask;
        }
    }
}

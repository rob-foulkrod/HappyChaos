using HappyChaos.Todo.Models;

namespace HappyChaos.Todo.Services;

public class TodoService
{
    private static readonly List<TodoTask> _tasks = new();
    private static int _nextId = 1;
    private static readonly object _lock = new();

    public TodoService()
    {
        // Seed with sample data if empty
        lock (_lock)
        {
            if (_tasks.Count == 0)
            {
                SeedData();
            }
        }
    }

    private static void SeedData()
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

    public IEnumerable<TodoTask> GetAll() 
    {
        lock (_lock)
        {
            return _tasks.ToList();
        }
    }

    public IEnumerable<TodoTask> GetByStatus(Models.TaskStatus status)
    {
        lock (_lock)
        {
            return _tasks.Where(t => t.Status == status).ToList();
        }
    }

    public TodoTask? GetById(int id)
    {
        lock (_lock)
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    public TodoTask Add(TodoTask task)
    {
        lock (_lock)
        {
            task.Id = _nextId++;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            _tasks.Add(task);
            return task;
        }
    }

    public bool Update(TodoTask task)
    {
        lock (_lock)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing == null) return false;

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Priority = task.Priority;
            existing.Status = task.Status;
            existing.DueDate = task.DueDate;
            existing.AssignedTo = task.AssignedTo;
            existing.Category = task.Category;
            existing.UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;
            _tasks.Remove(task);
            return true;
        }
    }

    public bool ToggleComplete(int id)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;
            task.Status = task.Status == Models.TaskStatus.Completed
                ? Models.TaskStatus.InProgress
                : Models.TaskStatus.Completed;
            task.UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }

    public TaskSummary GetSummary()
    {
        lock (_lock)
        {
            return new TaskSummary
            {
                Total = _tasks.Count,
                NotStarted = _tasks.Count(t => t.Status == Models.TaskStatus.NotStarted),
                InProgress = _tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                OnHold = _tasks.Count(t => t.Status == Models.TaskStatus.OnHold),
                Completed = _tasks.Count(t => t.Status == Models.TaskStatus.Completed),
                Overdue = _tasks.Count(t => t.IsOverdue),
                DueSoon = _tasks.Count(t => t.IsDueSoon)
            };
        }
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

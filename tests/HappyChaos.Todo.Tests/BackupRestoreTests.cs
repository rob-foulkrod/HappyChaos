using HappyChaos.Todo.Models;
using HappyChaos.Todo.Services;
using System.Text.Json;

namespace HappyChaos.Todo.Tests;

public class BackupRestoreTests
{
    private static TodoService CreateServiceWithTasks(List<TodoTask> tasks)
    {
        var repo = new InMemoryTodoRepository();
        // Replace seed data with the provided tasks
        repo.ReplaceAllAsync(tasks).GetAwaiter().GetResult();
        return new TodoService(repo);
    }

    private static List<TodoTask> CreateSampleTasks()
    {
        return new List<TodoTask>
        {
            new TodoTask
            {
                Id = 1,
                Title = "Task One",
                Description = "First task",
                Priority = TaskPriority.High,
                Status = Models.TaskStatus.InProgress,
                Category = "DevOps",
                AssignedTo = "Alice",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TodoTask
            {
                Id = 2,
                Title = "Task Two",
                Description = "Second task",
                Priority = TaskPriority.Medium,
                Status = Models.TaskStatus.NotStarted,
                Category = "Backend",
                AssignedTo = "Bob",
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TodoTask
            {
                Id = 3,
                Title = "Task Three",
                Description = "Third task",
                Priority = TaskPriority.Low,
                Status = Models.TaskStatus.Completed,
                Category = "DevOps",
                AssignedTo = "Alice",
                CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc)
            }
        };
    }

    [Fact]
    public async Task Export_ReturnsAllTasks()
    {
        var tasks = CreateSampleTasks();
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();

        Assert.Equal(3, backup.Tasks.Count);
    }

    [Fact]
    public async Task Export_ReturnsDistinctCategories()
    {
        var tasks = CreateSampleTasks();
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();

        Assert.Equal(2, backup.Categories.Count);
        Assert.Contains("DevOps", backup.Categories);
        Assert.Contains("Backend", backup.Categories);
    }

    [Fact]
    public async Task Export_CategoriesAreSorted()
    {
        var tasks = CreateSampleTasks();
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();

        Assert.Equal(new List<string> { "Backend", "DevOps" }, backup.Categories);
    }

    [Fact]
    public async Task Export_PreservesTaskProperties()
    {
        var tasks = CreateSampleTasks();
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();

        var task = backup.Tasks.First(t => t.Title == "Task One");
        Assert.Equal("First task", task.Description);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(Models.TaskStatus.InProgress, task.Status);
        Assert.Equal("DevOps", task.Category);
        Assert.Equal("Alice", task.AssignedTo);
    }

    [Fact]
    public async Task Export_ExcludesNullAndEmptyCategories()
    {
        var tasks = new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "With Category", Category = "Work" },
            new TodoTask { Id = 2, Title = "Null Category", Category = null },
            new TodoTask { Id = 3, Title = "Empty Category", Category = "" },
            new TodoTask { Id = 4, Title = "Whitespace Category", Category = "   " }
        };
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();

        Assert.Single(backup.Categories);
        Assert.Equal("Work", backup.Categories[0]);
    }

    [Fact]
    public async Task Import_ReplacesExistingTasks()
    {
        var service = CreateServiceWithTasks(CreateSampleTasks());

        var newTasks = new List<TodoTask>
        {
            new TodoTask { Id = 10, Title = "New Task A", Category = "New" },
            new TodoTask { Id = 11, Title = "New Task B", Category = "New" }
        };
        var newBackup = new TodoBackup { Tasks = newTasks, Categories = new List<string> { "New" } };

        await service.ImportAsync(newBackup);

        var allTasks = (await service.GetAllAsync()).ToList();
        Assert.Equal(2, allTasks.Count);
        Assert.Equal("New Task A", allTasks[0].Title);
        Assert.Equal("New Task B", allTasks[1].Title);
    }

    [Fact]
    public async Task Import_OldTasksAreRemoved()
    {
        var service = CreateServiceWithTasks(CreateSampleTasks());

        var newBackup = new TodoBackup
        {
            Tasks = new List<TodoTask>
            {
                new TodoTask { Id = 1, Title = "Only Task" }
            },
            Categories = new List<string>()
        };

        await service.ImportAsync(newBackup);

        Assert.Null(await service.GetByIdAsync(2));
        Assert.Null(await service.GetByIdAsync(3));
    }

    [Fact]
    public async Task Import_EmptyBackupClearsAllTasks()
    {
        var service = CreateServiceWithTasks(CreateSampleTasks());

        await service.ImportAsync(new TodoBackup { Tasks = new List<TodoTask>(), Categories = new List<string>() });

        var allTasks = (await service.GetAllAsync()).ToList();
        Assert.Empty(allTasks);
    }

    [Fact]
    public async Task Import_NewTasksGetCorrectIds()
    {
        var service = CreateServiceWithTasks(CreateSampleTasks());

        var newBackup = new TodoBackup
        {
            Tasks = new List<TodoTask>
            {
                new TodoTask { Id = 50, Title = "High ID Task" }
            },
            Categories = new List<string>()
        };
        await service.ImportAsync(newBackup);

        // After import, adding a new task should get the next sequential ID
        var addedTask = await service.AddAsync(new TodoTask { Title = "Added After Import" });
        Assert.Equal(51, addedTask.Id);
    }

    [Fact]
    public async Task Import_ThrowsOnNullBackup()
    {
        var service = CreateServiceWithTasks(CreateSampleTasks());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ImportAsync(null!));
    }

    [Fact]
    public async Task ExportThenImport_RoundTrip_PreservesData()
    {
        var tasks = CreateSampleTasks();
        var service = CreateServiceWithTasks(tasks);

        // Export
        var backup = await service.ExportAsync();

        // Serialize to JSON (simulates saving/loading a file)
        var json = JsonSerializer.Serialize(backup);

        // Deserialize
        var restoredBackup = JsonSerializer.Deserialize<TodoBackup>(json);
        Assert.NotNull(restoredBackup);

        // Import into a fresh state
        await service.ImportAsync(restoredBackup);

        // Verify all tasks are preserved
        var restoredTasks = (await service.GetAllAsync()).ToList();
        Assert.Equal(tasks.Count, restoredTasks.Count);

        for (int i = 0; i < tasks.Count; i++)
        {
            Assert.Equal(tasks[i].Id, restoredTasks[i].Id);
            Assert.Equal(tasks[i].Title, restoredTasks[i].Title);
            Assert.Equal(tasks[i].Description, restoredTasks[i].Description);
            Assert.Equal(tasks[i].Priority, restoredTasks[i].Priority);
            Assert.Equal(tasks[i].Status, restoredTasks[i].Status);
            Assert.Equal(tasks[i].Category, restoredTasks[i].Category);
            Assert.Equal(tasks[i].AssignedTo, restoredTasks[i].AssignedTo);
        }
    }

    [Fact]
    public async Task ExportThenImport_RoundTrip_JsonContainsCategoriesAndTasks()
    {
        var tasks = CreateSampleTasks();
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();
        var json = JsonSerializer.Serialize(backup);

        // Verify the JSON document contains both tasks and categories keys
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("Tasks", out _) ||
                    doc.RootElement.TryGetProperty("tasks", out _));
        Assert.True(doc.RootElement.TryGetProperty("Categories", out _) ||
                    doc.RootElement.TryGetProperty("categories", out _));
    }

    [Fact]
    public async Task Export_WithNoTasks_ReturnsEmptyBackup()
    {
        var service = CreateServiceWithTasks(new List<TodoTask>());

        var backup = await service.ExportAsync();

        Assert.Empty(backup.Tasks);
        Assert.Empty(backup.Categories);
    }

    [Fact]
    public async Task Import_PreservesTaskDueDates()
    {
        var dueDate = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var tasks = new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Due Date Task", DueDate = dueDate }
        };
        var service = CreateServiceWithTasks(tasks);

        var backup = await service.ExportAsync();
        var json = JsonSerializer.Serialize(backup);
        var restoredBackup = JsonSerializer.Deserialize<TodoBackup>(json)!;

        await service.ImportAsync(restoredBackup);

        var restored = await service.GetByIdAsync(1);
        Assert.NotNull(restored);
        Assert.Equal(dueDate, restored.DueDate);
    }
}

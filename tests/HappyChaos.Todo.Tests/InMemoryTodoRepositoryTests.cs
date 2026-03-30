using HappyChaos.Todo.Models;
using HappyChaos.Todo.Services;

namespace HappyChaos.Todo.Tests;

public class InMemoryTodoRepositoryTests
{
    private static InMemoryTodoRepository CreateEmptyRepo()
    {
        var repo = new InMemoryTodoRepository();
        // Clear seed data for isolated tests
        repo.ReplaceAllAsync(new List<TodoTask>()).GetAwaiter().GetResult();
        return repo;
    }

    private static InMemoryTodoRepository CreateRepoWithTasks(List<TodoTask> tasks)
    {
        var repo = new InMemoryTodoRepository();
        repo.ReplaceAllAsync(tasks).GetAwaiter().GetResult();
        return repo;
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_SeedData_ReturnsSeededTasks()
    {
        var repo = new InMemoryTodoRepository();
        var tasks = (await repo.GetAllAsync()).ToList();
        Assert.Equal(5, tasks.Count);
    }

    [Fact]
    public async Task GetAllAsync_Empty_ReturnsEmptyList()
    {
        var repo = CreateEmptyRepo();
        var tasks = (await repo.GetAllAsync()).ToList();
        Assert.Empty(tasks);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTask()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Test Task" }
        });

        var task = await repo.GetByIdAsync(1);
        Assert.NotNull(task);
        Assert.Equal("Test Task", task.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var repo = CreateEmptyRepo();
        var task = await repo.GetByIdAsync(999);
        Assert.Null(task);
    }

    // --- GetByStatusAsync ---

    [Fact]
    public async Task GetByStatusAsync_FiltersCorrectly()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Active", Status = Models.TaskStatus.InProgress },
            new TodoTask { Id = 2, Title = "Done", Status = Models.TaskStatus.Completed },
            new TodoTask { Id = 3, Title = "Also Active", Status = Models.TaskStatus.InProgress }
        });

        var inProgress = (await repo.GetByStatusAsync(Models.TaskStatus.InProgress)).ToList();
        Assert.Equal(2, inProgress.Count);
        Assert.All(inProgress, t => Assert.Equal(Models.TaskStatus.InProgress, t.Status));
    }

    [Fact]
    public async Task GetByStatusAsync_NoMatches_ReturnsEmpty()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Active", Status = Models.TaskStatus.InProgress }
        });

        var completed = (await repo.GetByStatusAsync(Models.TaskStatus.Completed)).ToList();
        Assert.Empty(completed);
    }

    // --- AddAsync ---

    [Fact]
    public async Task AddAsync_AssignsId()
    {
        var repo = CreateEmptyRepo();
        var task = await repo.AddAsync(new TodoTask { Title = "New Task" });
        Assert.True(task.Id > 0);
    }

    [Fact]
    public async Task AddAsync_AssignsSequentialIds()
    {
        var repo = CreateEmptyRepo();
        var task1 = await repo.AddAsync(new TodoTask { Title = "First" });
        var task2 = await repo.AddAsync(new TodoTask { Title = "Second" });
        Assert.Equal(task1.Id + 1, task2.Id);
    }

    [Fact]
    public async Task AddAsync_SetsTimestamps()
    {
        var repo = CreateEmptyRepo();
        var before = DateTime.UtcNow;
        var task = await repo.AddAsync(new TodoTask { Title = "New Task" });
        var after = DateTime.UtcNow;

        Assert.InRange(task.CreatedAt, before, after);
        Assert.InRange(task.UpdatedAt, before, after);
    }

    [Fact]
    public async Task AddAsync_TaskIsRetrievable()
    {
        var repo = CreateEmptyRepo();
        var added = await repo.AddAsync(new TodoTask { Title = "Findable" });
        var found = await repo.GetByIdAsync(added.Id);
        Assert.NotNull(found);
        Assert.Equal("Findable", found.Title);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ExistingTask_ReturnsTrue()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Original" }
        });

        var result = await repo.UpdateAsync(new TodoTask { Id = 1, Title = "Updated" });
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_UpdatesFields()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Original", Priority = TaskPriority.Low }
        });

        await repo.UpdateAsync(new TodoTask
        {
            Id = 1,
            Title = "Updated",
            Priority = TaskPriority.Critical,
            Description = "New desc",
            Category = "Work"
        });

        var task = await repo.GetByIdAsync(1);
        Assert.NotNull(task);
        Assert.Equal("Updated", task.Title);
        Assert.Equal(TaskPriority.Critical, task.Priority);
        Assert.Equal("New desc", task.Description);
        Assert.Equal("Work", task.Category);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Original", UpdatedAt = DateTime.UtcNow.AddDays(-1) }
        });

        var before = DateTime.UtcNow;
        await repo.UpdateAsync(new TodoTask { Id = 1, Title = "Updated" });
        var after = DateTime.UtcNow;

        var task = await repo.GetByIdAsync(1);
        Assert.NotNull(task);
        Assert.InRange(task.UpdatedAt, before, after);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentTask_ReturnsFalse()
    {
        var repo = CreateEmptyRepo();
        var result = await repo.UpdateAsync(new TodoTask { Id = 999, Title = "Ghost" });
        Assert.False(result);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ExistingTask_ReturnsTrue()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "To Delete" }
        });

        var result = await repo.DeleteAsync(1);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_RemovesFromStore()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "To Delete" },
            new TodoTask { Id = 2, Title = "Keep" }
        });

        await repo.DeleteAsync(1);

        Assert.Null(await repo.GetByIdAsync(1));
        Assert.NotNull(await repo.GetByIdAsync(2));
        var all = (await repo.GetAllAsync()).ToList();
        Assert.Single(all);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentTask_ReturnsFalse()
    {
        var repo = CreateEmptyRepo();
        var result = await repo.DeleteAsync(999);
        Assert.False(result);
    }

    // --- ReplaceAllAsync ---

    [Fact]
    public async Task ReplaceAllAsync_ReplacesAllTasks()
    {
        var repo = CreateRepoWithTasks(new List<TodoTask>
        {
            new TodoTask { Id = 1, Title = "Old" }
        });

        await repo.ReplaceAllAsync(new List<TodoTask>
        {
            new TodoTask { Id = 10, Title = "New A" },
            new TodoTask { Id = 11, Title = "New B" }
        });

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
        Assert.Null(await repo.GetByIdAsync(1));
        Assert.NotNull(await repo.GetByIdAsync(10));
    }

    [Fact]
    public async Task ReplaceAllAsync_EmptyList_ClearsStore()
    {
        var repo = new InMemoryTodoRepository(); // has seed data
        await repo.ReplaceAllAsync(new List<TodoTask>());
        var all = (await repo.GetAllAsync()).ToList();
        Assert.Empty(all);
    }

    [Fact]
    public async Task ReplaceAllAsync_SetsNextIdCorrectly()
    {
        var repo = CreateEmptyRepo();

        await repo.ReplaceAllAsync(new List<TodoTask>
        {
            new TodoTask { Id = 50, Title = "High ID" }
        });

        var added = await repo.AddAsync(new TodoTask { Title = "After Replace" });
        Assert.Equal(51, added.Id);
    }

    [Fact]
    public async Task ReplaceAllAsync_EmptyThenAdd_StartsFromId1()
    {
        var repo = CreateEmptyRepo();
        await repo.ReplaceAllAsync(new List<TodoTask>());

        var added = await repo.AddAsync(new TodoTask { Title = "First" });
        Assert.Equal(1, added.Id);
    }
}

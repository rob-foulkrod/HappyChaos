using HappyChaos.Todo.Models;

namespace HappyChaos.Todo.Services;

public interface ITodoRepository
{
    Task<IEnumerable<TodoTask>> GetAllAsync();
    Task<IEnumerable<TodoTask>> GetByStatusAsync(Models.TaskStatus status);
    Task<TodoTask?> GetByIdAsync(int id);
    Task<TodoTask> AddAsync(TodoTask task);
    Task<bool> UpdateAsync(TodoTask task);
    Task<bool> DeleteAsync(int id);
    Task ReplaceAllAsync(IEnumerable<TodoTask> tasks);
}

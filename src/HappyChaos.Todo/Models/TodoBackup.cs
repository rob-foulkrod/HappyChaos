namespace HappyChaos.Todo.Models;

public class TodoBackup
{
    public List<TodoTask> Tasks { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}

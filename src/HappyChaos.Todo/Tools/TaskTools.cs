using System.ComponentModel;
using HappyChaos.Todo.Models;
using HappyChaos.Todo.Services;
using ModelContextProtocol.Server;

namespace HappyChaos.Todo.Tools;

[McpServerToolType]
public class TaskTools
{
    [McpServerTool, Description("Get all tasks. Optionally filter by status (NotStarted, InProgress, OnHold, Completed).")]
    public static async Task<string> GetTasks(
        TodoService todoService,
        [Description("Optional status filter: NotStarted, InProgress, OnHold, or Completed")] string? status = null)
    {
        IEnumerable<TodoTask> tasks;

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Models.TaskStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            tasks = await todoService.GetByStatusAsync(parsedStatus);
        }
        else
        {
            tasks = await todoService.GetAllAsync();
        }

        var taskList = tasks.ToList();
        if (taskList.Count == 0)
        {
            return "No tasks found.";
        }

        return System.Text.Json.JsonSerializer.Serialize(taskList, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [McpServerTool, Description("Get a specific task by its ID.")]
    public static async Task<string> GetTaskById(
        TodoService todoService,
        [Description("The ID of the task to retrieve")] int id)
    {
        var task = await todoService.GetByIdAsync(id);
        if (task is null)
        {
            return $"Task with ID {id} not found.";
        }

        return System.Text.Json.JsonSerializer.Serialize(task, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [McpServerTool, Description("Add a new task. Returns the created task with its assigned ID.")]
    public static async Task<string> AddTask(
        TodoService todoService,
        [Description("Title of the task (required, 2-200 characters)")] string title,
        [Description("Description of the task (optional, max 1000 characters)")] string? description = null,
        [Description("Priority: Low, Medium, High, or Critical (default: Medium)")] string? priority = null,
        [Description("Status: NotStarted, InProgress, OnHold, or Completed (default: NotStarted)")] string? status = null,
        [Description("Due date in ISO 8601 format, e.g. 2025-12-31 (optional)")] string? dueDate = null,
        [Description("Person assigned to the task (optional, max 100 characters)")] string? assignedTo = null,
        [Description("Category of the task (optional, max 100 characters)")] string? category = null)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 2 || title.Length > 200)
        {
            return "Error: Title is required and must be between 2 and 200 characters.";
        }

        var task = new TodoTask
        {
            Title = title,
            Description = description,
            AssignedTo = assignedTo,
            Category = category
        };

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsedPriority))
        {
            task.Priority = parsedPriority;
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Models.TaskStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            task.Status = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(dueDate) && DateTime.TryParse(dueDate, out var parsedDate))
        {
            task.DueDate = parsedDate;
        }

        var created = await todoService.AddAsync(task);

        return System.Text.Json.JsonSerializer.Serialize(created, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [McpServerTool, Description("Edit an existing task by ID. Only provided fields will be updated.")]
    public static async Task<string> EditTask(
        TodoService todoService,
        [Description("The ID of the task to edit")] int id,
        [Description("New title (optional, 2-200 characters)")] string? title = null,
        [Description("New description (optional, max 1000 characters)")] string? description = null,
        [Description("New priority: Low, Medium, High, or Critical (optional)")] string? priority = null,
        [Description("New status: NotStarted, InProgress, OnHold, or Completed (optional)")] string? status = null,
        [Description("New due date in ISO 8601 format, e.g. 2025-12-31 (optional)")] string? dueDate = null,
        [Description("New assignee (optional, max 100 characters)")] string? assignedTo = null,
        [Description("New category (optional, max 100 characters)")] string? category = null)
    {
        var existing = await todoService.GetByIdAsync(id);
        if (existing is null)
        {
            return $"Task with ID {id} not found.";
        }

        if (title is not null)
        {
            if (title.Length < 2 || title.Length > 200)
            {
                return "Error: Title must be between 2 and 200 characters.";
            }
            existing.Title = title;
        }

        if (description is not null)
        {
            existing.Description = description;
        }

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsedPriority))
        {
            existing.Priority = parsedPriority;
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Models.TaskStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            existing.Status = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(dueDate) && DateTime.TryParse(dueDate, out var parsedDate))
        {
            existing.DueDate = parsedDate;
        }

        if (assignedTo is not null)
        {
            existing.AssignedTo = assignedTo;
        }

        if (category is not null)
        {
            existing.Category = category;
        }

        var updated = await todoService.UpdateAsync(existing);
        if (!updated)
        {
            return $"Failed to update task with ID {id}.";
        }

        var refreshed = await todoService.GetByIdAsync(id);
        return System.Text.Json.JsonSerializer.Serialize(refreshed, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [McpServerTool, Description("Delete a task by its ID.")]
    public static async Task<string> DeleteTask(
        TodoService todoService,
        [Description("The ID of the task to delete")] int id)
    {
        var deleted = await todoService.DeleteAsync(id);
        return deleted
            ? $"Task with ID {id} has been deleted."
            : $"Task with ID {id} not found.";
    }

    [McpServerTool, Description("Get a summary of all tasks including counts by status, overdue, and due soon.")]
    public static async Task<string> GetTaskSummary(TodoService todoService)
    {
        var summary = await todoService.GetSummaryAsync();
        return System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool, Description("Search tasks by keyword. Searches in title, description, and assignee fields.")]
    public static async Task<string> SearchTasks(
        TodoService todoService,
        [Description("The keyword to search for in task title, description, and assignee")] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return "Error: Search keyword is required.";
        }

        var allTasks = await todoService.GetAllAsync();
        var tasks = allTasks
            .Where(t =>
                (t.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.AssignedTo?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        if (tasks.Count == 0)
        {
            return $"No tasks found matching '{keyword}'.";
        }

        return System.Text.Json.JsonSerializer.Serialize(tasks, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [McpServerTool, Description("Export all tasks as a backup. Returns a JSON backup containing all tasks and their distinct categories.")]
    public static async Task<string> ExportBackup(TodoService todoService)
    {
        var backup = await todoService.ExportAsync();
        return System.Text.Json.JsonSerializer.Serialize(backup, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [McpServerTool, Description("Restore tasks from a backup. Replaces all existing tasks with the provided backup data. The backup should be a JSON string previously obtained from ExportBackup.")]
    public static async Task<string> RestoreBackup(
        TodoService todoService,
        [Description("JSON string of the backup data (as returned by ExportBackup)")] string backupJson)
    {
        if (string.IsNullOrWhiteSpace(backupJson))
        {
            return "Error: Backup JSON data is required.";
        }

        TodoBackup? backup;
        try
        {
            backup = System.Text.Json.JsonSerializer.Deserialize<TodoBackup>(backupJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
        }
        catch (System.Text.Json.JsonException ex)
        {
            return $"Error: Invalid backup JSON format. {ex.Message}";
        }

        if (backup is null)
        {
            return "Error: Backup data could not be parsed.";
        }

        await todoService.ImportAsync(backup);
        return $"Restore completed successfully. {backup.Tasks.Count} task(s) restored.";
    }
}

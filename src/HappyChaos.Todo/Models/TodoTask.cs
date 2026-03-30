using System.ComponentModel.DataAnnotations;

namespace HappyChaos.Todo.Models;

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TaskStatus
{
    [Display(Name = "Not Started")]
    NotStarted = 1,
    [Display(Name = "In Progress")]
    InProgress = 2,
    [Display(Name = "On Hold")]
    OnHold = 3,
    [Display(Name = "Completed")]
    Completed = 4
}

public class TodoTask
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Priority is required")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Required(ErrorMessage = "Status is required")]
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime? DueDate { get; set; }

    [StringLength(100, ErrorMessage = "Assignee name cannot exceed 100 characters")]
    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    [Display(Name = "Created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date && Status != TaskStatus.Completed;
    public bool IsDueSoon => DueDate.HasValue && DueDate.Value.Date <= DateTime.UtcNow.Date.AddDays(3) && DueDate.Value.Date >= DateTime.UtcNow.Date && Status != TaskStatus.Completed;
}

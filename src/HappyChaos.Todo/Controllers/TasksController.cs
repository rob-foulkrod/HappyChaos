using HappyChaos.Todo.Models;
using HappyChaos.Todo.Services;
using Microsoft.AspNetCore.Mvc;

namespace HappyChaos.Todo.Controllers;

public class TasksController : Controller
{
    private readonly TodoService _todoService;
    private readonly IConfiguration _configuration;

    public TasksController(TodoService todoService, IConfiguration configuration)
    {
        _todoService = todoService;
        _configuration = configuration;
    }

    // GET: Tasks - Current Project (all tasks dashboard)
    public async Task<IActionResult> Index(string? filter, string? category, string? search)
    {
        var tasks = (await _todoService.GetAllAsync()).AsEnumerable();

        // Apply filter
        if (!string.IsNullOrEmpty(filter))
        {
            switch (filter.ToLower())
            {
                case "active":
                    tasks = tasks.Where(t => t.Status != Models.TaskStatus.Completed);
                    break;
                case "completed":
                    tasks = tasks.Where(t => t.Status == Models.TaskStatus.Completed);
                    break;
                case "overdue":
                    tasks = tasks.Where(t => t.IsOverdue);
                    break;
                case "high":
                    tasks = tasks.Where(t => t.Priority == TaskPriority.High || t.Priority == TaskPriority.Critical);
                    break;
            }
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
            tasks = tasks.Where(t => t.Category == category);
        }

        // Apply search
        if (!string.IsNullOrEmpty(search))
        {
            tasks = tasks.Where(t =>
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (t.Description != null && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (t.AssignedTo != null && t.AssignedTo.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        // Sort: overdue first, then by priority desc, then by due date
        tasks = tasks.OrderBy(t => t.Status == Models.TaskStatus.Completed)
                     .ThenByDescending(t => t.IsOverdue)
                     .ThenByDescending(t => (int)t.Priority)
                     .ThenBy(t => t.DueDate ?? DateTime.MaxValue);

        var allTasks = await _todoService.GetAllAsync();
        var categories = allTasks.Where(t => !string.IsNullOrEmpty(t.Category))
                                  .Select(t => t.Category!)
                                  .Distinct()
                                  .OrderBy(c => c)
                                  .ToList();

        ViewBag.Summary = await _todoService.GetSummaryAsync();
        ViewBag.Filter = filter;
        ViewBag.Category = category;
        ViewBag.Search = search;
        ViewBag.Categories = categories;
        ViewBag.DefaultMessage = _configuration["DEFAULT_MESSAGE"];

        return View(tasks.ToList());
    }

    // GET: Tasks/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var task = await _todoService.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        return View(task);
    }

    // GET: Tasks/Create
    public IActionResult Create()
    {
        return View(new TodoTask { Priority = TaskPriority.Medium, Status = Models.TaskStatus.NotStarted });
    }

    // POST: Tasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TodoTask task)
    {
        if (ModelState.IsValid)
        {
            await _todoService.AddAsync(task);
            TempData["Success"] = $"Task \"{task.Title}\" was created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(task);
    }

    // GET: Tasks/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var task = await _todoService.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        return View(task);
    }

    // POST: Tasks/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TodoTask task)
    {
        if (id != task.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var updated = await _todoService.UpdateAsync(task);
            if (!updated)
            {
                return NotFound();
            }
            TempData["Success"] = $"Task \"{task.Title}\" was updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(task);
    }

    // GET: Tasks/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _todoService.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        return View(task);
    }

    // POST: Tasks/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await _todoService.GetByIdAsync(id);
        var title = task?.Title ?? "Task";
        var deleted = await _todoService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        TempData["Success"] = $"\"{title}\" was deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Tasks/ToggleComplete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        await _todoService.ToggleCompleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}

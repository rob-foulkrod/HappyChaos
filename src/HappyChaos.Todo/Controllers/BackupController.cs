using Microsoft.AspNetCore.Mvc;
using HappyChaos.Todo.Models;
using HappyChaos.Todo.Services;

namespace HappyChaos.Todo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BackupController : ControllerBase
{
    private readonly TodoService _todoService;

    public BackupController(TodoService todoService)
    {
        _todoService = todoService;
    }

    // GET: api/backup
    [HttpGet]
    public async Task<ActionResult<TodoBackup>> Export()
    {
        var backup = await _todoService.ExportAsync();
        return Ok(backup);
    }

    // POST: api/backup/restore
    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] TodoBackup? backup)
    {
        if (backup == null)
        {
            return BadRequest("Backup data is required.");
        }

        await _todoService.ImportAsync(backup);
        return Ok(new { message = "Restore completed successfully.", taskCount = backup.Tasks.Count });
    }
}

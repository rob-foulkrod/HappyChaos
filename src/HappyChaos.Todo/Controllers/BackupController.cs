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
    public ActionResult<TodoBackup> Export()
    {
        var backup = _todoService.Export();
        return Ok(backup);
    }

    // POST: api/backup/restore
    [HttpPost("restore")]
    public IActionResult Restore([FromBody] TodoBackup backup)
    {
        if (backup == null)
        {
            return BadRequest("Backup data is required.");
        }

        _todoService.Import(backup);
        return Ok(new { message = "Restore completed successfully.", taskCount = backup.Tasks.Count });
    }
}

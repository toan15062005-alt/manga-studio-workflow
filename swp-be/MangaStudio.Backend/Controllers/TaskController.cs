using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MangaStudio.Backend.Services.Interfaces;
using MangaStudio.Backend.Models.DTOs;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Controllers;

/// <summary>
/// Controller quản lý công việc (Tasks) giao cho trợ lý.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Guid.Empty;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// GET /api/assistants — Lấy danh sách tất cả trợ lý đang hoạt động.
    /// </summary>
    [HttpGet("assistants")]
    public async Task<IActionResult> GetAllAssistants()
    {
        try
        {
            var result = await _taskService.GetAllAssistants();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/pages/{pageId}/tasks — Lấy danh sách công việc của một trang truyện.
    /// </summary>
    [HttpGet("pages/{pageId:guid}/tasks")]
    public async Task<IActionResult> GetTasksByPage(Guid pageId)
    {
        try
        {
            var result = await _taskService.GetTasksByPage(pageId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/pages/{pageId}/tasks — Tạo công việc mới giao cho trợ lý liên kết với một trang.
    /// </summary>
    [HttpPost("pages/{pageId:guid}/tasks")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> CreateTask(Guid pageId, [FromBody] CreateTaskDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var assignerId = GetCurrentUserId();
            var result = await _taskService.CreateTask(pageId, assignerId, dto);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/tasks/my-tasks — Trợ lý xem danh sách các công việc được giao của mình.
    /// </summary>
    [HttpGet("tasks/my-tasks")]
    [Authorize(Roles = "assistant")]
    public async Task<IActionResult> GetMyTasks()
    {
        try
        {
            var assistantId = GetCurrentUserId();
            var result = await _taskService.GetMyTasks(assistantId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/tasks/{id} — Cập nhật thông tin công việc.
    /// </summary>
    [HttpPut("tasks/{id:guid}")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var mangakaId = GetCurrentUserId();
            var result = await _taskService.UpdateTask(id, mangakaId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/tasks/{id}/submit — Trợ lý nộp bài làm kèm file (upload).
    /// </summary>
    [HttpPost("tasks/{id:guid}/submit")]
    [Authorize(Roles = "assistant")]
    public async Task<IActionResult> SubmitTask(Guid id, [FromForm] string? note, IFormFile? file)
    {
        try
        {
            var assistantId = GetCurrentUserId();
            var result = await _taskService.SubmitTask(id, assistantId, note, file);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/tasks/{id}/submissions — Lấy danh sách bài nộp của một công việc.
    /// </summary>
    [HttpGet("tasks/{id:guid}/submissions")]
    public async Task<IActionResult> GetSubmissions(Guid id)
    {
        try
        {
            var result = await _taskService.GetSubmissions(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/submissions/{id}/review — Mangaka duyệt hoặc từ chối bài nộp của trợ lý.
    /// </summary>
    [HttpPut("submissions/{id:guid}/review")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> ReviewSubmission(Guid id, [FromBody] ReviewSubmissionDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var reviewerId = GetCurrentUserId();
            var result = await _taskService.ReviewSubmission(id, reviewerId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
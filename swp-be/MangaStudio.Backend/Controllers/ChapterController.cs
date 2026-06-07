using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MangaStudio.Backend.Services.Interfaces;
using MangaStudio.Backend.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Controllers;

/// <summary>
/// Controller quản lý chương truyện: chi tiết, trang, upload và nộp chương.
/// </summary>
[ApiController]
[Route("api/chapters")]
[Authorize]
public class ChapterController : ControllerBase
{
    private readonly IChapterService _chapterService;

    public ChapterController(IChapterService chapterService)
    {
        _chapterService = chapterService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Guid.Empty;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// GET /api/chapters/{id} — Xem chi tiết chương truyện.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetChapterById(Guid id)
    {
        try
        {
            var result = await _chapterService.GetChapterById(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/chapters/{id} — Cập nhật thông tin chương.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var mangakaId = GetCurrentUserId();
            var result = await _chapterService.UpdateChapter(id, mangakaId, dto);
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
    /// GET /api/chapters/{id}/pages — Lấy danh sách các trang thuộc chương truyện.
    /// </summary>
    [HttpGet("{id:guid}/pages")]
    public async Task<IActionResult> GetPages(Guid id)
    {
        try
        {
            var result = await _chapterService.GetPagesByChapter(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/chapters/{id}/upload-pages — Tải lên nhiều trang truyện.
    /// </summary>
    [HttpPost("{id:guid}/upload-pages")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> UploadPages(Guid id, List<IFormFile> files)
    {
        try
        {
            var uploadedById = GetCurrentUserId();
            var result = await _chapterService.UploadPages(id, files, uploadedById);
            return Ok(result);
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
    /// DELETE /api/chapters/pages/{pageId} — Xóa một trang truyện.
    /// </summary>
    [HttpDelete("pages/{pageId:guid}")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> DeletePage(Guid pageId)
    {
        try
        {
            var mangakaId = GetCurrentUserId();
            await _chapterService.DeletePage(pageId, mangakaId);
            return NoContent();
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
    /// POST /api/chapters/{id}/submit — Nộp chương truyện để xem xét xuất bản.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> SubmitChapter(Guid id)
    {
        try
        {
            var mangakaId = GetCurrentUserId();
            var result = await _chapterService.SubmitChapterForPublishing(id, mangakaId);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
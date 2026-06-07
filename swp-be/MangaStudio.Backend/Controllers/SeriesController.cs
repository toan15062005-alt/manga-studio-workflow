using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MangaStudio.Backend.Services.Interfaces;
using MangaStudio.Backend.Models.DTOs;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Controllers;

/// <summary>
/// Controller quản lý bộ truyện (Series) và chương (Chapter) bên trong nó.
/// </summary>
[ApiController]
[Route("api/series")]
[Authorize]
public class SeriesController : ControllerBase
{
    private readonly ISeriesService _seriesService;
    private readonly IChapterService _chapterService;

    public SeriesController(ISeriesService seriesService, IChapterService chapterService)
    {
        _seriesService = seriesService;
        _chapterService = chapterService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Guid.Empty;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// GET /api/series — Lấy danh sách bộ truyện của Mangaka đang đăng nhập.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "mangaka,tantou")]
    public async Task<IActionResult> GetMySeries()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _seriesService.GetSeriesByMangaka(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/series/{id} — Xem chi tiết bộ truyện.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSeriesById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _seriesService.GetSeriesById(id, userId);
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
    /// POST /api/series — Tạo bộ truyện mới.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> CreateSeries([FromBody] CreateSeriesDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var mangakaId = GetCurrentUserId();
            var result = await _seriesService.CreateSeries(mangakaId, dto);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/series/{id} — Cập nhật thông tin bộ truyện.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> UpdateSeries(Guid id, [FromBody] UpdateSeriesDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var mangakaId = GetCurrentUserId();
            var result = await _seriesService.UpdateSeries(id, mangakaId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/series/{id}/chapters — Lấy danh sách chương của bộ truyện.
    /// </summary>
    [HttpGet("{id:guid}/chapters")]
    public async Task<IActionResult> GetChapters(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _seriesService.GetChaptersBySeries(id, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/series/{id}/chapters — Tạo chương mới trong bộ truyện.
    /// </summary>
    [HttpPost("{id:guid}/chapters")]
    [Authorize(Roles = "mangaka")]
    public async Task<IActionResult> CreateChapter(Guid id, [FromBody] CreateChapterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var mangakaId = GetCurrentUserId();
            var result = await _chapterService.CreateChapter(id, mangakaId, dto);
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
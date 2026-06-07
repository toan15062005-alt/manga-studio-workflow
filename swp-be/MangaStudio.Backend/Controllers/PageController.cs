using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MangaStudio.Backend.Services.Interfaces;
using MangaStudio.Backend.Models.DTOs;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Controllers;

/// <summary>
/// Controller quản lý trang truyện: xem chi tiết, annotation và review.
/// </summary>
[ApiController]
[Route("api/pages")]
[Authorize]
public class PageController : ControllerBase
{
    private readonly IPageService _pageService;

    public PageController(IPageService pageService)
    {
        _pageService = pageService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Guid.Empty;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// GET /api/pages/{id} — Lấy thông tin chi tiết trang truyện.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPageById(Guid id)
    {
        try
        {
            var result = await _pageService.GetPageById(id);
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
    /// GET /api/pages/{id}/annotations — Lấy danh sách ghi chú (annotations) của trang.
    /// </summary>
    [HttpGet("{id:guid}/annotations")]
    public async Task<IActionResult> GetAnnotations(Guid id)
    {
        try
        {
            var result = await _pageService.GetAnnotations(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/pages/{id}/annotations — Tạo ghi chú (annotation) mới trên trang.
    /// </summary>
    [HttpPost("{id:guid}/annotations")]
    public async Task<IActionResult> CreateAnnotation(Guid id, [FromBody] CreateAnnotationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var userId = GetCurrentUserId();
            var result = await _pageService.CreateAnnotation(id, userId, dto);
            return StatusCode(201, result);
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
    /// PUT /api/pages/annotations/{id}/resolve — Đánh dấu ghi chú là đã giải quyết.
    /// </summary>
    [HttpPut("annotations/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveAnnotation(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _pageService.ResolveAnnotation(id, userId);
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
    /// GET /api/pages/{id}/reviews — Lấy danh sách nhận xét review của trang.
    /// </summary>
    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetPageReviews(Guid id)
    {
        try
        {
            var result = await _pageService.GetPageReviews(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/pages/{id}/reviews — Thêm nhận xét review cho trang (chỉ dành cho Tantou hoặc Mangaka).
    /// </summary>
    [HttpPost("{id:guid}/reviews")]
    [Authorize(Roles = "mangaka,tantou")]
    public async Task<IActionResult> CreatePageReview(Guid id, [FromBody] CreatePageReviewDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var reviewerId = GetCurrentUserId();
            var result = await _pageService.CreatePageReview(id, reviewerId, dto);
            return StatusCode(201, result);
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
}
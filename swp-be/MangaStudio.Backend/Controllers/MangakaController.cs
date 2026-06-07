using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MangaStudio.Backend.Services.Interfaces;
using System.Security.Claims;

namespace MangaStudio.Backend.Controllers;

/// <summary>
/// Controller xử lý các tác vụ nghiệp vụ của Mangaka (Tác giả truyện tranh).
/// </summary>
[ApiController]
[Route("api/mangaka")] 
[Authorize] // Bảo vệ toàn bộ API trong Controller này, yêu cầu phải truyền kèm JWT Token hợp lệ
public class MangakaController : ControllerBase 
{ 
    private readonly IMangakaService _mangakaService; 

    // Dependency Injection: Nhận vào Mangaka Service
    public MangakaController(IMangakaService mangakaService)
    { 
        _mangakaService = mangakaService; 
    }

    /// <summary>
    /// Lấy dữ liệu thống kê màn hình Dashboard của Mangaka (Số lượng Series và Trợ lý).
    /// </summary>
    /// <param name="mangakaId">ID của Mangaka.</param>
    /// <returns>HTTP 200 kèm DTO thống kê.</returns>
    [HttpGet("dashboard-stats/{mangakaId}")]
    public async Task<IActionResult> GetDashboardStats(Guid mangakaId)
    {
        var result = await _mangakaService.GetDashboardStats(mangakaId);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các bộ truyện (Series) của Mangaka quản lý.
    /// </summary>
    /// <param name="mangakaId">ID của Mangaka.</param>
    /// <returns>HTTP 200 kèm danh sách bộ truyện.</returns>
    [HttpGet("series")]
    public async Task<IActionResult> GetSeries(Guid mangakaId)
    { 
        var result = await _mangakaService.GetSeries(mangakaId);
        return Ok(result);
    }

    /// <summary>
    /// Tải lên hình ảnh trang truyện mới cho một Chapter cụ thể.
    /// </summary>
    /// <param name="id">ID của Chapter cần upload.</param>
    /// <param name="file">Tệp hình ảnh trang truyện tải lên.</param>
    /// <returns>HTTP 200 kèm đường dẫn ảnh đã lưu trên server.</returns>
    [HttpPost("chapters/{id}/upload-pages")]
    public async Task<IActionResult> UploadPage(Guid id, IFormFile file)
    { 
        if (file == null || file.Length == 0)
        {
            return BadRequest("File không hợp lệ hoặc trống.");
        }

        // Lấy User ID từ Claims của JWT Token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Không xác định được người dùng đăng nhập.");
        }

        var result = await _mangakaService.UploadPage(id, file, userId);
        return Ok(result);
    }
}

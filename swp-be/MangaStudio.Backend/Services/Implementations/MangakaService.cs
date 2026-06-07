using Microsoft.EntityFrameworkCore;
using MangaStudio.Backend.Data;
using MangaStudio.Backend.Models.DTOs;
using MangaStudio.Backend.Models.Entities;
using MangaStudio.Backend.Services.Interfaces;

namespace MangaStudio.Backend.Services.Implementations;

/// <summary>
/// Dịch vụ triển khai các nghiệp vụ liên quan đến vai trò Mangaka (Tác giả).
/// </summary>
public class MangakaService : IMangakaService
{
    private readonly AppDbContext _context;

    // Dependency Injection: Nhận database context của ứng dụng
    public MangakaService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy thống kê dữ liệu trang Dashboard bao gồm: Tổng số Series và tổng số trợ lý.
    /// </summary>
    /// <param name="mangakaId">ID định danh Mangaka.</param>
    /// <returns>DashboardStatsDto chứa tổng số Series và số trợ lý.</returns>
    public async Task<DashboardStatsDto> GetDashboardStats(Guid mangakaId)
    {
        // Đếm số lượng bộ truyện (Series) thuộc quyền sở hữu của Mangaka này
        int totalSeries = await _context.Series
            .CountAsync(x => x.MangakaId == mangakaId);

        // Đếm số lượng trợ lý của Mangaka (ở đây giả sử so khớp thông tin vai trò là Assistant)
        int totalAssistants = await _context.Users
            .CountAsync(x =>
                x.Role.Code == "Assistant" && // Check vai trò của User là trợ lý
                x.UserId == mangakaId         // Check liên kết tới người quản lý
            );

        return new DashboardStatsDto
        {
            TotalSeries = totalSeries,
            TotalAssistants = totalAssistants
        };
    }

    /// <summary>
    /// Lấy danh sách các bộ truyện (Series) do Mangaka này sáng tác.
    /// </summary>
    /// <param name="mangakaId">ID định danh Mangaka.</param>
    /// <returns>Danh sách bộ truyện dạng MangaSeriesDto.</returns>
    public async Task<List<MangaSeriesDto>> GetSeries(Guid mangakaId)
    {
        return await _context.Series
            .Where(x => x.MangakaId == mangakaId)
            .Select(x => new MangaSeriesDto
            {
                Id = x.SeriesId,
                Title = x.Title,
                Description = x.Synopsis, // Synopsis ánh xạ thành Description của DTO
                CoverImageUrl = x.CoverImageUrl
            })
            .ToListAsync();
    }

    /// <summary>
    /// Tải hình vẽ trang truyện lên server và lưu thông tin vào database.
    /// </summary>
    /// <param name="chapterId">ID Chapter truyện chứa trang vẽ này.</param>
    /// <param name="file">Tệp ảnh vẽ tải lên từ máy khách.</param>
    /// <returns>Đường dẫn URL của trang vẽ đã upload.</returns>
    public async Task<string> UploadPage(Guid chapterId, IFormFile file, Guid uploadedById)
    {
        // 1. Xác định thư mục lưu trữ file tải lên (Thư mục Uploads nằm tại thư mục gốc ứng dụng)
        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        // Nếu thư mục Uploads chưa tồn tại thì tạo mới
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 2. Tạo tên file ngẫu nhiên bằng Guid để tránh trùng lặp đè file cũ
        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        string filePath = Path.Combine(uploadsFolder, fileName);

        // 3. Copy file vẽ từ stream request của client vào ổ đĩa của server
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 4. Tìm số trang (PageNumber) tiếp theo của Chapter này để tránh trùng lặp
        // Bằng cách tìm PageNumber lớn nhất hiện tại rồi cộng thêm 1, nếu chưa có trang nào thì bắt đầu từ 1
        int maxPageNumber = await _context.MangaPages
            .Where(p => p.ChapterId == chapterId)
            .Select(p => (int?)p.PageNumber)
            .MaxAsync() ?? 0;
        int nextPageNumber = maxPageNumber + 1;

        // 5. Khởi tạo đối tượng thực thể MangaPage để lưu vào database
        var mangaPage = new MangaPage
        {
            PageId = Guid.NewGuid(),
            ChapterId = chapterId,
            CurrentImageUrl = "/Uploads/" + fileName, // URL tương đối phục vụ hiển thị ở frontend
            UploadedAt = DateTime.UtcNow,
            Status = "pending", // Thay đổi từ "Active" thành "pending" để thỏa mãn Check Constraint trong Database
            PageNumber = nextPageNumber, // Sử dụng số trang đã tính toán động
            UploadedById = uploadedById
        };

        // 6. Thêm thực thể vào database và lưu các thay đổi xuống CSDL
        _context.MangaPages.Add(mangaPage);
        await _context.SaveChangesAsync();

        return mangaPage.CurrentImageUrl;
    }
}
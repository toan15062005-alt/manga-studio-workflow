using MangaStudio.Backend.Models.DTOs;

namespace MangaStudio.Backend.Services.Interfaces;

/// <summary>Interface dịch vụ quản lý chương truyện.</summary>
public interface IChapterService
{
    /// <summary>Tạo chương mới trong một bộ truyện.</summary>
    Task<ChapterDto> CreateChapter(Guid seriesId, Guid mangakaId, CreateChapterDto dto);

    /// <summary>Lấy chi tiết một chương.</summary>
    Task<ChapterDto> GetChapterById(Guid chapterId);

    /// <summary>Cập nhật thông tin chương.</summary>
    Task<ChapterDto> UpdateChapter(Guid chapterId, Guid mangakaId, UpdateChapterDto dto);

    /// <summary>Lấy danh sách trang của chương.</summary>
    Task<List<PageDto>> GetPagesByChapter(Guid chapterId);

    /// <summary>Upload nhiều trang cùng lúc cho một chương.</summary>
    Task<UploadPagesResponseDto> UploadPages(Guid chapterId, List<IFormFile> files, Guid uploadedById);

    /// <summary>Xóa một trang khỏi chương.</summary>
    System.Threading.Tasks.Task DeletePage(Guid pageId, Guid mangakaId);

    /// <summary>Nộp chương để xem xét xuất bản.</summary>
    Task<ChapterDto> SubmitChapterForPublishing(Guid chapterId, Guid mangakaId);
}

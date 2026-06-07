using MangaStudio.Backend.Models.DTOs;

namespace MangaStudio.Backend.Services.Interfaces;

/// <summary>Interface dịch vụ quản lý annotation và review trang.</summary>
public interface IPageService
{
    /// <summary>Lấy chi tiết thông tin trang.</summary>
    Task<PageDto> GetPageById(Guid pageId);

    /// <summary>Lấy danh sách annotation của trang.</summary>
    Task<List<AnnotationDto>> GetAnnotations(Guid pageId);

    /// <summary>Tạo annotation mới trên trang.</summary>
    Task<AnnotationDto> CreateAnnotation(Guid pageId, Guid createdById, CreateAnnotationDto dto);

    /// <summary>Đánh dấu annotation là đã giải quyết.</summary>
    Task<AnnotationDto> ResolveAnnotation(Guid annotationId, Guid userId);

    /// <summary>Lấy danh sách review của trang.</summary>
    Task<List<PageReviewDto>> GetPageReviews(Guid pageId);

    /// <summary>Tạo nhận xét review cho trang (Tantou/Mangaka).</summary>
    Task<PageReviewDto> CreatePageReview(Guid pageId, Guid reviewerId, CreatePageReviewDto dto);
}

namespace MangaStudio.Backend.Models.DTOs;

/// <summary>DTO hiển thị thông tin trang truyện.</summary>
public class PageDto
{
    public Guid PageId { get; set; }
    public Guid ChapterId { get; set; }
    public int PageNumber { get; set; }
    public string? CurrentImageUrl { get; set; }
    public string Status { get; set; } = null!;
    public Guid? UploadedById { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public int TaskCount { get; set; }
    public int AnnotationCount { get; set; }
}

/// <summary>DTO kết quả upload nhiều trang cùng lúc.</summary>
public class UploadPagesResponseDto
{
    public int TotalUploaded { get; set; }
    public List<PageUploadResultDto> Pages { get; set; } = new();
}

/// <summary>DTO kết quả từng trang được upload.</summary>
public class PageUploadResultDto
{
    public Guid PageId { get; set; }
    public int PageNumber { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public string Status { get; set; } = null!;
}

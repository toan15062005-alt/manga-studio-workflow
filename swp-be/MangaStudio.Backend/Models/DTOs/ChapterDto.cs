using System.ComponentModel.DataAnnotations;

namespace MangaStudio.Backend.Models.DTOs;

/// <summary>DTO trả về thông tin chương truyện.</summary>
public class ChapterDto
{
    public Guid ChapterId { get; set; }
    public Guid SeriesId { get; set; }
    public string SeriesTitle { get; set; } = null!;
    public int ChapterNumber { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public DateTime? SubmittedForPublishingAt { get; set; }
    public int PageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>DTO tạo chương mới.</summary>
public class CreateChapterDto
{
    [Required(ErrorMessage = "Số chương là bắt buộc.")]
    [Range(1, 9999, ErrorMessage = "Số chương phải từ 1 đến 9999.")]
    public int ChapterNumber { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    public DateTime? DueDate { get; set; }
}

/// <summary>DTO cập nhật thông tin chương.</summary>
public class UpdateChapterDto
{
    [StringLength(255)]
    public string? Title { get; set; }

    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
}

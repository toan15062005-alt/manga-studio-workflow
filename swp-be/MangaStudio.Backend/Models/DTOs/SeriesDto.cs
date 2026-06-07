using System.ComponentModel.DataAnnotations;

namespace MangaStudio.Backend.Models.DTOs;

/// <summary>DTO trả về thông tin bộ truyện.</summary>
public class SeriesDto
{
    public Guid SeriesId { get; set; }
    public string Title { get; set; } = null!;
    public string? TitleJp { get; set; }
    public string? Synopsis { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = null!;
    public Guid MangakaId { get; set; }
    public string MangakaName { get; set; } = null!;
    public Guid? TantouId { get; set; }
    public string? TantouName { get; set; }
    public int? Ranking { get; set; }
    public int ReaderCount { get; set; }
    public decimal? Rating { get; set; }
    public List<string> Genres { get; set; } = new();
    public int ChapterCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>DTO tạo bộ truyện mới.</summary>
public class CreateSeriesDto
{
    [Required(ErrorMessage = "Tên bộ truyện là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự.")]
    public string Title { get; set; } = null!;

    [StringLength(255)]
    public string? TitleJp { get; set; }

    public string? Synopsis { get; set; }

    public List<string> Genres { get; set; } = new();
}

/// <summary>DTO cập nhật bộ truyện.</summary>
public class UpdateSeriesDto
{
    [StringLength(255)]
    public string? Title { get; set; }

    [StringLength(255)]
    public string? TitleJp { get; set; }

    public string? Synopsis { get; set; }
    public string? Status { get; set; }
    public List<string>? Genres { get; set; }
}

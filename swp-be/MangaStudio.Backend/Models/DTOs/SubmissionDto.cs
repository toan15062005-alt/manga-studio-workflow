using System.ComponentModel.DataAnnotations;

namespace MangaStudio.Backend.Models.DTOs;

/// <summary>DTO hiển thị thông tin bài nộp của trợ lý.</summary>
public class TaskSubmissionDto
{
    public Guid SubmissionId { get; set; }
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public Guid SubmittedById { get; set; }
    public string SubmittedByName { get; set; } = null!;
    public Guid? PageVersionId { get; set; }
    public string? FileUrl { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
}

/// <summary>DTO trợ lý nộp bài (có thể kèm file).</summary>
public class SubmitTaskDto
{
    public string? Note { get; set; }
    // File sẽ được nhận qua IFormFile trong controller
}

/// <summary>DTO Mangaka duyệt bài nộp của trợ lý.</summary>
public class ReviewSubmissionDto
{
    [Required]
    [RegularExpression("^(approved|rejected)$", ErrorMessage = "Decision chỉ được là 'approved' hoặc 'rejected'.")]
    public string Decision { get; set; } = null!;

    public string? Note { get; set; }
}

/// <summary>DTO hiển thị phiên bản trang (PageVersion).</summary>
public class PageVersionDto
{
    public Guid PageVersionId { get; set; }
    public Guid PageId { get; set; }
    public int VersionNumber { get; set; }
    public string FileUrl { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long? FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public Guid UploadedById { get; set; }
    public string UploadedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace MangaStudio.Backend.Models.DTOs;

/// <summary>DTO hiển thị thông tin công việc (task).</summary>
public class TaskDto
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
    public Guid PageId { get; set; }
    public int PageNumber { get; set; }
    public string? ChapterTitle { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public Guid AssignerId { get; set; }
    public string AssignerName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly? DueDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>DTO tạo công việc mới giao cho trợ lý.</summary>
public class CreateTaskDto
{
    [Required(ErrorMessage = "Tiêu đề công việc là bắt buộc.")]
    [MaxLength(255)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Loại công việc là bắt buộc.")]
    [RegularExpression("^(line_art|background|effects|coloring|lettering|review)$",
        ErrorMessage = "Type không hợp lệ. Chấp nhận: line_art, background, effects, coloring, lettering, review.")]
    public string Type { get; set; } = null!;

    public Guid? RegionId { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateOnly? DueDate { get; set; }

    [Range(0, 999999999, ErrorMessage = "Số tiền thanh toán phải >= 0.")]
    public decimal PaymentAmount { get; set; }
}

/// <summary>DTO cập nhật công việc.</summary>
public class UpdateTaskDto
{
    [MaxLength(255)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [RegularExpression("^(line_art|background|effects|coloring|lettering|review)$",
        ErrorMessage = "Type không hợp lệ. Chấp nhận: line_art, background, effects, coloring, lettering, review.")]
    public string? Type { get; set; }

    public Guid? RegionId { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateOnly? DueDate { get; set; }

    [Range(0, 999999999, ErrorMessage = "Số tiền thanh toán phải >= 0.")]
    public decimal? PaymentAmount { get; set; }

    [RegularExpression("^(pending|in_progress|submitted|revision|approved|cancelled)$",
        ErrorMessage = "Status không hợp lệ.")]
    public string? Status { get; set; }
}

/// <summary>DTO hiển thị thông tin trợ lý.</summary>
public class AssistantDto
{
    public Guid AssistantId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Specialty { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Rating { get; set; }
}
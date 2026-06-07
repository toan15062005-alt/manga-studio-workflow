using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaStudio.Backend.Models.DTOs;

/// <summary>DTO hiển thị đề xuất bộ truyện.</summary>
public class ProposalDto
{
    public Guid ProposalId { get; set; }
    public Guid SeriesId { get; set; }
    public string SeriesTitle { get; set; } = null!;
    public string? SeriesSynopsis { get; set; }
    public List<string> SeriesGenres { get; set; } = new();
    public Guid SubmittedById { get; set; }
    public string SubmittedByName { get; set; } = null!;
    public Guid? ReviewedById { get; set; }
    public string? ReviewedByName { get; set; }
    public string Status { get; set; } = null!;
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>DTO duyệt hoặc từ chối đề xuất bộ truyện.</summary>
public class ReviewProposalDto
{
    [Required(ErrorMessage = "Quyết định duyệt là bắt buộc.")]
    [RegularExpression("^(approved|rejected)$", ErrorMessage = "Decision chỉ được là 'approved' hoặc 'rejected'.")]
    public string Decision { get; set; } = null!;

    public string? Feedback { get; set; }
}

/// <summary>DTO hiển thị lịch xuất bản.</summary>
public class PublishScheduleDto
{
    public Guid ScheduleId { get; set; }
    public Guid ChapterId { get; set; }
    public int ChapterNumber { get; set; }
    public string? ChapterTitle { get; set; }
    public string SeriesTitle { get; set; } = null!;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = null!;
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>DTO tạo lịch xuất bản.</summary>
public class CreatePublishScheduleDto
{
    [Required(ErrorMessage = "Ngày lên lịch là bắt buộc.")]
    public DateTime ScheduledDate { get; set; }
}

/// <summary>DTO hiển thị thông tin bảng lương trợ lý.</summary>
public class PayrollDto
{
    public Guid PayrollRecordId { get; set; }
    public Guid AssistantId { get; set; }
    public string AssistantName { get; set; } = null!;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime CreatedAt { get; set; }
}
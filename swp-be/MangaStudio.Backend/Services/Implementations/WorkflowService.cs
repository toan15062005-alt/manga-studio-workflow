using Microsoft.EntityFrameworkCore;
using MangaStudio.Backend.Data;
using MangaStudio.Backend.Models.DTOs;
using MangaStudio.Backend.Models.Entities;
using MangaStudio.Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Services.Implementations;

/// <summary>
/// Triển khai nghiệp vụ quản lý đề xuất series, lịch xuất bản và bảng lương.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _context;

    public WorkflowService(AppDbContext context)
    {
        _context = context;
    }

    // === Series Proposals ===

    /// <summary>
    /// Lấy danh sách đề xuất chờ duyệt (status = 'submitted').
    /// </summary>
    public async Task<List<ProposalDto>> GetPendingProposals()
    {
        return await _context.SeriesProposals
            .Where(p => p.Status == "submitted")
            .Include(p => p.Series)
                .ThenInclude(s => s.SeriesGenres)
            .Include(p => p.SubmittedBy)
            .Include(p => p.ReviewedBy)
            .OrderByDescending(p => p.SubmittedAt)
            .Select(p => MapProposalToDto(p))
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách đề xuất của một Mangaka.
    /// </summary>
    public async Task<List<ProposalDto>> GetProposalsByMangaka(Guid mangakaId)
    {
        return await _context.SeriesProposals
            .Where(p => p.SubmittedById == mangakaId)
            .Include(p => p.Series)
                .ThenInclude(s => s.SeriesGenres)
            .Include(p => p.SubmittedBy)
            .Include(p => p.ReviewedBy)
            .OrderByDescending(p => p.SubmittedAt)
            .Select(p => MapProposalToDto(p))
            .ToListAsync();
    }

    /// <summary>
    /// Tantou phê duyệt hoặc từ chối đề xuất series.
    /// Business Rule:
    /// - Nếu approved -> Series.Status = 'active' và gán TantouId cho Series.
    /// - Cập nhật thông tin người duyệt, thời gian duyệt và feedback.
    /// </summary>
    public async Task<ProposalDto> ReviewProposal(Guid proposalId, Guid tantouId, ReviewProposalDto dto)
    {
        var proposal = await _context.SeriesProposals
            .Include(p => p.Series)
                .ThenInclude(s => s.SeriesGenres)
            .Include(p => p.SubmittedBy)
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId)
            ?? throw new KeyNotFoundException($"Không tìm thấy đề xuất với ID {proposalId}");

        var decision = dto.Decision.ToLower();
        proposal.Status = decision;
        proposal.ReviewedById = tantouId;
        proposal.ReviewedAt = DateTime.UtcNow;
        proposal.ReviewNote = dto.Feedback;

        if (decision == "approved")
        {
            proposal.Series.Status = "active"; // BR-Proposal
            proposal.Series.TantouId = tantouId; // Assign Tantou to Series
        }
        else if (decision == "rejected")
        {
            proposal.Series.Status = "proposal"; // Revert/Keep proposal
        }

        proposal.Series.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Load reviewer details
        var reviewedBy = await _context.Users.FindAsync(tantouId);
        proposal.ReviewedBy = reviewedBy;

        return MapProposalToDto(proposal);
    }

    // === Publish Schedule ===

    /// <summary>
    /// Lấy tất cả lịch xuất bản (có thể lọc theo series).
    /// </summary>
    public async Task<List<PublishScheduleDto>> GetPublishSchedules(Guid? seriesId = null)
    {
        var query = _context.PublishSchedules
            .Include(s => s.ApprovedBy)
            .Include(s => s.Chapter)
                .ThenInclude(c => c.Series)
            .AsQueryable();

        if (seriesId.HasValue)
        {
            query = query.Where(s => s.Chapter.SeriesId == seriesId.Value);
        }

        return await query
            .OrderBy(s => s.ScheduledDate)
            .Select(s => MapPublishScheduleToDto(s))
            .ToListAsync();
    }

    /// <summary>
    /// Tạo lịch xuất bản cho một chương truyện.
    /// </summary>
    public async Task<PublishScheduleDto> CreatePublishSchedule(Guid chapterId, Guid createdById, CreatePublishScheduleDto dto)
    {
        var chapter = await _context.Chapters
            .Include(c => c.Series)
            .FirstOrDefaultAsync(c => c.ChapterId == chapterId)
            ?? throw new KeyNotFoundException($"Không tìm thấy chương với ID {chapterId}");

        // Tạo lịch xuất bản với status mặc định là 'scheduled'
        var schedule = new PublishSchedule
        {
            PublishScheduleId = Guid.NewGuid(),
            ChapterId = chapterId,
            ScheduledDate = dto.ScheduledDate,
            Status = "scheduled",
            CreatedAt = DateTime.UtcNow
        };

        _context.PublishSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return await GetPublishScheduleById(schedule.PublishScheduleId);
    }

    /// <summary>
    /// Tantou phê duyệt lịch xuất bản.
    /// </summary>
    public async Task<PublishScheduleDto> ApprovePublishSchedule(Guid scheduleId, Guid tantouId)
    {
        var schedule = await _context.PublishSchedules
            .Include(s => s.Chapter)
            .FirstOrDefaultAsync(s => s.PublishScheduleId == scheduleId)
            ?? throw new KeyNotFoundException($"Không tìm thấy lịch xuất bản với ID {scheduleId}");

        schedule.ApprovedById = tantouId;
        // status is either kept as scheduled or approved in terms of business. Since DB only accepts 'scheduled','published','cancelled', we keep it as 'scheduled' or we can mark it 'published' if appropriate, but generally it remains 'scheduled' and ApprovedById != null represents approval.
        
        await _context.SaveChangesAsync();

        return await GetPublishScheduleById(schedule.PublishScheduleId);
    }

    // === Payroll ===

    /// <summary>
    /// Lấy danh sách bảng lương trợ lý. Mangaka xem được tất cả, trợ lý xem của chính mình.
    /// </summary>
    public async Task<List<PayrollDto>> GetPayrollRecords(Guid? assistantId = null)
    {
        var query = _context.PayrollRecords
            .Include(p => p.Assistant)
            .AsQueryable();

        if (assistantId.HasValue)
        {
            query = query.Where(p => p.AssistantId == assistantId.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PayrollDto
            {
                PayrollRecordId = p.PayrollRecordId,
                AssistantId = p.AssistantId,
                AssistantName = p.Assistant.FullName,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                TotalHours = 0,
                TotalAmount = p.TotalAmount ?? (p.BaseAmount + p.BonusAmount - p.DeductionAmount),
                Status = p.Status,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Mangaka đánh dấu đã thanh toán lương cho trợ lý.
    /// Business Rule: Chỉ thanh toán khi status là 'pending'.
    /// </summary>
    public async Task<PayrollDto> MarkPayrollAsPaid(Guid payrollRecordId, Guid mangakaId)
    {
        var record = await _context.PayrollRecords
            .Include(p => p.Assistant)
            .FirstOrDefaultAsync(p => p.PayrollRecordId == payrollRecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy bản ghi lương với ID {payrollRecordId}");

        if (record.Status != "pending")
        {
            throw new InvalidOperationException("Chỉ được thanh toán các bản ghi lương đang ở trạng thái 'pending'.");
        }

        record.Status = "paid";
        record.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new PayrollDto
        {
            PayrollRecordId = record.PayrollRecordId,
            AssistantId = record.AssistantId,
            AssistantName = record.Assistant.FullName,
            PeriodStart = record.PeriodStart,
            PeriodEnd = record.PeriodEnd,
            TotalHours = 0,
            TotalAmount = record.TotalAmount ?? (record.BaseAmount + record.BonusAmount - record.DeductionAmount),
            Status = record.Status,
            PaidAt = record.PaidAt,
            CreatedAt = record.CreatedAt
        };
    }

    private async Task<PublishScheduleDto> GetPublishScheduleById(Guid scheduleId)
    {
        var s = await _context.PublishSchedules
            .Include(s => s.ApprovedBy)
            .Include(s => s.Chapter)
                .ThenInclude(c => c.Series)
            .FirstOrDefaultAsync(s => s.PublishScheduleId == scheduleId)
            ?? throw new KeyNotFoundException();
        return MapPublishScheduleToDto(s);
    }

    private static ProposalDto MapProposalToDto(SeriesProposal p)
    {
        return new ProposalDto
        {
            ProposalId = p.ProposalId,
            SeriesId = p.SeriesId,
            SeriesTitle = p.Series?.Title ?? "Unknown",
            SeriesSynopsis = p.Series?.Synopsis,
            SeriesGenres = p.Series?.SeriesGenres?.Select(g => g.Genre).ToList() ?? new List<string>(),
            SubmittedById = p.SubmittedById,
            SubmittedByName = p.SubmittedBy?.FullName ?? "Unknown",
            ReviewedById = p.ReviewedById,
            ReviewedByName = p.ReviewedBy?.FullName,
            Status = p.Status,
            Feedback = p.ReviewNote, // mapped from ReviewNote in DB
            SubmittedAt = p.SubmittedAt,
            ReviewedAt = p.ReviewedAt
        };
    }

    private static PublishScheduleDto MapPublishScheduleToDto(PublishSchedule s)
    {
        return new PublishScheduleDto
        {
            ScheduleId = s.PublishScheduleId,
            ChapterId = s.ChapterId,
            ChapterNumber = s.Chapter?.ChapterNumber ?? 0,
            ChapterTitle = s.Chapter?.Title,
            SeriesTitle = s.Chapter?.Series?.Title ?? "Unknown",
            ScheduledDate = s.ScheduledDate,
            Status = s.Status,
            ApprovedById = s.ApprovedById,
            ApprovedByName = s.ApprovedBy?.FullName,
            PublishedAt = s.PublishedAt,
            CreatedAt = s.CreatedAt
        };
    }
}
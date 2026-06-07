using MangaStudio.Backend.Models.DTOs;

namespace MangaStudio.Backend.Services.Interfaces;

/// <summary>Interface dịch vụ quản lý đề xuất bộ truyện và lịch xuất bản.</summary>
public interface IWorkflowService
{
    // === Series Proposals ===

    /// <summary>Lấy danh sách đề xuất dành cho Tantou duyệt.</summary>
    Task<List<ProposalDto>> GetPendingProposals();

    /// <summary>Lấy đề xuất của một Mangaka cụ thể.</summary>
    Task<List<ProposalDto>> GetProposalsByMangaka(Guid mangakaId);

    /// <summary>Tantou duyệt hoặc từ chối đề xuất series.</summary>
    Task<ProposalDto> ReviewProposal(Guid proposalId, Guid tantouId, ReviewProposalDto dto);

    // === Publish Schedule ===

    /// <summary>Lấy tất cả lịch xuất bản (có thể lọc theo ngày).</summary>
    Task<List<PublishScheduleDto>> GetPublishSchedules(Guid? seriesId = null);

    /// <summary>Tạo lịch xuất bản cho chương.</summary>
    Task<PublishScheduleDto> CreatePublishSchedule(Guid chapterId, Guid createdById, CreatePublishScheduleDto dto);

    /// <summary>Tantou phê duyệt lịch xuất bản.</summary>
    Task<PublishScheduleDto> ApprovePublishSchedule(Guid scheduleId, Guid tantouId);

    // === Payroll ===

    /// <summary>Lấy danh sách bản ghi lương của trợ lý (Mangaka xem).</summary>
    Task<List<PayrollDto>> GetPayrollRecords(Guid? assistantId = null);

    /// <summary>Đánh dấu đã thanh toán lương cho trợ lý.</summary>
    Task<PayrollDto> MarkPayrollAsPaid(Guid payrollRecordId, Guid mangakaId);
}

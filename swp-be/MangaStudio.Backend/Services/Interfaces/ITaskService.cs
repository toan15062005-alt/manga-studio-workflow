using MangaStudio.Backend.Models.DTOs;

namespace MangaStudio.Backend.Services.Interfaces;

/// <summary>Interface dịch vụ quản lý công việc (Tasks) giao cho trợ lý.</summary>
public interface ITaskService
{
    /// <summary>Tạo công việc mới giao cho trợ lý, liên kết với một trang cụ thể.</summary>
    Task<TaskDto> CreateTask(Guid pageId, Guid assignerId, CreateTaskDto dto);

    /// <summary>Lấy danh sách công việc của một trang.</summary>
    Task<List<TaskDto>> GetTasksByPage(Guid pageId);

    /// <summary>Lấy danh sách công việc của trợ lý đang đăng nhập.</summary>
    Task<List<TaskDto>> GetMyTasks(Guid assistantId);

    /// <summary>Cập nhật thông tin công việc.</summary>
    Task<TaskDto> UpdateTask(Guid taskId, Guid mangakaId, UpdateTaskDto dto);

    /// <summary>Trợ lý nộp bài làm kèm file đính kèm (PageVersion).</summary>
    Task<TaskSubmissionDto> SubmitTask(Guid taskId, Guid assistantId, string? note, IFormFile? file);

    /// <summary>Lấy danh sách bài nộp của một công việc.</summary>
    Task<List<TaskSubmissionDto>> GetSubmissions(Guid taskId);

    /// <summary>Mangaka duyệt hoặc từ chối bài nộp của trợ lý.</summary>
    Task<TaskSubmissionDto> ReviewSubmission(Guid submissionId, Guid reviewerId, ReviewSubmissionDto dto);

    /// <summary>Lấy danh sách tất cả trợ lý đang hoạt động.</summary>
    Task<List<AssistantDto>> GetAllAssistants();
}

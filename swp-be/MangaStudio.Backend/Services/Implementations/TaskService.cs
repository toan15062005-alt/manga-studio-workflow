using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MangaStudio.Backend.Data;
using MangaStudio.Backend.Models.DTOs;
using MangaStudio.Backend.Models.Entities;
using MangaStudio.Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Services.Implementations;

/// <summary>
/// Triển khai nghiệp vụ quản lý công việc (Tasks) giao cho trợ lý.
/// </summary>
public class TaskService : ITaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo công việc mới giao cho trợ lý.
    /// </summary>
    public async Task<TaskDto> CreateTask(Guid pageId, Guid assignerId, CreateTaskDto dto)
    {
        var page = await _context.MangaPages
            .Include(p => p.Chapter)
                .ThenInclude(c => c.Series)
            .FirstOrDefaultAsync(p => p.PageId == pageId)
            ?? throw new KeyNotFoundException($"Trang truyện với ID {pageId} không tồn tại.");

        if (page.Chapter.Series.MangakaId != assignerId)
        {
            throw new UnauthorizedAccessException("Chỉ Mangaka sở hữu bộ truyện mới có quyền giao việc.");
        }

        if (dto.AssigneeId.HasValue)
        {
            var assistant = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == dto.AssigneeId.Value);
            if (assistant == null || !string.Equals(assistant.Role.Code, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Người được giao việc phải là trợ lý (Assistant).");
            }
        }

        var task = new MangaStudio.Backend.Models.Entities.Task
        {
            TaskId = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            PageId = pageId,
            RegionId = dto.RegionId,
            AssigneeId = dto.AssigneeId,
            AssignerId = assignerId,
            Status = "pending",
            DueDate = dto.DueDate,
            PaymentAmount = dto.PaymentAmount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update page status to assigned if needed
        if (page.Status == "pending" || page.Status == "revision")
        {
            page.Status = "assigned";
        }

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return await GetTaskById(task.TaskId);
    }

    /// <summary>
    /// Lấy danh sách công việc của một trang truyện.
    /// </summary>
    public async Task<List<TaskDto>> GetTasksByPage(Guid pageId)
    {
        return await _context.Tasks
            .Where(t => t.PageId == pageId)
            .Include(t => t.Assignee)
            .Include(t => t.Assigner)
            .Include(t => t.Page)
                .ThenInclude(p => p.Chapter)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => MapTaskToDto(t))
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách công việc của trợ lý đang đăng nhập.
    /// </summary>
    public async Task<List<TaskDto>> GetMyTasks(Guid assistantId)
    {
        return await _context.Tasks
            .Where(t => t.AssigneeId == assistantId)
            .Include(t => t.Assignee)
            .Include(t => t.Assigner)
            .Include(t => t.Page)
                .ThenInclude(p => p.Chapter)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => MapTaskToDto(t))
            .ToListAsync();
    }

    /// <summary>
    /// Cập nhật thông tin công việc.
    /// </summary>
    public async Task<TaskDto> UpdateTask(Guid taskId, Guid mangakaId, UpdateTaskDto dto)
    {
        var task = await _context.Tasks
            .Include(t => t.Page)
                .ThenInclude(p => p.Chapter)
                    .ThenInclude(c => c.Series)
            .FirstOrDefaultAsync(t => t.TaskId == taskId)
            ?? throw new KeyNotFoundException($"Công việc với ID {taskId} không tồn tại.");

        if (task.Page.Chapter.Series.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa công việc này.");
        }

        if (dto.Title != null) task.Title = dto.Title;
        if (dto.Description != null) task.Description = dto.Description;
        if (dto.Type != null) task.Type = dto.Type;
        if (dto.RegionId != null) task.RegionId = dto.RegionId;
        if (dto.AssigneeId != null) task.AssigneeId = dto.AssigneeId;
        if (dto.DueDate != null) task.DueDate = dto.DueDate;
        if (dto.PaymentAmount != null) task.PaymentAmount = dto.PaymentAmount.Value;
        if (dto.Status != null) task.Status = dto.Status;

        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetTaskById(task.TaskId);
    }

    /// <summary>
    /// Trợ lý nộp bài làm kèm file đính kèm.
    /// </summary>
    public async Task<TaskSubmissionDto> SubmitTask(Guid taskId, Guid assistantId, string? note, IFormFile? file)
    {
        var task = await _context.Tasks
            .Include(t => t.Page)
            .FirstOrDefaultAsync(t => t.TaskId == taskId)
            ?? throw new KeyNotFoundException($"Công việc với ID {taskId} không tồn tại.");

        if (task.AssigneeId != assistantId)
        {
            throw new UnauthorizedAccessException("Bạn không phải người được giao việc này.");
        }

        PageVersion? pageVersion = null;
        if (file != null && file.Length > 0)
        {
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".psd", ".clip" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                throw new ArgumentException("Chỉ chấp nhận nộp file ảnh hoặc file gốc (.PNG, .JPG, .JPEG, .PSD, .CLIP).");
            }

            if (file.Length > 50 * 1024 * 1024)
            {
                throw new ArgumentException("Kích thước file tối đa 50MB.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/Uploads/{uniqueFileName}";

            var maxVer = await _context.PageVersions
                .Where(v => v.PageId == task.PageId)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync() ?? 0;

            pageVersion = new PageVersion
            {
                PageVersionId = Guid.NewGuid(),
                PageId = task.PageId,
                VersionNumber = maxVer + 1,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSizeBytes = file.Length,
                MimeType = file.ContentType,
                UploadedById = assistantId,
                CreatedAt = DateTime.UtcNow,
                Note = note ?? $"Nộp sản phẩm cho công việc: {task.Title}"
            };

            _context.PageVersions.Add(pageVersion);

            // Cập nhật CurrentImageUrl của MangaPage
            task.Page.CurrentImageUrl = fileUrl;
        }

        // Cập nhật trạng thái
        task.Status = "submitted";
        task.Page.Status = "submitted";
        task.UpdatedAt = DateTime.UtcNow;

        var submission = new TaskSubmission
        {
            SubmissionId = Guid.NewGuid(),
            TaskId = taskId,
            SubmittedById = assistantId,
            PageVersionId = pageVersion?.PageVersionId,
            Note = note,
            Status = "submitted",
            SubmittedAt = DateTime.UtcNow
        };

        _context.TaskSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        return await GetSubmissionById(submission.SubmissionId);
    }

    /// <summary>
    /// Lấy danh sách bài nộp của một công việc.
    /// </summary>
    public async Task<List<TaskSubmissionDto>> GetSubmissions(Guid taskId)
    {
        return await _context.TaskSubmissions
            .Where(s => s.TaskId == taskId)
            .Include(s => s.SubmittedBy)
            .Include(s => s.Task)
            .Include(s => s.PageVersion)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new TaskSubmissionDto
            {
                SubmissionId = s.SubmissionId,
                TaskId = s.TaskId,
                TaskTitle = s.Task.Title,
                SubmittedById = s.SubmittedById,
                SubmittedByName = s.SubmittedBy.FullName,
                PageVersionId = s.PageVersionId,
                FileUrl = s.PageVersion != null ? s.PageVersion.FileUrl : null,
                Note = s.Note,
                Status = s.Status,
                SubmittedAt = s.SubmittedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Mangaka phê duyệt hoặc từ chối bài nộp của trợ lý.
    /// </summary>
    public async Task<TaskSubmissionDto> ReviewSubmission(Guid submissionId, Guid reviewerId, ReviewSubmissionDto dto)
    {
        var submission = await _context.TaskSubmissions
            .Include(s => s.Task)
                .ThenInclude(t => t.Page)
            .Include(s => s.SubmittedBy)
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId)
            ?? throw new KeyNotFoundException($"Không tìm thấy bài nộp với ID {submissionId}");

        if (submission.Task.AssignerId != reviewerId)
        {
            throw new UnauthorizedAccessException("Bạn không phải người giao việc này nên không thể duyệt.");
        }

        var decision = dto.Decision.ToLower();
        if (decision == "approved")
        {
            submission.Status = "accepted";
            submission.Task.Status = "approved";
            submission.Task.Page.Status = "review"; // Hoàn thành task -> chờ review toàn bộ trang

            // Tạo PayrollRecord cho trợ lý
            var periodDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var payroll = new PayrollRecord
            {
                PayrollRecordId = Guid.NewGuid(),
                AssistantId = submission.SubmittedById,
                TaskId = submission.TaskId,
                PeriodStart = periodDate,
                PeriodEnd = periodDate,
                BaseAmount = submission.Task.PaymentAmount,
                BonusAmount = 0,
                DeductionAmount = 0,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.PayrollRecords.Add(payroll);
        }
        else // rejected
        {
            submission.Status = "rejected";
            submission.Task.Status = "revision";
            submission.Task.Page.Status = "revision";
        }

        submission.Task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetSubmissionById(submission.SubmissionId);
    }

    /// <summary>
    /// Lấy danh sách tất cả trợ lý đang hoạt động.
    /// </summary>
    public async Task<List<AssistantDto>> GetAllAssistants()
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.AssistantProfile)
            .Where(u => u.Role.Code.ToLower() == "assistant" && u.IsActive)
            .Select(u => new AssistantDto
            {
                AssistantId = u.UserId,
                FullName = u.FullName,
                Specialty = u.AssistantProfile != null ? u.AssistantProfile.Specialty : null,
                HourlyRate = u.AssistantProfile != null ? u.AssistantProfile.HourlyRate : 0,
                Rating = u.AssistantProfile != null ? (decimal)u.AssistantProfile.Rating : 0
            })
            .ToListAsync();
    }

    private async Task<TaskDto> GetTaskById(Guid taskId)
    {
        var t = await _context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Assigner)
            .Include(t => t.Page)
                .ThenInclude(p => p.Chapter)
            .FirstOrDefaultAsync(t => t.TaskId == taskId)
            ?? throw new KeyNotFoundException();
        return MapTaskToDto(t);
    }

    private async Task<TaskSubmissionDto> GetSubmissionById(Guid submissionId)
    {
        var s = await _context.TaskSubmissions
            .Include(s => s.SubmittedBy)
            .Include(s => s.Task)
            .Include(s => s.PageVersion)
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId)
            ?? throw new KeyNotFoundException();
        return new TaskSubmissionDto
        {
            SubmissionId = s.SubmissionId,
            TaskId = s.TaskId,
            TaskTitle = s.Task.Title,
            SubmittedById = s.SubmittedById,
            SubmittedByName = s.SubmittedBy.FullName,
            PageVersionId = s.PageVersionId,
            FileUrl = s.PageVersion != null ? s.PageVersion.FileUrl : null,
            Note = s.Note,
            Status = s.Status,
            SubmittedAt = s.SubmittedAt
        };
    }

    private static TaskDto MapTaskToDto(MangaStudio.Backend.Models.Entities.Task t)
    {
        return new TaskDto
        {
            TaskId = t.TaskId,
            Title = t.Title,
            Description = t.Description,
            Type = t.Type,
            PageId = t.PageId,
            PageNumber = t.Page != null ? t.Page.PageNumber : 0,
            ChapterTitle = t.Page?.Chapter?.Title,
            RegionId = t.RegionId,
            AssigneeId = t.AssigneeId,
            AssigneeName = t.Assignee != null ? t.Assignee.FullName : null,
            AssignerId = t.AssignerId,
            AssignerName = t.Assigner?.FullName ?? "Unknown",
            Status = t.Status,
            DueDate = t.DueDate,
            PaymentAmount = t.PaymentAmount,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
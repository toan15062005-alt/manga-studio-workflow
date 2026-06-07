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
using System.Threading;
using System.Threading.Tasks;

namespace MangaStudio.Backend.Services.Implementations;

/// <summary>
/// Triển khai các nghiệp vụ quản lý chương truyện.
/// </summary>
public class ChapterService : IChapterService
{
    private readonly AppDbContext _context;
    private static readonly SemaphoreSlim _uploadLock = new SemaphoreSlim(1, 1);

    public ChapterService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo chương mới trong một bộ truyện.
    /// Business Rule: ChapterNumber phải là duy nhất trong Series.
    /// </summary>
    public async Task<ChapterDto> CreateChapter(Guid seriesId, Guid mangakaId, CreateChapterDto dto)
    {
        var series = await _context.Series.FindAsync(seriesId)
            ?? throw new KeyNotFoundException($"Bộ truyện với ID {seriesId} không tồn tại.");

        if (series.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thêm chương vào bộ truyện này.");
        }

        var chapterExists = await _context.Chapters
            .AnyAsync(c => c.SeriesId == seriesId && c.ChapterNumber == dto.ChapterNumber);

        if (chapterExists)
        {
            throw new ArgumentException($"Số chương {dto.ChapterNumber} đã tồn tại trong bộ truyện này.");
        }

        var chapter = new Chapter
        {
            ChapterId = Guid.NewGuid(),
            SeriesId = seriesId,
            ChapterNumber = dto.ChapterNumber,
            Title = dto.Title,
            Status = "draft",
            DueDate = dto.DueDate.HasValue ? DateOnly.FromDateTime(dto.DueDate.Value) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();

        return await GetChapterById(chapter.ChapterId);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một chương truyện.
    /// </summary>
    public async Task<ChapterDto> GetChapterById(Guid chapterId)
    {
        var chapter = await _context.Chapters
            .Include(c => c.Series)
            .Include(c => c.MangaPages)
            .FirstOrDefaultAsync(c => c.ChapterId == chapterId)
            ?? throw new KeyNotFoundException($"Chương truyện với ID {chapterId} không tồn tại.");

        return MapToDto(chapter);
    }

    /// <summary>
    /// Cập nhật thông tin chương truyện.
    /// </summary>
    public async Task<ChapterDto> UpdateChapter(Guid chapterId, Guid mangakaId, UpdateChapterDto dto)
    {
        var chapter = await _context.Chapters
            .Include(c => c.Series)
            .FirstOrDefaultAsync(c => c.ChapterId == chapterId)
            ?? throw new KeyNotFoundException($"Chương truyện với ID {chapterId} không tồn tại.");

        if (chapter.Series.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa chương này.");
        }

        if (dto.Title != null) chapter.Title = dto.Title;
        if (dto.DueDate != null) chapter.DueDate = DateOnly.FromDateTime(dto.DueDate.Value);
        if (dto.Status != null) chapter.Status = dto.Status;

        chapter.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetChapterById(chapter.ChapterId);
    }

    /// <summary>
    /// Lấy danh sách trang của chương.
    /// </summary>
    public async Task<List<PageDto>> GetPagesByChapter(Guid chapterId)
    {
        return await _context.MangaPages
            .Where(p => p.ChapterId == chapterId)
            .Include(p => p.UploadedBy)
            .Include(p => p.PageAnnotations)
            .Include(p => p.PageRegions) // used for counting tasks
            .OrderBy(p => p.PageNumber)
            .Select(p => new PageDto
            {
                PageId = p.PageId,
                ChapterId = p.ChapterId,
                PageNumber = p.PageNumber,
                CurrentImageUrl = p.CurrentImageUrl,
                Status = p.Status,
                UploadedById = p.UploadedById,
                UploadedByName = p.UploadedBy != null ? p.UploadedBy.FullName : null,
                UploadedAt = p.UploadedAt,
                AnnotationCount = p.PageAnnotations.Count,
                TaskCount = _context.Tasks.Count(t => t.PageId == p.PageId)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Upload nhiều trang cùng lúc dưới dạng ảnh (.PNG, .JPG, .JPEG) hoặc file gốc (.PSD, .CLIP).
    /// Business Rule:
    /// - Dung lượng tối đa 50MB/file.
    /// - Tự động tạo số trang (PageNumber) theo thứ tự tăng dần, thread-safe.
    /// </summary>
    public async Task<UploadPagesResponseDto> UploadPages(Guid chapterId, List<IFormFile> files, Guid uploadedById)
    {
        if (files == null || files.Count == 0)
        {
            throw new ArgumentException("Danh sách file tải lên không được trống.");
        }

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".psd", ".clip" };
        var maxFileSize = 50 * 1024 * 1024; // 50 MB

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                throw new ArgumentException($"Định dạng file {file.FileName} không được hỗ trợ. Chỉ chấp nhận .PNG, .JPG, .JPEG, .PSD, .CLIP.");
            }

            if (file.Length > maxFileSize)
            {
                throw new ArgumentException($"File {file.FileName} vượt quá kích thước tối đa cho phép là 50MB.");
            }
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var resultDto = new UploadPagesResponseDto();

        // Sử dụng SemaphoreSlim để đảm bảo PageNumber tăng dần thread-safe
        await _uploadLock.WaitAsync();
        try
        {
            var maxPageNumber = await _context.MangaPages
                .Where(p => p.ChapterId == chapterId)
                .Select(p => (int?)p.PageNumber)
                .MaxAsync() ?? 0;

            foreach (var file in files)
            {
                maxPageNumber++;

                var ext = Path.GetExtension(file.FileName).ToLower();
                var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/Uploads/{uniqueFileName}";

                var page = new MangaPage
                {
                    PageId = Guid.NewGuid(),
                    ChapterId = chapterId,
                    PageNumber = maxPageNumber,
                    CurrentImageUrl = imageUrl,
                    Status = "pending",
                    UploadedById = uploadedById,
                    UploadedAt = DateTime.UtcNow
                };

                // Create initial version
                var version = new PageVersion
                {
                    PageVersionId = Guid.NewGuid(),
                    PageId = page.PageId,
                    VersionNumber = 1,
                    FileUrl = imageUrl,
                    FileName = file.FileName,
                    FileSizeBytes = file.Length,
                    MimeType = file.ContentType,
                    UploadedById = uploadedById,
                    CreatedAt = DateTime.UtcNow,
                    Note = "Tải lên trang ban đầu"
                };

                _context.MangaPages.Add(page);
                _context.PageVersions.Add(version);

                resultDto.Pages.Add(new PageUploadResultDto
                {
                    PageId = page.PageId,
                    PageNumber = page.PageNumber,
                    ImageUrl = imageUrl,
                    OriginalFileName = file.FileName,
                    Status = page.Status
                });
            }

            await _context.SaveChangesAsync();
            resultDto.TotalUploaded = files.Count;
        }
        finally
        {
            _uploadLock.Release();
        }

        return resultDto;
    }

    /// <summary>
    /// Xóa một trang khỏi chương truyện.
    /// </summary>
    public async System.Threading.Tasks.Task DeletePage(Guid pageId, Guid mangakaId)
    {
        var page = await _context.MangaPages
            .Include(p => p.Chapter)
                .ThenInclude(c => c.Series)
            .FirstOrDefaultAsync(p => p.PageId == pageId)
            ?? throw new KeyNotFoundException($"Trang truyện với ID {pageId} không tồn tại.");

        if (page.Chapter.Series.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa trang này.");
        }

        // Xóa các dữ liệu liên quan
        var versions = _context.PageVersions.Where(v => v.PageId == pageId);
        _context.PageVersions.RemoveRange(versions);

        var annotations = _context.PageAnnotations.Where(a => a.PageId == pageId);
        _context.PageAnnotations.RemoveRange(annotations);

        var tasks = _context.Tasks.Where(t => t.PageId == pageId);
        _context.Tasks.RemoveRange(tasks);

        _context.MangaPages.Remove(page);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Nộp chương truyện để xem xét xuất bản.
    /// Business Rule: Phải có ít nhất 1 trang mới được nộp.
    /// </summary>
    public async Task<ChapterDto> SubmitChapterForPublishing(Guid chapterId, Guid mangakaId)
    {
        var chapter = await _context.Chapters
            .Include(c => c.Series)
            .Include(c => c.MangaPages)
            .FirstOrDefaultAsync(c => c.ChapterId == chapterId)
            ?? throw new KeyNotFoundException($"Chương truyện với ID {chapterId} không tồn tại.");

        if (chapter.Series.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền nộp chương này.");
        }

        if (chapter.MangaPages.Count == 0)
        {
            throw new InvalidOperationException("Chương truyện phải có ít nhất 1 trang trước khi nộp xuất bản.");
        }

        chapter.Status = "submitted_for_publishing";
        chapter.SubmittedForPublishingAt = DateTime.UtcNow;
        chapter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetChapterById(chapter.ChapterId);
    }

    private static ChapterDto MapToDto(Chapter c)
    {
        return new ChapterDto
        {
            ChapterId = c.ChapterId,
            SeriesId = c.SeriesId,
            SeriesTitle = c.Series?.Title ?? "Unknown",
            ChapterNumber = c.ChapterNumber,
            Title = c.Title,
            Status = c.Status,
            DueDate = c.DueDate.HasValue ? c.DueDate.Value.ToDateTime(TimeOnly.MinValue) : null,
            SubmittedForPublishingAt = c.SubmittedForPublishingAt,
            PageCount = c.MangaPages?.Count ?? 0,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
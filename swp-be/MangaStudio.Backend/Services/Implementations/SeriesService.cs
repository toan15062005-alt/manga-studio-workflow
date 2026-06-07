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
/// Triển khai các nghiệp vụ quản lý bộ truyện (Series).
/// </summary>
public class SeriesService : ISeriesService
{
    private readonly AppDbContext _context;

    public SeriesService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách bộ truyện do Mangaka này sở hữu.
    /// </summary>
    public async Task<List<SeriesDto>> GetSeriesByMangaka(Guid mangakaId)
    {
        return await _context.Series
            .Where(s => s.MangakaId == mangakaId)
            .Include(s => s.SeriesGenres)
            .Include(s => s.Chapters)
            .Include(s => s.Mangaka)
            .Include(s => s.Tantou)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    /// <summary>
    /// Lấy chi tiết một bộ truyện theo ID.
    /// </summary>
    public async Task<SeriesDto> GetSeriesById(Guid seriesId, Guid requestUserId)
    {
        var series = await _context.Series
            .Include(s => s.SeriesGenres)
            .Include(s => s.Chapters)
            .Include(s => s.Mangaka)
            .Include(s => s.Tantou)
            .FirstOrDefaultAsync(s => s.SeriesId == seriesId)
            ?? throw new KeyNotFoundException($"Series với ID {seriesId} không tồn tại.");

        return MapToDto(series);
    }

    /// <summary>
    /// Tạo bộ truyện mới. Đồng thời tự động tạo SeriesProposal với trạng thái 'submitted'.
    /// Business Rule: Status mặc định khi tạo mới là 'proposal'.
    /// </summary>
    public async Task<SeriesDto> CreateSeries(Guid mangakaId, CreateSeriesDto dto)
    {
        var series = new Series
        {
            SeriesId = Guid.NewGuid(),
            Title = dto.Title,
            TitleJp = dto.TitleJp,
            Synopsis = dto.Synopsis,
            Status = "proposal", // BR-Series
            MangakaId = mangakaId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.Genres != null)
        {
            foreach (var genre in dto.Genres)
            {
                series.SeriesGenres.Add(new SeriesGenre
                {
                    SeriesId = series.SeriesId,
                    Genre = genre
                });
            }
        }

        _context.Series.Add(series);

        var proposal = new SeriesProposal
        {
            ProposalId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            SubmittedById = mangakaId,
            Status = "submitted",
            SubmittedAt = DateTime.UtcNow
        };

        _context.SeriesProposals.Add(proposal);

        await _context.SaveChangesAsync();

        return await GetSeriesById(series.SeriesId, mangakaId);
    }

    /// <summary>
    /// Cập nhật thông tin bộ truyện.
    /// Business Rule: Chỉ Mangaka sở hữu series mới được sửa.
    /// </summary>
    public async Task<SeriesDto> UpdateSeries(Guid seriesId, Guid mangakaId, UpdateSeriesDto dto)
    {
        var series = await _context.Series
            .Include(s => s.SeriesGenres)
            .FirstOrDefaultAsync(s => s.SeriesId == seriesId)
            ?? throw new KeyNotFoundException($"Series với ID {seriesId} không tồn tại.");

        if (series.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật bộ truyện này.");
        }

        if (dto.Title != null) series.Title = dto.Title;
        if (dto.TitleJp != null) series.TitleJp = dto.TitleJp;
        if (dto.Synopsis != null) series.Synopsis = dto.Synopsis;
        if (dto.Status != null) series.Status = dto.Status;

        if (dto.Genres != null)
        {
            _context.SeriesGenres.RemoveRange(series.SeriesGenres);
            foreach (var genre in dto.Genres)
            {
                series.SeriesGenres.Add(new SeriesGenre
                {
                    SeriesId = series.SeriesId,
                    Genre = genre
                });
            }
        }

        series.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetSeriesById(series.SeriesId, mangakaId);
    }

    /// <summary>
    /// Lấy danh sách chương của bộ truyện.
    /// </summary>
    public async Task<List<ChapterDto>> GetChaptersBySeries(Guid seriesId, Guid requestUserId)
    {
        return await _context.Chapters
            .Where(c => c.SeriesId == seriesId)
            .Include(c => c.Series)
            .OrderBy(c => c.ChapterNumber)
            .Select(c => new ChapterDto
            {
                ChapterId = c.ChapterId,
                SeriesId = c.SeriesId,
                SeriesTitle = c.Series.Title,
                ChapterNumber = c.ChapterNumber,
                Title = c.Title,
                Status = c.Status,
                DueDate = c.DueDate.HasValue ? c.DueDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                SubmittedForPublishingAt = c.SubmittedForPublishingAt,
                PageCount = c.MangaPages.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    private static SeriesDto MapToDto(Series series)
    {
        return new SeriesDto
        {
            SeriesId = series.SeriesId,
            Title = series.Title,
            TitleJp = series.TitleJp,
            Synopsis = series.Synopsis,
            CoverImageUrl = series.CoverImageUrl,
            Status = series.Status,
            MangakaId = series.MangakaId,
            MangakaName = series.Mangaka?.FullName ?? "Unknown",
            TantouId = series.TantouId,
            TantouName = series.Tantou?.FullName,
            Ranking = series.Ranking,
            ReaderCount = series.ReaderCount,
            Rating = series.Rating,
            Genres = series.SeriesGenres.Select(g => g.Genre).ToList(),
            ChapterCount = series.Chapters?.Count ?? 0,
            CreatedAt = series.CreatedAt,
            UpdatedAt = series.UpdatedAt
        };
    }
}
using MangaStudio.Backend.Models.DTOs;

namespace MangaStudio.Backend.Services.Interfaces;

public interface IMangakaService
{
    Task<DashboardStatsDto> GetDashboardStats(Guid mangakaId);

    Task<List<MangaSeriesDto>> GetSeries(Guid mangakaId);

    Task<string> UploadPage(Guid chapterId, IFormFile file, Guid uploadedById);
}
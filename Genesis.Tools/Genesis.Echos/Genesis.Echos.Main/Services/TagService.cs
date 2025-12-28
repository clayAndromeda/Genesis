using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Genesis.Echos.Main.Services;

public class TagService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TagService> _logger;

    public TagService(IServiceScopeFactory scopeFactory, ILogger<TagService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// すべてのタグを取得
    /// </summary>
    public async Task<List<Tag>> GetAllTagsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tags");
            throw;
        }
    }

    /// <summary>
    /// IDでタグを取得
    /// </summary>
    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Tags
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by id: {TagId}", id);
            throw;
        }
    }
}

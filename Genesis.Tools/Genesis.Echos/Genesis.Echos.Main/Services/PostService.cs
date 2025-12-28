using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Genesis.Echos.Main.Services;

public class PostService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostService> _logger;

    public PostService(IServiceScopeFactory scopeFactory, ILogger<PostService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// すべての投稿を取得（作成者、いいね数を含む）
    /// </summary>
    public async Task<List<Post>> GetAllPostsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Posts
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            throw;
        }
    }

    /// <summary>
    /// IDで投稿を取得（作成者、いいね、コメントを含む）
    /// </summary>
    public async Task<Post?> GetPostByIdAsync(int id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Posts
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .ThenInclude(l => l.User)
                .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post by ID: {PostId}", id);
            throw;
        }
    }

    /// <summary>
    /// 新しい投稿を作成
    /// </summary>
    public async Task<Post> CreatePostAsync(Post post)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            post.CreatedAt = DateTime.UtcNow;
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created post {PostId} by user {UserId}", post.Id, post.AuthorId);
            return post;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            throw;
        }
    }

    /// <summary>
    /// 投稿を更新（作成者のみ）
    /// </summary>
    public async Task<bool> UpdatePostAsync(Post post, string currentUserId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingPost = await context.Posts.FindAsync(post.Id);
            if (existingPost == null)
            {
                _logger.LogWarning("Post {PostId} not found", post.Id);
                return false;
            }

            // 作成者チェック
            if (existingPost.AuthorId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to update post {PostId} owned by {OwnerId}",
                    currentUserId, post.Id, existingPost.AuthorId);
                return false;
            }

            existingPost.Title = post.Title;
            existingPost.Content = post.Content;
            existingPost.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            _logger.LogInformation("Updated post {PostId}", post.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", post.Id);
            throw;
        }
    }

    /// <summary>
    /// 投稿を削除（作成者のみ）
    /// </summary>
    public async Task<bool> DeletePostAsync(int id, string currentUserId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var post = await context.Posts.FindAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found", id);
                return false;
            }

            // 作成者チェック
            if (post.AuthorId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete post {PostId} owned by {OwnerId}",
                    currentUserId, id, post.AuthorId);
                return false;
            }

            context.Posts.Remove(post);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted post {PostId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            throw;
        }
    }

    /// <summary>
    /// いいねをトグル（追加/削除）
    /// </summary>
    public async Task<bool> ToggleLikeAsync(int postId, string userId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingLike = await context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                // いいねを削除
                context.Likes.Remove(existingLike);
                await context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} unliked post {PostId}", userId, postId);
                return false; // いいねが削除された
            }
            else
            {
                // いいねを追加
                var like = new Like
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                context.Likes.Add(like);
                await context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} liked post {PostId}", userId, postId);
                return true; // いいねが追加された
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like for post {PostId} by user {UserId}", postId, userId);
            throw;
        }
    }

    /// <summary>
    /// ユーザーが投稿にいいねしているかチェック
    /// </summary>
    public async Task<bool> HasUserLikedAsync(int postId, string userId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking like status for post {PostId} by user {UserId}", postId, userId);
            throw;
        }
    }
}

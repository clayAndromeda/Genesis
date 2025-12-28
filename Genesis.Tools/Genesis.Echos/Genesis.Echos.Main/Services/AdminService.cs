using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Domain.Enums;
using Genesis.Echos.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Genesis.Echos.Main.Services;

public class AdminService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminService> logger,
        UserManager<ApplicationUser> userManager)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// すべてのユーザーを取得
    /// </summary>
    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Users
                .OrderBy(u => u.Email)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    /// <summary>
    /// ユーザーのロールを変更（Adminロールへの変更は不可）
    /// </summary>
    public async Task<bool> ChangeUserRoleAsync(string userId, UserRole newRole)
    {
        try
        {
            // Adminロールへの変更は許可しない
            if (newRole == UserRole.Admin)
            {
                _logger.LogWarning("Attempted to change user {UserId} to Admin role", userId);
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // 既存のロールを削除
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // 新しいロールを追加
            user.Role = newRole;
            await _userManager.UpdateAsync(user);
            await _userManager.AddToRoleAsync(user, newRole.ToString());

            _logger.LogInformation("Changed user {UserId} role to {NewRole}", userId, newRole);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user {UserId} role to {NewRole}", userId, newRole);
            throw;
        }
    }

    /// <summary>
    /// ユーザーを削除（Adminロールの削除は不可）
    /// </summary>
    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // Adminロールの削除は許可しない
            if (user.Role == UserRole.Admin)
            {
                _logger.LogWarning("Attempted to delete Admin user {UserId}", userId);
                return false;
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted user {UserId}", userId);
                return true;
            }

            _logger.LogWarning("Failed to delete user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 管理者として投稿を削除
    /// </summary>
    public async Task<bool> DeletePostAsync(int postId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var post = await context.Posts.FindAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found", postId);
                return false;
            }

            context.Posts.Remove(post);
            await context.SaveChangesAsync();

            _logger.LogInformation("Admin deleted post {PostId}", postId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", postId);
            throw;
        }
    }
}

using Genesis.Echos.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Genesis.Echos.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // Create roles
        if (!await roleManager.RoleExistsAsync("Leader"))
        {
            await roleManager.CreateAsync(new IdentityRole("Leader"));
        }
        if (!await roleManager.RoleExistsAsync("Member"))
        {
            await roleManager.CreateAsync(new IdentityRole("Member"));
        }

        // Create default tags
        if (!context.Tags.Any())
        {
            context.Tags.AddRange(
                new Tag { Name = "アイデア", Color = "#0d6efd", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "バグ報告", Color = "#dc3545", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "改善提案", Color = "#198754", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "質問", Color = "#ffc107", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "その他", Color = "#6c757d", CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }
    }
}

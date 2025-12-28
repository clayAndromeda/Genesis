using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Genesis.Echos.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
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

        // Create default Leader user
        if (!context.Users.Any(u => u.Email == "leader@echos.com"))
        {
            var leader = new ApplicationUser
            {
                UserName = "leader@echos.com",
                Email = "leader@echos.com",
                EmailConfirmed = true,
                Role = UserRole.Leader,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(leader, "Leader123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(leader, "Leader");
            }
        }

        // Create default Member user
        if (!context.Users.Any(u => u.Email == "member@echos.com"))
        {
            var member = new ApplicationUser
            {
                UserName = "member@echos.com",
                Email = "member@echos.com",
                EmailConfirmed = true,
                Role = UserRole.Member,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(member, "Member123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(member, "Member");
            }
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

using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Genesis.Echos.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
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
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Promote configured admin users
        var adminEmails = configuration.GetSection("AdminSettings:AdminEmails").Get<string[]>();
        if (adminEmails != null && adminEmails.Length > 0)
        {
            foreach (var email in adminEmails)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user != null && user.Role != UserRole.Admin)
                {
                    // Remove existing roles
                    var currentRoles = await userManager.GetRolesAsync(user);
                    await userManager.RemoveFromRolesAsync(user, currentRoles);

                    // Set as Admin
                    user.Role = UserRole.Admin;
                    await userManager.UpdateAsync(user);
                    await userManager.AddToRoleAsync(user, "Admin");
                }
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

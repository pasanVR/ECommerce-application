using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var config = sp.GetRequiredService<IConfiguration>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        var adminEmail = (config["Seed:AdminEmail"] ?? "admin@ecommerce.local").ToLowerInvariant();
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin#12345";

        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new User
            {
                FullName = "System Administrator",
                Email = adminEmail,
                PasswordHash = hasher.Hash(adminPassword),
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }
    }
}

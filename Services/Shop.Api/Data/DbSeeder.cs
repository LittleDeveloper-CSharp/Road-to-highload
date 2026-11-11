using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Domain;

namespace Shop.Api.Data;

public static class DbSeeder
{
    public const string SeedUserEmail = "demo@local.test";
    public const string SeedUserPassword = "Demo123!";
    public static readonly Guid SeedProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (env.IsDevelopment())
        {
            await db.Database.MigrateAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.Add(new Product
            {
                Id = SeedProductId,
                Name = "Demo Widget",
                Stock = 100,
                Price = 9.99m,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var existing = await userManager.FindByEmailAsync(SeedUserEmail);
        if (existing is null)
        {
            var user = new IdentityUser
            {
                UserName = SeedUserEmail,
                Email = SeedUserEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, SeedUserPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to seed demo user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

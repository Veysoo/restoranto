using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    // Bu sürüm arttığında mevcut default şifreler güncellenir
    private const string SeedVersion = "v2";

    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        await WaitForDatabaseAsync(context, cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);

        // Her başlangıçta şifre versiyonunu kontrol et
        await EnsureStrongPasswordsAsync(context, cancellationToken);

        if (await context.Users.AnyAsync(cancellationToken)) return;

        var adminId = Guid.NewGuid();
        context.Users.AddRange(
            new User
            {
                UserId = adminId,
                FullName = "Yönetici",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Resto@Admin2024!"),
                Role = UserRole.Admin
            },
            new User
            {
                FullName = "Ahmet Yılmaz",
                Username = "ahmet",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Garson@2024!"),
                Role = UserRole.Waiter
            },
            new User
            {
                FullName = "Ayşe Kaya",
                Username = "ayse",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Kasiyer@2024!"),
                Role = UserRole.Cashier
            }
        );

        context.Settings.AddRange(
            new Setting { Key = "RestaurantName", Value = "Lezzet Durağı", Description = "Restoran adı" },
            new Setting { Key = "DefaultTaxRate", Value = "10", Description = "Varsayılan KDV %" },
            new Setting { Key = "AccentColor", Value = "#4F6EF7", Description = "Tema rengi" },
            new Setting { Key = "SeedVersion", Value = SeedVersion, Description = "Seed versiyonu" }
        );

        var categories = new[]
        {
            new MenuCategory { Name = "Çorbalar", Icon = "🍲", DisplayOrder = 1 },
            new MenuCategory { Name = "Başlangıçlar", Icon = "🥗", DisplayOrder = 2 },
            new MenuCategory { Name = "Ana Yemekler", Icon = "🍽️", DisplayOrder = 3 },
            new MenuCategory { Name = "Pide & Pizza", Icon = "🫓", DisplayOrder = 4 },
            new MenuCategory { Name = "İçecekler", Icon = "☕", DisplayOrder = 5 },
            new MenuCategory { Name = "Tatlılar", Icon = "🍮", DisplayOrder = 6 }
        };
        context.MenuCategories.AddRange(categories);
        await context.SaveChangesAsync(cancellationToken);

        context.MenuItems.AddRange(
            // Çorbalar
            new MenuItem { CategoryId = categories[0].CategoryId, Name = "Mercimek Çorbası", Price = 90, TaxRate = 10, PrepTimeMinutes = 5 },
            new MenuItem { CategoryId = categories[0].CategoryId, Name = "Ezogelin Çorbası", Price = 90, TaxRate = 10, PrepTimeMinutes = 5 },
            new MenuItem { CategoryId = categories[0].CategoryId, Name = "Domates Çorbası", Price = 95, TaxRate = 10, PrepTimeMinutes = 5 },
            // Başlangıçlar
            new MenuItem { CategoryId = categories[1].CategoryId, Name = "Humus", Price = 130, TaxRate = 10 },
            new MenuItem { CategoryId = categories[1].CategoryId, Name = "Sigara Böreği", Price = 110, TaxRate = 10 },
            new MenuItem { CategoryId = categories[1].CategoryId, Name = "Acuka", Price = 100, TaxRate = 10 },
            new MenuItem { CategoryId = categories[1].CategoryId, Name = "Kaşarlı Puf Böreği", Price = 140, TaxRate = 10 },
            // Ana Yemekler
            new MenuItem { CategoryId = categories[2].CategoryId, Name = "Izgara Köfte", Price = 340, TaxRate = 10, PrepTimeMinutes = 20 },
            new MenuItem { CategoryId = categories[2].CategoryId, Name = "Tavuk Şiş", Price = 300, TaxRate = 10, PrepTimeMinutes = 18 },
            new MenuItem { CategoryId = categories[2].CategoryId, Name = "Adana Kebap", Price = 380, TaxRate = 10, PrepTimeMinutes = 22 },
            new MenuItem { CategoryId = categories[2].CategoryId, Name = "Balık Izgara", Price = 480, TaxRate = 10, PrepTimeMinutes = 25 },
            new MenuItem { CategoryId = categories[2].CategoryId, Name = "İskender", Price = 420, TaxRate = 10, PrepTimeMinutes = 15 },
            new MenuItem { CategoryId = categories[2].CategoryId, Name = "Mantı", Price = 260, TaxRate = 10, PrepTimeMinutes = 15 },
            // Pide & Pizza
            new MenuItem { CategoryId = categories[3].CategoryId, Name = "Kıymalı Pide", Price = 230, TaxRate = 10, PrepTimeMinutes = 20 },
            new MenuItem { CategoryId = categories[3].CategoryId, Name = "Kaşarlı Pide", Price = 200, TaxRate = 10, PrepTimeMinutes = 18 },
            new MenuItem { CategoryId = categories[3].CategoryId, Name = "Karışık Pizza", Price = 320, TaxRate = 10, PrepTimeMinutes = 25 },
            // İçecekler
            new MenuItem { CategoryId = categories[4].CategoryId, Name = "Ayran", Price = 40, TaxRate = 10 },
            new MenuItem { CategoryId = categories[4].CategoryId, Name = "Cola", Price = 50, TaxRate = 10 },
            new MenuItem { CategoryId = categories[4].CategoryId, Name = "Limonata", Price = 65, TaxRate = 10 },
            new MenuItem { CategoryId = categories[4].CategoryId, Name = "Türk Kahvesi", Price = 70, TaxRate = 10, PrepTimeMinutes = 5 },
            new MenuItem { CategoryId = categories[4].CategoryId, Name = "Çay", Price = 35, TaxRate = 10, PrepTimeMinutes = 3 },
            new MenuItem { CategoryId = categories[4].CategoryId, Name = "Su", Price = 20, TaxRate = 10 },
            // Tatlılar
            new MenuItem { CategoryId = categories[5].CategoryId, Name = "Künefe", Price = 200, TaxRate = 10, PrepTimeMinutes = 10 },
            new MenuItem { CategoryId = categories[5].CategoryId, Name = "Sütlaç", Price = 110, TaxRate = 10 },
            new MenuItem { CategoryId = categories[5].CategoryId, Name = "Baklava", Price = 180, TaxRate = 10 },
            new MenuItem { CategoryId = categories[5].CategoryId, Name = "Fırın Sütlaç", Price = 120, TaxRate = 10 }
        );

        var sections = new[] { ("İç Salon", 10), ("Bahçe Terası", 8), ("Bar", 4), ("VIP", 2) };
        var tableNum = 1;
        var order = 1;
        foreach (var (section, count) in sections)
        {
            for (var i = 1; i <= count; i++)
            {
                context.Tables.Add(new Table
                {
                    TableNumber = tableNum,
                    Name = $"{section} {i}",
                    Capacity = section == "Bar" ? 2 : section == "VIP" ? 8 : (i % 3 == 0 ? 6 : 4),
                    Section = section,
                    DisplayOrder = order++
                });
                tableNum++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Her başlangıçta çalışır. Eski/zayıf şifreler varsa günceller.
    /// </summary>
    private static async Task EnsureStrongPasswordsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var versionSetting = await context.Settings.FirstOrDefaultAsync(s => s.Key == "SeedVersion", cancellationToken);
        if (versionSetting?.Value == SeedVersion) return;

        // Eski zayıf şifreleri güçlü ile değiştir
        var weakPasswords = new Dictionary<string, (string weakPwd, string strongPwd, string fullName)>
        {
            ["admin"] = ("admin123", "Resto@Admin2024!", "Yönetici"),
            ["ahmet"] = ("waiter123", "Garson@2024!", "Ahmet Yılmaz"),
            ["ayse"] = ("cashier123", "Kasiyer@2024!", "Ayşe Kaya"),
        };

        var users = await context.Users.ToListAsync(cancellationToken);
        bool updated = false;
        foreach (var user in users)
        {
            if (weakPasswords.TryGetValue(user.Username, out var info))
            {
                if (BCrypt.Net.BCrypt.Verify(info.weakPwd, user.PasswordHash))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(info.strongPwd);
                    user.FullName = info.fullName;
                    updated = true;
                }
            }
        }

        if (versionSetting == null)
        {
            context.Settings.Add(new Setting { Key = "SeedVersion", Value = SeedVersion, Description = "Seed versiyonu" });
        }
        else
        {
            versionSetting.Value = SeedVersion;
        }

        if (updated || versionSetting == null)
            await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task WaitForDatabaseAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string bulunamadı.");

        const int maxAttempts = 30;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"SQL Server {maxAttempts} denemede hazır olmadı.",
            lastError);
    }
}

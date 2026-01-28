using BerkYazilim.Models;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Data
{
    public static class DbSeeder
    {
        public static void Seed(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<AppDbContext>();

                // 1. Veritabanı yoksa oluştur (Migrationları uygula)
                context.Database.Migrate();

                // 2. KULLANICILAR (USERS)
                if (!context.Users.Any())
                {
                    // --- CREATOR (SUPER ADMIN - SEN) ---
                    context.Users.Add(new User
                    {
                        DealerCode = "CREATOR",
                        FullName = "Berk Yazılım Sahibi",
                        Password = BCrypt.Net.BCrypt.HashPassword("master"),
                        Role = "SuperAdmin", // Yeni Rolün
                        Email = "creator@berkyazilim.com",
                        Address = "Yönetim Merkezi",
                        IsActive = true,
                        SubscriptionEndDate = DateTime.MaxValue // Senin süren asla bitmez
                    });
                    // --- YÖNETİCİ (ADMIN) ---
                    context.Users.Add(new User
                    {
                        DealerCode = "ADMIN",
                        FullName = "Sistem Yöneticisi",
                        Password = BCrypt.Net.BCrypt.HashPassword("master"),
                        Role = "Admin",
                        Email = "admin@berkyazilim.com",
                        Address = "Genel Merkez",
                        Phone = "0850 123 45 67"
                    });

                    // --- BAYİ (DEALER) ---
                    context.Users.Add(new User
                    {
                        DealerCode = "BAYI-2025-001",
                        FullName = "Ahmet Bilişim",
                        Password = BCrypt.Net.BCrypt.HashPassword("123"),
                        Role = "Dealer",
                        Email = "ahmet@bayi.com",
                        Address = "Teknopark İstanbul, Pendik",
                        Phone = "0555 999 88 77"
                    });

                    context.SaveChanges();
                }

                // 3. ÜRÜNLER (PRODUCTS)
                if (!context.Products.Any())
                {
                    var products = new List<Product>
                    {
                        new Product { Title="AMD Ryzen 9 7950X 16-Core", Category="İşlemci", CategorySlug="islemci", Brand="AMD", ImageUrl="https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?w=500&h=300&fit=crop", Price=18499.00m, OldPrice=19999.00m, Stock=47, StockLocations="İst:20, Ank:15", IsNew=true, IsHot=false, Specs="16 Core, 5.7 GHz" },
                        new Product { Title="Intel Core i9-14900K 24-Core", Category="İşlemci", CategorySlug="islemci", Brand="Intel", ImageUrl="https://images.unsplash.com/photo-1555680202-c31f73531838?w=500&h=300&fit=crop", Price=22999.00m, OldPrice=23999.00m, Stock=35, StockLocations="İst:15, Ank:12", IsNew=true, IsHot=true, Specs="24 Core, 6.0 GHz" },
                        new Product { Title="NVIDIA GeForce RTX 4090 24GB", Category="Ekran Kartı", CategorySlug="ekran-karti", Brand="NVIDIA", ImageUrl="https://images.unsplash.com/photo-1591488320449-011701bb6704?w=500&h=300&fit=crop", Price=65999.00m, OldPrice=69999.00m, Stock=8, StockLocations="İst:5, Ank:3", IsNew=false, IsHot=true, Specs="24GB GDDR6X" },
                        new Product { Title="MSI MEG X670E ACE Anakart", Category="Anakart", CategorySlug="anakart", Brand="MSI", ImageUrl="https://images.unsplash.com/photo-1587202372634-32705e3bf49c?w=500&h=300&fit=crop", Price=19999.00m, OldPrice=21999.00m, Stock=6, StockLocations="İst:4, Ank:2", IsNew=true, IsHot=false, Specs="E-ATX, AM5" },
                        new Product { Title="Corsair Vengeance 64GB DDR5", Category="RAM", CategorySlug="ram", Brand="Corsair", ImageUrl="https://images.unsplash.com/photo-1541845157-a6d2d100c931?w=500&h=300&fit=crop", Price=9499.00m, OldPrice=9999.00m, Stock=78, StockLocations="İst:30, Ank:28", IsNew=false, IsHot=false, Specs="6000MHz, CL30" },
                        new Product { Title="Samsung 990 PRO 2TB SSD", Category="SSD", CategorySlug="ssd", Brand="Samsung", ImageUrl="https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=500&h=300&fit=crop", Price=5299.00m, OldPrice=5699.00m, Stock=92, StockLocations="İst:40, Ank:32", IsNew=false, IsHot=true, Specs="7450 MB/s Okuma" }
                    };

                    context.Products.AddRange(products);
                    context.SaveChanges();
                }
                // DbSeeder.cs içine Seed metodunun altına ekleyin:
                if (!context.SystemSettings.Any())
                {
                    context.SystemSettings.Add(new Models.SystemSetting()); // Varsayılan değerlerle ekler
                    context.SaveChanges();
                }
            }
        }
    }
}
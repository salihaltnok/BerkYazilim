using BerkYazilim.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;


QuestPDF.Settings.License = LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// 1. Controller servisini ekle (API için gerekli)
builder.Services.AddControllers();

// Veritabaný servisi (Burasý zaten vardý)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Veritabanýný tohumla (Burasý zaten vardý)
BerkYazilim.Data.DbSeeder.Seed(app);

// Hata ayýklama sayfasý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 2. Statik dosyalarý (wwwroot içindekileri) kullanýma aç
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 3. API Rotalarýný eþle
app.MapControllers();

// Varsayýlan olarak login.html açýlsýn istersen (Ýsteðe baðlý)
app.MapGet("/", async context =>
{
    context.Response.Redirect("/login.html");
    await Task.CompletedTask;
});

app.Run();
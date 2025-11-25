using Microsoft.EntityFrameworkCore;
using Sushi.Data;

var builder = WebApplication.CreateBuilder(args);

// ثبت کنترلرها و Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS برای فرانت‌اند (Next.js روی پورت 3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", p =>
        p.WithOrigins("http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// DbContext (SQL Server)
builder.Services.AddDbContext<SushiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Swagger فقط در حالت Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// اگر نمی‌خوای HTTPS اجباری باشه، همین‌طور کامنت بمونه
// app.UseHttpsRedirection();

// 👈 خیلی مهم: برای سرو کردن فایل‌های استاتیک مثل عکس‌ها از wwwroot
app.UseStaticFiles();

// فعال‌سازی CORS قبل از کنترلرها
app.UseCors("frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sushi.Data;
using Sushi.Models.Auth;

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

// Password hasher برای کاربران
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// تنظیمات JWT
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? "p9Z!c3P#qLm82^Gd5@wXr7$Bk1Nf4&Hs8Yz0TuV6jKoQ2eCiR%aDnLgMhJ";

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger فقط در حالت Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // اگر HTTPS نمی‌خوای، همین‌طور کامنت باشه

// سرو کردن فایل‌های استاتیک مثل عکس‌ها از wwwroot
app.UseStaticFiles();

// فعال‌سازی CORS قبل از کنترلرها
app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

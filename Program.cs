using Microsoft.EntityFrameworkCore;
using Sushi.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS برای فرانت‌اند (Next.js روی 3000)
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// فقط HTTP می‌خوایم
// app.UseHttpsRedirection();

// فعال‌سازی CORS قبل از کنترلرها
app.UseCors("frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

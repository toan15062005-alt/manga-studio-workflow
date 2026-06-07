using MangaStudio.Backend.Data;
using MangaStudio.Backend.Services.Implementations;
using MangaStudio.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Controllers
builder.Services.AddControllers();

// Cấu hình CORS để cho phép Frontend gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Cấu hình Swagger Explorer
builder.Services.AddEndpointsApiExplorer();

// Cấu hình Swagger có hỗ trợ nút "Authorize" để test JWT Token trực tiếp trên giao diện Swagger UI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Manga Studio API", Version = "v1" });
    
    // Khai báo cơ chế Authorization sử dụng Bearer Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Token của bạn dưới dạng: Bearer {chuỗi_token_của_bạn}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Đăng ký DbContext kết nối tới Database SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

// Cấu hình JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MangaStudioWorkflowSuperSecretKey12345!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Tắt kiểm tra HTTPS để phát triển ở local dễ dàng
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MangaStudio.Backend",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MangaStudio.Frontend",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Tránh độ lệch múi giờ gây trễ hạn token
    };
});

// Đăng ký các Service xử lý nghiệp vụ (Dependency Injection)
builder.Services.AddScoped<IMangakaService, MangakaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISeriesService, SeriesService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();


var app = builder.Build();

// Sử dụng Swagger trong môi trường phát triển
app.UseSwagger();
app.UseSwaggerUI();

// Chỉ chuyển hướng HTTPS khi không ở môi trường Development để chạy thử local dễ dàng
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

// Kích hoạt CORS (Đặt trước Authentication/Authorization)
app.UseCors("AllowAll");

// Kích hoạt middleware xác thực (Authentication) - PHẢI ĐẶT TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
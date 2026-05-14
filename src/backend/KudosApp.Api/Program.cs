using System.Text;
using System.Text.Json.Serialization;
using KudosApp.Api.Data;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<ZohoOptions>(builder.Configuration.GetSection(ZohoOptions.SectionName));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3)));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Named HttpClient for Zoho API calls (3-second connect + 15-second read timeout)
builder.Services.AddHttpClient("ZohoCliq", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Singleton services (no DB dependency)
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IZohoBridge, ZohoBridge>();

// Scoped services (depend on AppDbContext which is Scoped)
builder.Services.AddScoped<IDataSeeder, DataSeeder>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReminderPolicy, ReminderPolicy>();
builder.Services.AddScoped<IVisibilityService, VisibilityService>();
builder.Services.AddScoped<IPointsService, PointsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IActionItemService, ActionItemService>();

// Background services
builder.Services.AddHostedService<ActionItemReminderHostedService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    seeder.Seed();
}

app.Run();

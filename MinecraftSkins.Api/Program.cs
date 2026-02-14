using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.OpenApi.Models;
using MinecraftSkins.Api;
using MinecraftSkins.Infrastructure.Data;
using Serilog;

// Load .env file if it exists (for local development)
if (File.Exists(".env"))
{
    Env.Load();
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting web application");
    var builder = WebApplication.CreateBuilder(args);

    // Отключаем встроенные логгеры (Console, Debug, EventSource)
    builder.Logging.ClearProviders();

    // Add services to the container.
    builder.Services.AddSerilog();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new Exception("Database connection string not configured");

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Redis Cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:Configuration"];
        options.InstanceName = "MinecraftSkins_";
    });

    // Add Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(option =>
    {
        option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                          "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                          "Example: \"Bearer 12345abcdef\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        option.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });
    });

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    // Use Serilog request logging middleware (logs HTTP requests cleanly)
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Fail-safe for Redis
    try
    {
        var redis = app.Services.GetRequiredService<IDistributedCache>();
        _ = redis.GetStringAsync("test");
    }
    catch (Exception ex)
    {
        // If Redis is not available, we can log it and continue without caching
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Redis is not available. Caching is disabled.");
    }

    // Database Migration
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    app.Run();
}
catch(Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

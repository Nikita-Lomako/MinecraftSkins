using System.Text;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinecraftSkins.Api.Configuration;
using MinecraftSkins.Api.Endpoints;
using MinecraftSkins.Api.Extensions;
using MinecraftSkins.Api.Handlers;
using MinecraftSkins.Application;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Application.Options;
using MinecraftSkins.Application.Services;
using MinecraftSkins.Application.Validation;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Infrastructure.Data;
using MinecraftSkins.Infrastructure.Data.Extensions;
using MinecraftSkins.Infrastructure.Configuration;
using MinecraftSkins.Infrastructure.Repositories;
using MinecraftSkins.Infrastructure.Services;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

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

    var jwtSecret = builder.Configuration["ApiSettings:Secret"]
        ?? throw new Exception("JWT secret not configured");

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Redis Cache
    builder.Services.AddMemoryCache();
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:Configuration"];
        options.InstanceName = "MinecraftSkins_";
    });

    // Register Repositories
    builder.Services.AddScoped<ISkinRepository, SkinRepository>();
    builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
    builder.Services.AddScoped<IAuthRepository, AuthRepository>();
    builder.Services.AddScoped<ICartRepository, CartRepository>();

    // Register Services
    builder.Services.AddScoped<ISkinService, SkinService>();
    builder.Services.AddScoped<IPurchaseService, PurchaseService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IBtcRateService, BtcRateService>();
    builder.Services.AddScoped<IQueryHistoryService, QueryHistoryService>();
    builder.Services.Configure<QueryHistoryOptions>(builder.Configuration.GetSection(QueryHistoryOptions.SectionName));
    builder.Services.AddHostedService<QueryHistoryCleanupHostedService>();

    // Register Price Calculator
    builder.Services.Configure<PriceCalculatorOptions>(builder.Configuration.GetSection(PriceCalculatorOptions.SectionName));
    builder.Services.AddScoped<IPriceCalculator, StandardPriceCalculator>(); // Default strategy
    // To switch strategy, use example below.
    // Example:
    // builder.Services.AddScoped<IPriceCalculator, PromoPriceCalculator>();

    // Configuration: "BtcRateProvider:Provider" = "CoinGecko" or "Binance"
    builder.Services.AddSingleton<IBtcRateProvider>(new FakeBtcRateProvider(80000m));
    //builder.Services.AddBtcRateProviders(builder.Configuration);

    // Register Idempotency Filter
    builder.Services.AddScoped(sp => new MinecraftSkins.Api.Filters.IdempotencyFilter(60));

    // Register HTTP Message Handlers for HttpClient
    builder.Services.AddTransient<MinecraftSkins.Api.Handlers.RateLimiterHandler>();
    builder.Services.AddTransient<MinecraftSkins.Api.Handlers.PollyLoggingHandler>();

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>(name: "Database")
        .AddCheck<MinecraftSkins.Api.HealthChecks.BtcRateProviderHealthCheck>(
            name: "BTC Rate Provider",
            tags: new[] { "external", "api" })
        .AddCheck<MinecraftSkins.Api.HealthChecks.RedisHealthCheck>(
            name: "Redis",
            tags: new[] { "cache", "redis" });


    // Register Validators
    builder.Services.AddScoped<IValidator<SkinCreateDto>, SkinCreateDtoValidator>();
    builder.Services.AddScoped<IValidator<SkinUpdateDto>, SkinUpdateDtoValidator>();
    builder.Services.AddScoped<IValidator<PurchaseCreateDto>, PurchaseCreateDtoValidator>();

    // Register AutoMapper
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());

    // Add Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    // JWT Authentication
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
    builder.Services.AddAuthorization();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(option =>
    {
        option.OperationFilter<MinecraftSkins.Api.Swagger.IdempotencyHeaderFilter>(); // Add this line
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
        
        // Добавляем health checks endpoint в Swagger документацию
        option.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MinecraftSkins API",
            Version = "v1",
            Description = "API для продажи Minecraft-скинов с BTC-индексированным ценообразованием"
        });
    });

    builder.Services.AddHttpContextAccessor();

    //// OpenTelemetry metrics pipeline:
    //builder.Services.AddOpenTelemetry()
    //    .ConfigureResource(resource => resource.AddService(
    //serviceName: "MinecraftSkinsAPI",
    //serviceVersion: "1.0.0"))
    //    .WithMetrics(metrics =>
    //    {
    //        metrics
    //            .AddAspNetCoreInstrumentation()
    //            .AddHttpClientInstrumentation()
    //            .AddRuntimeInstrumentation()
    //            .AddPrometheusExporter();
    //    });

    // CORS: разрешаем запросы с фронтенда (docker-compose: порт 3000; локальная разработка: 5173)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (origins?.Length > 0)
                policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
            else
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
    });

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

    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/metrics"),
        branch => branch.UseHttpsRedirection());

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();
    //app.UseOpenTelemetryPrometheusScrapingEndpoint();

    // Map Endpoints
    app.MapSkinEndpoints();
    app.MapPurchaseEndpoints();
    app.MapCartEndpoints();
    app.MapAuthEndpoints();
    app.MapRateEndpoints();
    app.MapHealthEndpoints();

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

    // Database: apply migration (schema + roles + skins from ModelBuilder.Seed) then seed users
    // Users are seeded here, not in AppDbContext.Seed(), because passwords must be hashed with UserManager at runtime.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var scopeLogger = scope.ServiceProvider.GetService<ILogger<Program>>();
        await DataSeeder.EnsureSeedSkinsAsync(db, scopeLogger);
        await DataSeeder.EnsureSeedUsersAsync(userManager, roleManager, scopeLogger);
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

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerUI;
using MultiVendor.Ecommerce.Api.Middleware;
using MultiVendor.Ecommerce.Application.Events;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Application.Interfaces.Auth;
using MultiVendor.Ecommerce.Application.Services;
using MultiVendor.Ecommerce.Infrastructure.Data;
using MultiVendor.Ecommerce.Infrastructure.Repositories;
using MultiVendor.Ecommerce.Infrastructure.Services;
using MultiVendor.Ecommerce.Infrastructure.Services.Auth;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/app-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
            retainedFileCountLimit: 7));

    // ── Database ──────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── Authentication ────────────────────────────────────────────────────────
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                ValidAudience            = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                ClockSkew                = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // ── Application Services ──────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITokenService,           TokenService>();
    builder.Services.AddScoped<IAuthService,            AuthService>();
    builder.Services.AddScoped<ICurrentMerchantService, CurrentMerchantService>();
    builder.Services.AddScoped<IProductRepository,      ProductRepository>();
    builder.Services.AddScoped<IProductService,         ProductService>();
    builder.Services.AddScoped<IVariantRepository,      VariantRepository>();
    builder.Services.AddScoped<IVariantService,         VariantService>();
    builder.Services.AddScoped<IEmailService,           DummyEmailService>();
    builder.Services.AddScoped<IEventHandler<LowStockEvent>, LowStockEventHandler>();

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName  = "MultiVendor:";
    });

    // ── API / Swagger ─────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "Multi-Vendor E-Commerce API",
            Version     = "v1",
            Description = "Backend API for a multi-vendor e-commerce platform. " +
                          "Authenticate using the /api/auth/login endpoint and paste " +
                          "the returned access token into the Authorize button."
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name        = "Authorization",
            Scheme      = "Bearer",
            In          = ParameterLocation.Header,
            Type        = SecuritySchemeType.Http,
            Description = "Enter your JWT access token"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Vendor E-Commerce API v1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
        c.DocExpansion(DocExpansion.List);
    });

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
        await DataSeeder.SeedAsync(db);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}

public partial class Program { }

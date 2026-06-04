using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using MultiVendor.Ecommerce.Api.Middleware;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Application.Interfaces.Auth;
using MultiVendor.Ecommerce.Application.Services;
using MultiVendor.Ecommerce.Infrastructure.Data;
using MultiVendor.Ecommerce.Infrastructure.Repositories;
using MultiVendor.Ecommerce.Infrastructure.Services;
using MultiVendor.Ecommerce.Infrastructure.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication ────────────────────────────────────────────────────────────
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

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService,           TokenService>();
builder.Services.AddScoped<IAuthService,            AuthService>();
builder.Services.AddScoped<ICurrentMerchantService, CurrentMerchantService>();
builder.Services.AddScoped<IProductRepository,      ProductRepository>();
builder.Services.AddScoped<IProductService,         ProductService>();
builder.Services.AddScoped<IVariantRepository,      VariantRepository>();
builder.Services.AddScoped<IVariantService,         VariantService>();

// ── API / Swagger ─────────────────────────────────────────────────────────────
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
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Vendor E-Commerce API v1");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
    c.DocExpansion(DocExpansion.List);
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

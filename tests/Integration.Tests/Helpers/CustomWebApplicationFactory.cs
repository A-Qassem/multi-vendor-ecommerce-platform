using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MultiVendor.Ecommerce.Infrastructure.Data;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Integration.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                { "Jwt:Key", "ThisIsATestKeyForIntegrationTestingThatIsLongEnough" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:ExpiresInMinutes", "15" }
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidIssuer = "TestIssuer";
                options.TokenValidationParameters.ValidAudience = "TestAudience";
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("ThisIsATestKeyForIntegrationTestingThatIsLongEnough"));
            });
            // Remove the app's AppDbContext registration and all DbContextOptions
            var dbContextOptionsDescriptors = services
                .Where(d => d.ServiceType.Name.Contains("DbContextOptions"))
                .ToList();

            foreach (var d in dbContextOptionsDescriptors)
            {
                services.Remove(d);
            }

            // Add AppDbContext using an in-memory database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Replace Redis cache with in-memory distributed cache
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDistributedCache));

            if (redisDescriptor != null)
            {
                services.Remove(redisDescriptor);
            }
            services.AddDistributedMemoryCache();
        });
    }

    public async Task<(string Email, string Password, Guid MerchantId)> SeedDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var merchant = new Merchant
        {
            Name = "Integration Test Merchant",
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password)
        };

        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        return (email, password, merchant.Id);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Integration.Tests.Helpers;
using MultiVendor.Ecommerce.Application.Common;

namespace MultiVendor.Ecommerce.Integration.Tests.Variants;

public class VariantsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public VariantsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid ProductId)> SetupProductAsync()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Product For Variant", Description = "Test", Status = "Active", BasePrice = 10m
        });
        
        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Product creation failed: {createResponse.StatusCode} - {errorContent}");
        }

        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        return (token, createdProduct!.Id);
    }

    [Fact]
    public async Task GetVariants_ShouldReturn200_WhenProductOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var response = await _client.GetAsync($"/api/products/{productId}/variants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<VariantResponse>>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetVariants_ShouldReturn403_WhenNotProductOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var response = await _client.GetAsync($"/api/products/{productId}/variants");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateVariant_ShouldReturn201_WithValidRequest()
    {
        var (_, productId) = await SetupProductAsync();

        var request = new CreateVariantRequest
        {
            SKU = $"SKU-{Guid.NewGuid()}",
            Quantity = 10,
            IsActive = true,
            Attributes = new List<VariantAttributeRequest>()
        };

        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<VariantResponse>();
        Assert.NotNull(content);
        content!.SKU.Should().Be(request.SKU);
    }

    [Fact]
    public async Task CreateVariant_ShouldReturn409_WhenDuplicateSKU()
    {
        var (_, productId) = await SetupProductAsync();

        var sku = $"DUPE-{Guid.NewGuid()}";
        var request = new CreateVariantRequest
        {
            SKU = sku,
            Quantity = 10,
            IsActive = true,
            Attributes = new List<VariantAttributeRequest>()
        };

        await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);
        var response2 = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);

        // Conflict due to exception mapping in ExceptionHandlingMiddleware mapping "SKU" InvalidOperationException to 409
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateVariant_ShouldReturn403_WhenNotProductOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var request = new CreateVariantRequest { SKU = "SKU-XXX", Quantity = 10, IsActive = true, Attributes = new List<VariantAttributeRequest>() };
        
        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetVariant_ShouldReturn200_WhenOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var request = new CreateVariantRequest { SKU = $"SKU-{Guid.NewGuid()}", Quantity = 10, IsActive = true, Attributes = new List<VariantAttributeRequest>() };
        var createResponse = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);
        var createdVariant = await createResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var response = await _client.GetAsync($"/api/products/{productId}/variants/{createdVariant!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<VariantResponse>();
        content!.Id.Should().Be(createdVariant.Id);
    }

    [Fact]
    public async Task GetVariant_ShouldReturn404_WhenNotFound()
    {
        var (_, productId) = await SetupProductAsync();

        var response = await _client.GetAsync($"/api/products/{productId}/variants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVariant_ShouldReturn200_WhenOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var request = new CreateVariantRequest { SKU = $"SKU-{Guid.NewGuid()}", Quantity = 10, IsActive = true, Attributes = new List<VariantAttributeRequest>() };
        var createResponse = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);
        var createdVariant = await createResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var updateRequest = new UpdateVariantRequest
        {
            Quantity = 20
        };

        var response = await _client.PutAsJsonAsync($"/api/products/{productId}/variants/{createdVariant!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<VariantResponse>();
        content!.Quantity.Should().Be(20);
    }

    [Fact]
    public async Task UpdateVariant_ShouldReturn403_WhenNotOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var request = new CreateVariantRequest { SKU = $"SKU-{Guid.NewGuid()}", Quantity = 10, IsActive = true, Attributes = new List<VariantAttributeRequest>() };
        var createResponse = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);
        var createdVariant = await createResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}/variants/{createdVariant!.Id}", new UpdateVariantRequest { Quantity = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteVariant_ShouldReturn204_WhenOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var request = new CreateVariantRequest { SKU = $"SKU-{Guid.NewGuid()}", Quantity = 10, IsActive = true, Attributes = new List<VariantAttributeRequest>() };
        var createResponse = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);
        var createdVariant = await createResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var response = await _client.DeleteAsync($"/api/products/{productId}/variants/{createdVariant!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteVariant_ShouldReturn403_WhenNotOwner()
    {
        var (_, productId) = await SetupProductAsync();

        var request = new CreateVariantRequest { SKU = $"SKU-{Guid.NewGuid()}", Quantity = 10, IsActive = true, Attributes = new List<VariantAttributeRequest>() };
        var createResponse = await _client.PostAsJsonAsync($"/api/products/{productId}/variants", request);
        var createdVariant = await createResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        
        var response = await _client.DeleteAsync($"/api/products/{productId}/variants/{createdVariant!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

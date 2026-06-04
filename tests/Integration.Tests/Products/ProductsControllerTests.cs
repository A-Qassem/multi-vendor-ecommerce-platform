using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Integration.Tests.Helpers;
using MultiVendor.Ecommerce.Application.Common;

namespace MultiVendor.Ecommerce.Integration.Tests.Products;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ShouldReturn200_WhenAuthenticated()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<ProductResponse>>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetProducts_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn201_WithValidRequest()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateProductRequest
        {
            Name = "Integration Test Product",
            Description = "Test",
            Status = "Active",
            BasePrice = 9.99m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(content);
        content!.Name.Should().Be("Integration Test Product");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400_WithMissingName()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateProductRequest
        {
            // Missing name
            Description = "Test",
            Status = "Active",
            BasePrice = 9.99m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn401_WhenNotAuthenticated()
    {
        var request = new CreateProductRequest
        {
            Name = "Integration Test Product",
            Description = "Test",
            Status = "Active",
            BasePrice = 9.99m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProduct_ShouldReturn200_WhenOwner()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Test", Description = "Test", Status = "Active", BasePrice = 10m
        });
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.GetAsync($"/api/products/{createdProduct!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ProductResponse>();
        content!.Id.Should().Be(createdProduct.Id);
    }

    [Fact]
    public async Task GetProduct_ShouldReturn403_WhenNotOwner()
    {
        // Setup owner and product
        var (ownerEmail, ownerPassword, _) = await _factory.SeedDatabaseAsync();
        var ownerToken = await AuthHelper.GetTokenAsync(_client, ownerEmail, ownerPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Owner Product", Description = "Test", Status = "Active", BasePrice = 10m
        });
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Setup other merchant
        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        // Attempt to access with other merchant
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var response = await _client.GetAsync($"/api/products/{createdProduct!.Id}");

        // Middleware maps UnauthorizedAccessException to 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProduct_ShouldReturn404_WhenNotFound()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn200_WhenOwner()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Old Name", Description = "Test", Status = "Active", BasePrice = 10m
        });
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var updateRequest = new UpdateProductRequest
        {
            Name = "New Name"
        };

        var response = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ProductResponse>();
        content!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn403_WhenNotOwner()
    {
        var (ownerEmail, ownerPassword, _) = await _factory.SeedDatabaseAsync();
        var ownerToken = await AuthHelper.GetTokenAsync(_client, ownerEmail, ownerPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Owner Product", Description = "Test", Status = "Active", BasePrice = 10m
        });
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var response = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", new UpdateProductRequest { Name = "Hacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturn204_WhenOwner()
    {
        var (email, password, _) = await _factory.SeedDatabaseAsync();
        var token = await AuthHelper.GetTokenAsync(_client, email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "To Delete", Description = "Test", Status = "Active", BasePrice = 10m
        });
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturn403_WhenNotOwner()
    {
        var (ownerEmail, ownerPassword, _) = await _factory.SeedDatabaseAsync();
        var ownerToken = await AuthHelper.GetTokenAsync(_client, ownerEmail, ownerPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Owner Product", Description = "Test", Status = "Active", BasePrice = 10m
        });
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var (otherEmail, otherPassword, _) = await _factory.SeedDatabaseAsync();
        var otherToken = await AuthHelper.GetTokenAsync(_client, otherEmail, otherPassword);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var response = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

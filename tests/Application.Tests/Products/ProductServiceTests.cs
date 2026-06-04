using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Application.Services;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Domain.Enums;

namespace MultiVendor.Ecommerce.Application.Tests.Products;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository>      _repositoryMock      = new();
    private readonly Mock<ICurrentMerchantService> _currentMerchantMock = new();
    private readonly Mock<IDistributedCache>       _cacheMock           = new();
    private readonly ProductService _sut;

    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly Guid ProductId  = Guid.NewGuid();

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _repositoryMock.Object,
            _currentMerchantMock.Object,
            _cacheMock.Object);

        _currentMerchantMock.Setup(x => x.MerchantId).Returns(MerchantId);

        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _cacheMock
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Product OwnedProduct(string name = "Product") => new()
    {
        Id          = ProductId,
        MerchantId  = MerchantId,
        Name        = name,
        Description = "desc",
        Status      = ProductStatus.Active,
        BasePrice   = 99.99m
    };

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExistsInDb()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());

        var result = await _sut.GetByIdAsync(ProductId);

        result.Id.Should().Be(ProductId);
        result.Name.Should().Be("Product");
        result.BasePrice.Should().Be(99.99m);
        result.MerchantId.Should().Be(MerchantId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCachedProduct_WhenCacheHit()
    {
        var cached = new ProductResponse { Id = ProductId, MerchantId = MerchantId, Name = "Cached" };
        var bytes  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cached));

        _cacheMock
            .Setup(x => x.GetAsync($"product:{ProductId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await _sut.GetByIdAsync(ProductId);

        result.Name.Should().Be("Cached");
        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync((Product?)null);

        var act = async () => await _sut.GetByIdAsync(ProductId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowUnauthorizedAccessException_WhenMerchantDoesNotOwnProduct()
    {
        var product = OwnedProduct();
        product.MerchantId = Guid.NewGuid();  // different merchant
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(product);

        var act = async () => await _sut.GetByIdAsync(ProductId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not own this product.");
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResult_WithMappedItems()
    {
        var products = Enumerable.Range(1, 3)
            .Select(i => new Product { Id = Guid.NewGuid(), MerchantId = MerchantId, Name = $"P{i}", Status = ProductStatus.Active })
            .ToList();

        _repositoryMock
            .Setup(x => x.GetAllAsync(MerchantId, It.IsAny<ProductListRequest>()))
            .ReturnsAsync((products, 3));

        var result = await _sut.GetAllAsync(new ProductListRequest { Page = 1, PageSize = 10 });

        result.TotalCount.Should().Be(3);
        result.Data.Should().HaveCount(3);
        result.TotalPages.Should().Be(1);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnProductResponse_WhenCreationSucceeds()
    {
        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        var request = new CreateProductRequest
        {
            Name      = "New Product",
            Status    = "Active",
            BasePrice = 49.99m
        };

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("New Product");
        result.MerchantId.Should().Be(MerchantId);
        result.BasePrice.Should().Be(49.99m);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenStatusIsInvalid()
    {
        var act = async () => await _sut.CreateAsync(new CreateProductRequest
        {
            Name      = "Bad",
            Status    = "NotAStatus",
            BasePrice = 10m
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid status value*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ShouldCallCacheRemove_WhenUpdateIsSuccessful()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        await _sut.UpdateAsync(ProductId, new UpdateProductRequest { Name = "Updated" });

        _cacheMock.Verify(
            x => x.RemoveAsync($"product:{ProductId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowUnauthorizedAccessException_WhenMerchantDoesNotOwnProduct()
    {
        var product = OwnedProduct();
        product.MerchantId = Guid.NewGuid();
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(product);

        var act = async () => await _sut.UpdateAsync(ProductId, new UpdateProductRequest { Name = "X" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ShouldCallCacheRemove_WhenDeletionIsSuccessful()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());
        _repositoryMock.Setup(x => x.DeleteAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(ProductId);

        _cacheMock.Verify(
            x => x.RemoveAsync($"product:{ProductId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowUnauthorizedAccessException_WhenMerchantDoesNotOwnProduct()
    {
        var product = OwnedProduct();
        product.MerchantId = Guid.NewGuid();
        _repositoryMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(product);

        var act = async () => await _sut.DeleteAsync(ProductId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not own this product.");

        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Product>()), Times.Never);
    }
}

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Application.Services;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Domain.Enums;

namespace MultiVendor.Ecommerce.Application.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICurrentMerchantService> _currentMerchantMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly ProductService _sut;

    private static readonly Guid MerchantId  = Guid.NewGuid();
    private static readonly Guid ProductId   = Guid.NewGuid();

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _repositoryMock.Object,
            _currentMerchantMock.Object,
            _cacheMock.Object);

        _currentMerchantMock
            .Setup(x => x.MerchantId)
            .Returns(MerchantId);

        // Default: cache miss — GetStringAsync is an extension over GetAsync
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _cacheMock
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var product = new Product
        {
            Id          = ProductId,
            MerchantId  = MerchantId,
            Name        = "Test Product",
            Description = "A test product",
            Status      = ProductStatus.Active,
            BasePrice   = 99.99m
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ProductId))
            .ReturnsAsync(product);

        // Act
        var result = await _sut.GetByIdAsync(ProductId);

        // Assert
        result.Id.Should().Be(ProductId);
        result.Name.Should().Be("Test Product");
        result.BasePrice.Should().Be(99.99m);
        result.MerchantId.Should().Be(MerchantId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowUnauthorizedAccessException_WhenMerchantDoesNotOwnProduct()
    {
        // Arrange
        var anotherMerchantId = Guid.NewGuid();

        var product = new Product
        {
            Id         = ProductId,
            MerchantId = anotherMerchantId,
            Name       = "Someone Else's Product"
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ProductId))
            .ReturnsAsync(product);

        // Act
        var act = async () => await _sut.DeleteAsync(ProductId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not own this product.");

        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCallCacheRemove_WhenUpdateIsSuccessful()
    {
        // Arrange
        var product = new Product
        {
            Id          = ProductId,
            MerchantId  = MerchantId,
            Name        = "Original Name",
            Description = "Original description",
            Status      = ProductStatus.Draft,
            BasePrice   = 50m
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ProductId))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateProductRequest { Name = "Updated Name" };

        // Act
        await _sut.UpdateAsync(ProductId, request);

        // Assert
        _cacheMock.Verify(
            x => x.RemoveAsync($"product:{ProductId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

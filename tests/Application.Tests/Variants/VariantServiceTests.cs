using FluentAssertions;
using Moq;
using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Application.Events;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Application.Services;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Domain.Enums;

namespace MultiVendor.Ecommerce.Application.Tests.Variants;

public class VariantServiceTests
{
    private readonly Mock<IVariantRepository>           _variantRepoMock     = new();
    private readonly Mock<IProductRepository>           _productRepoMock     = new();
    private readonly Mock<ICurrentMerchantService>      _currentMerchantMock = new();
    private readonly Mock<IEventHandler<LowStockEvent>> _lowStockHandlerMock = new();
    private readonly VariantService _sut;

    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly Guid ProductId  = Guid.NewGuid();
    private static readonly Guid VariantId  = Guid.NewGuid();

    public VariantServiceTests()
    {
        _lowStockHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<LowStockEvent>()))
            .Returns(Task.CompletedTask);

        _sut = new VariantService(
            _variantRepoMock.Object,
            _productRepoMock.Object,
            _currentMerchantMock.Object,
            _lowStockHandlerMock.Object);

        _currentMerchantMock.Setup(x => x.MerchantId).Returns(MerchantId);
    }

    private Product OwnedProduct() => new()
    {
        Id         = ProductId,
        MerchantId = MerchantId,
        Name       = "T-Shirt",
        Status     = ProductStatus.Active
    };

    private ProductVariant VariantWithAttributes(params Guid[] optionIds) => new()
    {
        Id              = VariantId,
        ProductId       = ProductId,
        SKU             = "SKU-001",
        Quantity        = 10,
        IsActive        = true,
        AttributeValues = optionIds
            .Select(id => new VariantAttributeValue { AttributeOptionId = id })
            .ToList()
    };

    // ── Merchant isolation ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldThrowUnauthorizedAccessException_WhenMerchantDoesNotOwnParentProduct()
    {
        var product = OwnedProduct();
        product.MerchantId = Guid.NewGuid(); // different merchant
        _productRepoMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(product);

        var act = async () => await _sut.GetByIdAsync(ProductId, VariantId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not own this product.");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowUnauthorizedAccessException_WhenMerchantDoesNotOwnProduct()
    {
        var product = OwnedProduct();
        product.MerchantId = Guid.NewGuid();
        _productRepoMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(product);

        var act = async () => await _sut.DeleteAsync(ProductId, VariantId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _variantRepoMock.Verify(x => x.DeleteAsync(It.IsAny<ProductVariant>()), Times.Never);
    }

    // ── Dynamic attributes ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldCreateVariantWithMultipleDynamicAttributes()
    {
        // Arrange — simulate T-Shirt variant with Color + Size (2 dynamic attributes)
        _productRepoMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());
        _variantRepoMock.Setup(x => x.SkuExistsAsync("TSHIRT-RED-M", null)).ReturnsAsync(false);

        ProductVariant? saved = null;
        _variantRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<ProductVariant>()))
            .Callback<ProductVariant>(v => saved = v)
            .ReturnsAsync((ProductVariant v) => v);

        var colorOptionId = Guid.NewGuid();
        var sizeOptionId  = Guid.NewGuid();

        _variantRepoMock
            .Setup(x => x.GetByIdWithAttributesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => new ProductVariant
            {
                Id              = VariantId,
                ProductId       = ProductId,
                SKU             = "TSHIRT-RED-M",
                Quantity        = 30,
                IsActive        = true,
                AttributeValues = new List<VariantAttributeValue>
                {
                    new() { AttributeOptionId = colorOptionId },
                    new() { AttributeOptionId = sizeOptionId }
                }
            });

        var request = new CreateVariantRequest
        {
            SKU        = "TSHIRT-RED-M",
            Quantity   = 30,
            IsActive   = true,
            Attributes = new List<VariantAttributeRequest>
            {
                new() { AttributeOptionId = colorOptionId },
                new() { AttributeOptionId = sizeOptionId }
            }
        };

        // Act
        var result = await _sut.CreateAsync(ProductId, request);

        // Assert — 2 dynamic attribute links stored, no hardcoded columns
        Assert.NotNull(saved);
        saved!.AttributeValues.Should().HaveCount(2);
        saved.AttributeValues.Select(av => av.AttributeOptionId)
            .Should().BeEquivalentTo(new[] { colorOptionId, sizeOptionId });

        result.SKU.Should().Be("TSHIRT-RED-M");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenSkuAlreadyExists()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());
        _variantRepoMock.Setup(x => x.SkuExistsAsync("DUPE-SKU", null)).ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(ProductId, new CreateVariantRequest
        {
            SKU        = "DUPE-SKU",
            Quantity   = 5,
            Attributes = new List<VariantAttributeRequest>()
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SKU*already exists*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ShouldReplaceAttributes_WhenAttributesProvided()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());

        var oldOptionId = Guid.NewGuid();
        var newOptionId = Guid.NewGuid();

        var existing = VariantWithAttributes(oldOptionId);

        _variantRepoMock
            .Setup(x => x.GetByIdWithAttributesAsync(VariantId))
            .ReturnsAsync(existing);

        _variantRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<ProductVariant>()))
            .Returns(Task.CompletedTask);

        _variantRepoMock
            .Setup(x => x.GetByIdWithAttributesAsync(VariantId))
            .ReturnsAsync(() => VariantWithAttributes(newOptionId));

        var request = new UpdateVariantRequest
        {
            Attributes = new List<VariantAttributeRequest>
            {
                new() { AttributeOptionId = newOptionId }
            }
        };

        await _sut.UpdateAsync(ProductId, VariantId, request);

        _variantRepoMock.Verify(x => x.UpdateAsync(
            It.Is<ProductVariant>(v =>
                v.AttributeValues.Count == 1 &&
                v.AttributeValues.First().AttributeOptionId == newOptionId)),
            Times.Once);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResult_ForOwnedProduct()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(OwnedProduct());

        var variants = Enumerable.Range(1, 5)
            .Select(i => new ProductVariant
            {
                Id              = Guid.NewGuid(),
                ProductId       = ProductId,
                SKU             = $"SKU-{i:D3}",
                Quantity        = i * 10,
                IsActive        = true,
                AttributeValues = new List<VariantAttributeValue>()
            })
            .ToList();

        _variantRepoMock
            .Setup(x => x.GetAllByProductAsync(ProductId, It.IsAny<VariantListRequest>()))
            .ReturnsAsync((variants, 5));

        var result = await _sut.GetAllAsync(ProductId, new VariantListRequest { Page = 1, PageSize = 10 });

        result.TotalCount.Should().Be(5);
        result.Data.Should().HaveCount(5);
    }
}

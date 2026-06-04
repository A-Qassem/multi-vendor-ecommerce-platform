using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendor.Ecommerce.Application.Common;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Application.Interfaces;

namespace MultiVendor.Ecommerce.Api.Controllers;

/// <summary>Manages products owned by the authenticated merchant.</summary>
[Authorize]
[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Get a paginated list of products for the authenticated merchant.</summary>
    /// <response code="200">Returns a paged list of products.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ProductListRequest request,
        CancellationToken ct)
    {
        var result = await _productService.GetAllAsync(request);
        return Ok(result);
    }

    /// <summary>Get a single product by ID.</summary>
    /// <response code="200">Returns the requested product.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(product);
    }

    /// <summary>Create a new product.</summary>
    /// <response code="201">Product created. Location header points to the new resource.</response>
    /// <response code="400">Invalid request data (e.g. unknown status value).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Update an existing product. Only provided fields are changed.</summary>
    /// <response code="200">Returns the updated product.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.UpdateAsync(id, request);
        return Ok(product);
    }

    /// <summary>Soft delete a product. The product is hidden but not removed from the database.</summary>
    /// <response code="204">Product deleted successfully.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }
}

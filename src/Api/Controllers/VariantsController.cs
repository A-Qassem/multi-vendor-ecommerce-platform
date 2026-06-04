using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendor.Ecommerce.Application.Common;
using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Application.Interfaces;

namespace MultiVendor.Ecommerce.Api.Controllers;

/// <summary>Manages variants for a specific product owned by the authenticated merchant.</summary>
[Authorize]
[Produces("application/json")]
[Consumes("application/json")]
[ApiController]
[Route("api/products/{productId:guid}/variants")]
public class VariantsController : ControllerBase
{
    private readonly IVariantService _variantService;

    public VariantsController(IVariantService variantService)
    {
        _variantService = variantService;
    }

    /// <summary>Get a paginated list of variants for a product.</summary>
    /// <response code="200">Returns a paged list of variants.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VariantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        Guid productId,
        [FromQuery] VariantListRequest request,
        CancellationToken ct)
    {
        var result = await _variantService.GetAllAsync(productId, request);
        return Ok(result);
    }

    /// <summary>Get a single variant by ID.</summary>
    /// <response code="200">Returns the requested variant with full attribute details.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product or variant not found.</response>
    [HttpGet("{variantId:guid}")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid productId, Guid variantId, CancellationToken ct)
    {
        var variant = await _variantService.GetByIdAsync(productId, variantId);
        return Ok(variant);
    }

    /// <summary>Create a new variant for a product.</summary>
    /// <response code="201">Variant created. Location header points to the new resource.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="409">SKU already exists across all variants.</response>
    [HttpPost]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid productId,
        [FromBody] CreateVariantRequest request,
        CancellationToken ct)
    {
        var variant = await _variantService.CreateAsync(productId, request);
        return CreatedAtAction(nameof(GetById),
            new { productId, variantId = variant.Id }, variant);
    }

    /// <summary>Update an existing variant. Only provided fields are changed. Replaces the full attributes list if provided.</summary>
    /// <response code="200">Returns the updated variant.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product or variant not found.</response>
    /// <response code="409">New SKU already exists on another variant.</response>
    [HttpPut("{variantId:guid}")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid productId,
        Guid variantId,
        [FromBody] UpdateVariantRequest request,
        CancellationToken ct)
    {
        var variant = await _variantService.UpdateAsync(productId, variantId, request);
        return Ok(variant);
    }

    /// <summary>Soft delete a variant. The variant is hidden but not removed from the database.</summary>
    /// <response code="204">Variant deleted successfully.</response>
    /// <response code="403">Product belongs to another merchant.</response>
    /// <response code="404">Product or variant not found.</response>
    [HttpDelete("{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid productId, Guid variantId, CancellationToken ct)
    {
        await _variantService.DeleteAsync(productId, variantId);
        return NoContent();
    }
}

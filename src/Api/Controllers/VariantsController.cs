using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Application.Interfaces;

namespace MultiVendor.Ecommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/products/{productId:guid}/variants")]
public class VariantsController : ControllerBase
{
    private readonly IVariantService _variantService;

    public VariantsController(IVariantService variantService)
    {
        _variantService = variantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid productId,
        [FromQuery] VariantListRequest request,
        CancellationToken ct)
    {
        var result = await _variantService.GetAllAsync(productId, request);
        return Ok(result);
    }

    [HttpGet("{variantId:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid variantId, CancellationToken ct)
    {
        var variant = await _variantService.GetByIdAsync(productId, variantId);
        return Ok(variant);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid productId,
        [FromBody] CreateVariantRequest request,
        CancellationToken ct)
    {
        var variant = await _variantService.CreateAsync(productId, request);
        return CreatedAtAction(nameof(GetById),
            new { productId, variantId = variant.Id }, variant);
    }

    [HttpPut("{variantId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        Guid variantId,
        [FromBody] UpdateVariantRequest request,
        CancellationToken ct)
    {
        var variant = await _variantService.UpdateAsync(productId, variantId, request);
        return Ok(variant);
    }

    [HttpDelete("{variantId:guid}")]
    public async Task<IActionResult> Delete(Guid productId, Guid variantId, CancellationToken ct)
    {
        await _variantService.DeleteAsync(productId, variantId);
        return NoContent();
    }
}

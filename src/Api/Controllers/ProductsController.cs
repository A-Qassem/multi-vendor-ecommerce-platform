using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Application.Interfaces;

namespace MultiVendor.Ecommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ProductListRequest request,
        CancellationToken ct)
    {
        var result = await _productService.GetAllAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.UpdateAsync(id, request);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }
}

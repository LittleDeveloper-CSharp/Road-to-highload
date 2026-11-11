using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Api.Inventory;

namespace Shop.Api.Controllers;

[ApiController]
[Authorize]
[Route("products")]
public class ProductsController(ProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await products.ListAsync(ct));
}

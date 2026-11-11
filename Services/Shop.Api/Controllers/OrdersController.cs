using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Api.Domain.Enums;
using Shop.Api.Models;
using Shop.Api.Ordering;

namespace Shop.Api.Controllers;

[ApiController]
[Authorize]
[Route("orders")]
public class OrdersController(OrderService orders, ICheckoutService checkout) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (order, error) = await orders.CreateAsync(userId, request.ProductId, request.Quantity, ct);
        return error switch
        {
            CreateOrderError.ProductNotFound => NotFound(new { error = "Product not found" }),
            CreateOrderError.InvalidQuantity => BadRequest(new { error = "Quantity must be positive" }),
            _ => Created($"/orders/{order!.Id}", new { order.Id, order.Status, order.ProductId, order.Quantity })
        };
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (ok, error, order) = await checkout.CheckoutAsync(id, userId, ct);
        if (order is null)
            return NotFound(new { error });

        if (!ok)
            return Conflict(new { error, order.Id, order.Status });

        return Ok(new { order.Id, order.Status, order.ProductId, order.Quantity });
    }
}

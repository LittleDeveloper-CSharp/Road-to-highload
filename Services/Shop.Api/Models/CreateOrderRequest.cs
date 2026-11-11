namespace Shop.Api.Models;

public record CreateOrderRequest(Guid ProductId, int Quantity);

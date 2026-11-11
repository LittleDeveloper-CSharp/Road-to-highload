using Shop.Api.Domain.Enums;

namespace Shop.Api.Domain;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
}

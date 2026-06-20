using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.Features.Orders.Dtos;

public record CreateOrderItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, 10_000)]
    public int Quantity { get; init; }
}

public record CreateOrderRequest
{
    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedDate,
    IReadOnlyList<OrderItemResponse> Items);

public enum OrderStatusAction
{
    Pay = 1,
    Ship = 2,
    Deliver = 3,
    Cancel = 4
}

public record UpdateOrderStatusRequest
{
    [Required]
    public OrderStatusAction Action { get; init; }
}

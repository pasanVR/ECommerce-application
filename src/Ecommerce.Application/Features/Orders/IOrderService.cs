using Ecommerce.Application.Features.Orders.Dtos;

namespace Ecommerce.Application.Features.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken ct = default);
    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);
}

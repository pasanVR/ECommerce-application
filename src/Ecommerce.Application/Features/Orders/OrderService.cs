using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Features.Orders.Dtos;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Orders;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public OrderService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var customerId = _currentUser.UserId
            ?? throw new DomainException("Authenticated user could not be resolved.");

        if (request.Items.Count == 0)
            throw new DomainException("An order must contain at least one item.");

        var requestedQuantities = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var productIds = requestedQuantities.Keys.ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var order = new Order { CustomerId = customerId };

        foreach (var (productId, quantity) in requestedQuantities)
        {
            if (!products.TryGetValue(productId, out var product))
                throw new NotFoundException(nameof(Product), productId);

            product.ReduceStock(quantity);

            order.AddItem(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(order.Id, ct);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(CancellationToken ct = default)
    {
        var customerId = _currentUser.UserId
            ?? throw new DomainException("Authenticated user could not be resolved.");

        return await QueryOrders()
            .Where(o => o.CustomerId == customerId)
            .Select(o => Map(o))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await QueryOrders()
            .Select(o => Map(o))
            .ToListAsync(ct);
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await QueryOrders().FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(Order), id);

        if (!_currentUser.IsAdmin && order.CustomerId != _currentUser.UserId)
            throw new NotFoundException(nameof(Order), id);

        return Map(order);
    }

    public async Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(Order), id);

        switch (request.Action)
        {
            case OrderStatusAction.Pay:
                order.MarkAsPaid();
                break;
            case OrderStatusAction.Ship:
                order.Ship();
                break;
            case OrderStatusAction.Deliver:
                order.Deliver();
                break;
            case OrderStatusAction.Cancel:
                order.Cancel();
                await RestoreStockAsync(order, ct);
                break;
            default:
                throw new DomainException($"Unknown order action: {request.Action}.");
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdInternalAsync(order.Id, ct);
    }

    private async Task RestoreStockAsync(Order order, CancellationToken ct)
    {
        var productIds = order.Items.Select(i => i.ProductId).ToList();

        var products = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var item in order.Items)
            if (products.TryGetValue(item.ProductId, out var product))
                product.Stock += item.Quantity;
    }

    private async Task<OrderResponse> GetByIdInternalAsync(Guid id, CancellationToken ct)
    {
        var order = await QueryOrders().FirstAsync(o => o.Id == id, ct);
        return Map(order);
    }

    private IQueryable<Order> QueryOrders() =>
        _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedDate);

    private static OrderResponse Map(Order o) => new(
        o.Id,
        o.CustomerId,
        o.Customer?.FullName ?? string.Empty,
        o.Status.ToString(),
        o.TotalAmount,
        o.CreatedDate,
        o.Items.Select(i => new OrderItemResponse(
            i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal)).ToList());
}

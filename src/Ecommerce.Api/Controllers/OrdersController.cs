using Ecommerce.Application.Features.Orders;
using Ecommerce.Application.Features.Orders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>Creates an order for the current customer.</summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await _orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Returns the current customer's own orders.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMine(CancellationToken ct)
        => Ok(await _orders.GetMyOrdersAsync(ct));

    /// <summary>Returns every order in the system. Admin only.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken ct)
        => Ok(await _orders.GetAllAsync(ct));

    /// <summary>Returns a single order (own order for customers, any for admins).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _orders.GetByIdAsync(id, ct));

    /// <summary>Advances or cancels an order through its lifecycle. Admin only.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken ct)
        => Ok(await _orders.UpdateStatusAsync(id, request, ct));
}

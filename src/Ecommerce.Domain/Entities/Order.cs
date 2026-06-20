using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public void AddItem(OrderItem item)
    {
        Items.Add(item);
        RecalculateTotal();
    }

    public void RecalculateTotal() => TotalAmount = Items.Sum(i => i.LineTotal);

    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Only a Pending order can be paid. Current status: {Status}.");
        Status = OrderStatus.Paid;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException($"Cannot ship an order that is not Paid. Current status: {Status}.");
        Status = OrderStatus.Shipped;
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainException($"Cannot deliver an order that has not been Shipped. Current status: {Status}.");
        Status = OrderStatus.Delivered;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new DomainException($"Cannot cancel an order once it has been Shipped. Current status: {Status}.");
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");
        Status = OrderStatus.Cancelled;
    }
}

using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (quantity > Stock)
            throw new DomainException($"Insufficient stock for '{Name}'. Available: {Stock}, requested: {quantity}.");

        Stock -= quantity;
    }

    public void Deactivate() => IsActive = false;
}

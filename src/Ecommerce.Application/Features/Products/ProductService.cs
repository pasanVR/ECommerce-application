using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Features.Products.Dtos;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Products;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;

    public ProductService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedDate)
            .Select(p => Map(p))
            .ToListAsync(ct);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return Map(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return Map(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;

        await _db.SaveChangesAsync(ct);
        return Map(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Deactivate();
        await _db.SaveChangesAsync(ct);
    }

    private static ProductResponse Map(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CreatedDate);
}

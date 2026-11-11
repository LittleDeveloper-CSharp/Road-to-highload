using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Inventory;

public class ProductService(AppDbContext db)
{
    public async Task<IReadOnlyList<ProductListItem>> ListAsync(CancellationToken ct)
    {
        return await db.Products
            .Select(p => new ProductListItem(p.Id, p.Name, p.Stock, p.Price))
            .ToListAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class SafeRepository(PettyCashDbContext context) : ISafeRepository
{
    public async Task<Safe?> GetByIdAsync(int id)
    {
        return await context.Safes
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IReadOnlyList<Safe>> GetAllAsync()
    {
        return await context.Safes
            .Include(s => s.Transactions)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Safe safe)
    {
        context.Safes.Add(safe);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Safe safe)
    {
        await context.SaveChangesAsync();
    }
}

using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface ICashBagRepository
{
    Task<CashBag?> GetByIdAsync(int id);
    Task<IReadOnlyList<CashBag>> GetAllAsync();
    Task AddAsync(CashBag bag);
    Task UpdateAsync(CashBag bag);
}

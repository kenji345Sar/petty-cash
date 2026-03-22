namespace PettyCash.Domain.Shared.Services;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}

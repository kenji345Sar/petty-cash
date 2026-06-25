using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.Tests.Transactions;

public class ReversePettyCashTransactionUseCaseTests
{
    private readonly Mock<IPettyCashTransactionRepository> _txRepo = new();
    private readonly Mock<ISafeRepository> _safeRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();
    private readonly Mock<IBalanceService> _balanceService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static ReverseTransactionRequestDto EmptyDesc => new("");

    private ReversePettyCashTransactionUseCase CreateUseCase() =>
        new(_txRepo.Object, _safeRepo.Object, _seqService.Object, _balanceService.Object, _unitOfWork.Object);

    private Safe MakeSafeWithBalance(int balance)
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(0, balance); // pettyCashBalance
        return safe;
    }

    // ── 正常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 入金取引の赤伝_出金取引が保存される()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Deposit, 5000, "入金", DateTime.UtcNow);
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(50000));

        var result = await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        Assert.Equal("Withdrawal", result.Type);
        Assert.Equal(5000, result.Amount);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<PettyCashTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 出金取引の赤伝_入金取引が保存される()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Withdrawal, 3000, "出金", DateTime.UtcNow);
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);

        var result = await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        Assert.Equal("Deposit", result.Type);
        Assert.Equal(3000, result.Amount);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<PettyCashTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 出金赤伝時はsafeRepoGetByIdが呼ばれない()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Withdrawal, 3000, "出金", DateTime.UtcNow);
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);

        await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        _safeRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ── 異常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 元取引未存在でKeyNotFoundExceptionでAddは呼ばれない()
    {
        _txRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PettyCashTransaction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99, EmptyDesc));

        _txRepo.Verify(r => r.AddAsync(It.IsAny<PettyCashTransaction>()), Times.Never);
    }

    [Fact]
    public async Task 入金赤伝で残高不足なら例外でAddは呼ばれない()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Deposit, 5000, "入金", DateTime.UtcNow);
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(1000)); // 5000不足

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1, EmptyDesc));

        _txRepo.Verify(r => r.AddAsync(It.IsAny<PettyCashTransaction>()), Times.Never);
    }

    // ── 保存有無 ──────────────────────────────────────────────────

    [Fact]
    public async Task 正常時はunitOfWorkSaveが呼ばれる()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Withdrawal, 3000, "出金", DateTime.UtcNow);
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);

        await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task 残高不足例外時はunitOfWorkSaveが呼ばれない()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Deposit, 5000, "入金", DateTime.UtcNow);
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(500));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1, EmptyDesc));

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

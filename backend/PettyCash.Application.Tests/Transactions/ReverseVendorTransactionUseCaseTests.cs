using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.Tests.Transactions;

public class ReverseVendorTransactionUseCaseTests
{
    private readonly Mock<IVendorTransactionRepository> _txRepo = new();
    private readonly Mock<ISafeRepository> _safeRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();
    private readonly Mock<IBalanceService> _balanceService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Denomination Denom10000 => new(1, 0, 0, 0, 0, 0, 0, 0, 0);
    private static ReverseTransactionRequestDto EmptyDesc => new("");
    private static ReverseTransactionRequestDto WithDesc(string d) => new(d);

    private ReverseVendorTransactionUseCase CreateUseCase() =>
        new(_txRepo.Object, _safeRepo.Object, _seqService.Object, _balanceService.Object, _unitOfWork.Object);

    // 入金取引（Deposit）→ 赤伝は出金（Withdrawal）
    private static VendorTransaction MakeDepositTx(int safeId = 1)
    {
        var bag = ChangeBag.CreateDeposit(safeId, 0, "テスト", DateTime.UtcNow, Denom10000);
        return bag.DepositTransaction!;
    }

    // 出金取引（Withdrawal）→ 赤伝は入金（Deposit）
    private static VendorTransaction MakeWithdrawalTx(int safeId = 1)
    {
        var bag = ChangeBag.CreateDeposit(safeId, 0, "テスト", DateTime.UtcNow, Denom10000);
        return bag.MoveToRegister(DateTime.UtcNow);
    }

    private Safe MakeSafeWithBalance(int balance)
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(balance, 0);
        return safe;
    }

    // ── 正常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 入金取引の赤伝_出金取引が保存される()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDepositTx());
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(50000));

        var result = await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        Assert.Equal("Withdrawal", result.Type);
        Assert.Equal(10000, result.Amount);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 出金取引の赤伝_入金取引が保存される()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeWithdrawalTx());

        var result = await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        Assert.Equal("Deposit", result.Type);
        Assert.Equal(10000, result.Amount);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 摘要が指定された場合はその摘要が使われる()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeWithdrawalTx());

        var result = await CreateUseCase().ExecuteAsync(1, WithDesc("手動修正"));

        Assert.Equal("手動修正", result.Description);
    }

    [Fact]
    public async Task 摘要が空の場合はデフォルト摘要になる()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeWithdrawalTx());

        var result = await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        Assert.Contains("修正（赤伝）", result.Description);
    }

    // ── 残高チェック ──────────────────────────────────────────────

    [Fact]
    public async Task 入金赤伝時はsafeRepoGetByIdが呼ばれる()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDepositTx(safeId: 1));
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(50000));

        await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        _safeRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task 出金赤伝時はsafeRepoGetByIdが呼ばれない()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeWithdrawalTx());

        await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        _safeRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ── 異常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 元取引未存在でKeyNotFoundExceptionでAddは呼ばれない()
    {
        _txRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((VendorTransaction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99, EmptyDesc));

        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Never);
    }

    [Fact]
    public async Task 入金赤伝で残高不足なら例外でAddは呼ばれない()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDepositTx());
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(5000)); // 10000不足

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1, EmptyDesc));

        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Never);
    }

    // ── 保存有無 ──────────────────────────────────────────────────

    [Fact]
    public async Task 正常時はunitOfWorkSaveが呼ばれる()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeWithdrawalTx());

        await CreateUseCase().ExecuteAsync(1, EmptyDesc);

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task 残高不足例外時はunitOfWorkSaveが呼ばれない()
    {
        _txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDepositTx());
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSafeWithBalance(1000));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1, EmptyDesc));

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task 元取引未存在例外時はunitOfWorkSaveが呼ばれない()
    {
        _txRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((VendorTransaction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99, EmptyDesc));

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

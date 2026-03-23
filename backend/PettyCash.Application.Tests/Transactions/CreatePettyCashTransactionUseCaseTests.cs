using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.Tests.Transactions;

public class CreatePettyCashTransactionUseCaseTests
{
    private readonly Mock<IPettyCashTransactionRepository> _txRepo = new();
    private readonly Mock<ISafeRepository> _safeRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();

    private CreatePettyCashTransactionUseCase CreateUseCase() =>
        new(_txRepo.Object, _safeRepo.Object, _seqService.Object);

    [Fact]
    public async Task 入金が正常に作成される()
    {
        var dto = new CreatePettyCashTransactionRequestDto(1, "Deposit", 5000, "小口入金", DateTime.UtcNow);

        var result = await CreateUseCase().ExecuteAsync(dto);

        Assert.Equal("Deposit", result.Type);
        Assert.Equal(5000, result.Amount);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<PettyCashTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 出金時に残高チェックが行われる()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(3000, 2000); // 合計5000
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

        var dto = new CreatePettyCashTransactionRequestDto(1, "Withdrawal", 3000, "出金", DateTime.UtcNow);
        var result = await CreateUseCase().ExecuteAsync(dto);

        Assert.Equal("Withdrawal", result.Type);
        Assert.Equal(3000, result.Amount);
    }

    [Fact]
    public async Task 出金時に残高不足なら例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(1000, 1000); // 合計2000
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

        var dto = new CreatePettyCashTransactionRequestDto(1, "Withdrawal", 3000, "出金", DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(dto));
    }

    [Fact]
    public async Task 金種指定で金額が自動計算される()
    {
        var denomDto = new DenominationDto(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000円
        var dto = new CreatePettyCashTransactionRequestDto(1, "Deposit", 0, "テスト", DateTime.UtcNow, denomDto);

        var result = await CreateUseCase().ExecuteAsync(dto);

        Assert.Equal(5000, result.Amount);
        Assert.NotNull(result.Denomination);
    }

    [Fact]
    public async Task 入金時は残高チェックが行われない()
    {
        var dto = new CreatePettyCashTransactionRequestDto(1, "Deposit", 5000, "入金", DateTime.UtcNow);

        await CreateUseCase().ExecuteAsync(dto);

        _safeRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}

using Moq;
using PettyCash.Application.UseCases.Bags;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.Tests.Bags;

public class MoveBagToRegisterUseCaseTests
{
    private readonly Mock<IChangeBagRepository> _bagRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();

    private MoveBagToRegisterUseCase CreateUseCase() =>
        new(_bagRepo.Object, _seqService.Object);

    [Fact]
    public async Task レジ移動で出金取引が生成される()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var result = await CreateUseCase().ExecuteAsync(1);

        Assert.Equal("Withdrawal", result.Type);
        Assert.Equal(10000, result.Amount);
        _bagRepo.Verify(r => r.UpdateAsync(bag), Times.Once);
    }

    [Fact]
    public async Task バッグが見つからない場合は例外()
    {
        _bagRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ChangeBag?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99));
    }

    [Fact]
    public async Task 既にレジ移動済みなら例外()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        bag.MoveToRegister(DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1));
    }
}

using Moq;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;

namespace PettyCash.Application.Tests.PrepBags;

public class HandOverPrepBagUseCaseTests
{
    private readonly Mock<IPrepBagRepository> _prepRepo = new();
    private readonly Mock<IVendorTransactionRepository> _txRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();

    private HandOverPrepBagUseCase CreateUseCase() =>
        new(_prepRepo.Object, _txRepo.Object, _seqService.Object);

    [Fact]
    public async Task 引渡で出金取引が保存される()
    {
        var cb = CashBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        _prepRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prep);

        var result = await CreateUseCase().ExecuteAsync(1);

        Assert.Equal("HandedOver", result.Status);
        Assert.Equal(5000, result.TotalAmount);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 準備バッグが見つからない場合は例外()
    {
        _prepRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PrepBag?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99));
    }

    [Fact]
    public async Task 二重引渡は例外()
    {
        var cb = CashBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        prep.MarkHandedOver(DateTime.UtcNow);
        _prepRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prep);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1));
    }
}

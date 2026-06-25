using Moq;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.Tests.PrepBags;

public class CancelPrepBagUseCaseTests
{
    private readonly Mock<IPrepBagRepository> _prepRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Denomination Denom5000 => new(0, 1, 0, 0, 0, 0, 0, 0, 0);

    private CancelPrepBagUseCase CreateUseCase() =>
        new(_prepRepo.Object, _unitOfWork.Object);

    private static PrepBag CreatePreparingBag()
    {
        var cb = CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, Denom5000);
        return PrepBag.Create(1, [cb], DateTime.UtcNow);
    }

    // ── 正常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 正常キャンセルでステータスがCancelledになる()
    {
        var bag = CreatePreparingBag();
        _prepRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var result = await CreateUseCase().ExecuteAsync(1);

        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task 正常キャンセルでprepRepoUpdateとunitOfWorkSaveが呼ばれる()
    {
        var bag = CreatePreparingBag();
        _prepRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        await CreateUseCase().ExecuteAsync(1);

        _prepRepo.Verify(r => r.UpdateAsync(bag), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── 異常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 存在しないIDでKeyNotFoundExceptionでUpdateは呼ばれない()
    {
        _prepRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PrepBag?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99));

        _prepRepo.Verify(r => r.UpdateAsync(It.IsAny<PrepBag>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task 引渡済みはキャンセルできずSaveは呼ばれない()
    {
        var bag = CreatePreparingBag();
        bag.MarkHandedOver(DateTime.UtcNow); // HandedOver 状態にする
        _prepRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(1));

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

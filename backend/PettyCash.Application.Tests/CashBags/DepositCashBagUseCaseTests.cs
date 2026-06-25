using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.CashBags;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.Tests.CashBags;

public class DepositCashBagUseCaseTests
{
    private readonly Mock<ICashBagRepository> _bagRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();
    private readonly Mock<IBalanceService> _balanceService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DepositCashBagUseCase CreateUseCase() =>
        new(_bagRepo.Object, _seqService.Object, _balanceService.Object, _unitOfWork.Object);

    // ── 正常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 金種指定で正常作成されAddとSaveが呼ばれる()
    {
        var dto = new DepositRequestDto(
            SafeId: 1, Amount: 0, Description: "レジ売上", Date: DateTime.UtcNow,
            Denomination: new DenominationDto(0, 1, 3, 0, 0, 0, 0, 0, 0)); // 5000+3000=8000

        var result = await CreateUseCase().ExecuteAsync(dto);

        Assert.Equal(8000, result.TotalAmount);
        Assert.Equal("MovedToSafe", result.Status);
        _bagRepo.Verify(r => r.AddAsync(It.IsAny<CashBag>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── 異常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 金種なしで例外でAddは呼ばれない()
    {
        var dto = new DepositRequestDto(1, 0, "テスト", DateTime.UtcNow, null); // denomination=null

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateUseCase().ExecuteAsync(dto));

        _bagRepo.Verify(r => r.AddAsync(It.IsAny<CashBag>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task 金種合計ゼロで例外でAddは呼ばれない()
    {
        var dto = new DepositRequestDto(
            SafeId: 1, Amount: 0, Description: "テスト", Date: DateTime.UtcNow,
            Denomination: new DenominationDto(0, 0, 0, 0, 0, 0, 0, 0, 0)); // 全ゼロ

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateUseCase().ExecuteAsync(dto));

        _bagRepo.Verify(r => r.AddAsync(It.IsAny<CashBag>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

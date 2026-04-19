using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.Tests.DenominationChecks;

public class CheckCashBagUseCaseTests
{
    private readonly Mock<ICashBagRepository> _bagRepo = new();
    private readonly Mock<IVendorDenominationCheckRepository> _checkRepo = new();
    private readonly Mock<IVendorTransactionRepository> _txRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Denomination Denom5000 => new(0, 1, 0, 0, 0, 0, 0, 0, 0);

    private CheckCashBagUseCase CreateUseCase() =>
        new(_bagRepo.Object, _checkRepo.Object, _txRepo.Object, _seqService.Object, Mock.Of<IBalanceService>(), Mock.Of<IProjectionService>(), Mock.Of<IEventStore>(), _unitOfWork.Object);

    [Fact]
    public async Task 差額ありなら調整取引が保存される()
    {
        var bag = CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, Denom5000);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var dto = new DenominationCheckRequestDto(0, 0, 4, 0, 0, 0, 0, 0, 0); // 4000円
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(-1000, result.Difference);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 差額ゼロなら調整取引は作成されない()
    {
        var bag = CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, Denom5000);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var dto = new DenominationCheckRequestDto(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000円
        await CreateUseCase().ExecuteAsync(1, dto);

        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Never);
    }
}

using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;

namespace PettyCash.Application.Tests.DenominationChecks;

public class CheckCashBagUseCaseTests
{
    private readonly Mock<ICashBagRepository> _bagRepo = new();
    private readonly Mock<IDenominationCheckRepository> _checkRepo = new();
    private readonly Mock<IVendorTransactionRepository> _txRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();

    private CheckCashBagUseCase CreateUseCase() =>
        new(_bagRepo.Object, _checkRepo.Object, _txRepo.Object, _seqService.Object);

    [Fact]
    public async Task 差額ありなら調整取引が保存される()
    {
        var bag = CashBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var dto = new DenominationCheckRequestDto(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000円→一致
        // 4000円に変更
        dto = new DenominationCheckRequestDto(0, 0, 4, 0, 0, 0, 0, 0, 0); // 4000円
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(-1000, result.Difference);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 差額ゼロなら調整取引は作成されない()
    {
        var bag = CashBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var dto = new DenominationCheckRequestDto(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000円
        await CreateUseCase().ExecuteAsync(1, dto);

        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Never);
    }
}

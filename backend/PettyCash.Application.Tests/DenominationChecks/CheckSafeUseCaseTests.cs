using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.Tests.DenominationChecks;

public class CheckSafeUseCaseTests
{
    private readonly Mock<ISafeRepository> _safeRepo = new();
    private readonly Mock<IDenominationCheckRepository> _checkRepo = new();
    private readonly Mock<IVendorTransactionRepository> _txRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();

    private CheckSafeUseCase CreateUseCase() =>
        new(_safeRepo.Object, _checkRepo.Object, _txRepo.Object, _seqService.Object);

    private static Safe CreateSafe(int vendorBalance, int pettyCashBalance)
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(vendorBalance, pettyCashBalance);
        return safe;
    }

    [Fact]
    public async Task 差額ありなら調整取引が保存される()
    {
        var safe = CreateSafe(30000, 5000); // 合計35000
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

        // 37000円を実数として入力（2000円過剰）
        var dto = new DenominationCheckRequestDto(3, 0, 7, 0, 0, 0, 0, 0, 0); // 37000
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(37000, result.CheckedAmount);
        Assert.Equal(35000, result.ExpectedAmount);
        Assert.Equal(2000, result.Difference);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task 差額ゼロなら調整取引は作成されない()
    {
        var safe = CreateSafe(30000, 5000);
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

        var dto = new DenominationCheckRequestDto(3, 0, 5, 0, 0, 0, 0, 0, 0); // 35000
        await CreateUseCase().ExecuteAsync(1, dto);

        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Never);
    }

    [Fact]
    public async Task 金庫が見つからない場合は例外()
    {
        _safeRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Safe?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99, new DenominationCheckRequestDto(0, 0, 0, 0, 0, 0, 0, 0, 0)));
    }
}

using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.PettyCash.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.Tests.DenominationChecks;

public class CheckSafeUseCaseTests
{
    private readonly Mock<ISafeRepository> _safeRepo = new();
    private readonly Mock<IPettyCashDenominationCheckRepository> _checkRepo = new();
    private readonly Mock<IPettyCashTransactionRepository> _txRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CheckSafeUseCase CreateUseCase() =>
        new(_safeRepo.Object, _checkRepo.Object, _txRepo.Object, _seqService.Object, Mock.Of<IBalanceService>(), _unitOfWork.Object);

    private static Safe CreateSafe(int vendorBalance, int pettyCashBalance)
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(vendorBalance, pettyCashBalance);
        return safe;
    }

    [Fact]
    public async Task 帳簿額は小口残高で業者残高を含まない()
    {
        var safe = CreateSafe(48000, 17100); // 合計65100
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

        var dto = new DenominationCheckRequestDto(1, 1, 2, 0, 1, 0, 0, 0, 0); // 17100
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(17100, result.ExpectedAmount);
        Assert.Equal(0, result.Difference);
    }

    [Fact]
    public async Task 差額ありなら小口取引に調整が保存される()
    {
        var safe = CreateSafe(30000, 5000);
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);
        PettyCashTransaction? saved = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<PettyCashTransaction>()))
            .Callback<PettyCashTransaction>(t => saved = t)
            .Returns(Task.CompletedTask);

        // 7000円を実数として入力（2000円過剰）
        var dto = new DenominationCheckRequestDto(0, 1, 2, 0, 0, 0, 0, 0, 0); // 7000
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(7000, result.CheckedAmount);
        Assert.Equal(5000, result.ExpectedAmount);
        Assert.Equal(2000, result.Difference);
        Assert.NotNull(saved);
        Assert.Equal(TransactionType.Adjustment, saved!.Type);
        Assert.Equal(2000, saved.Amount);
    }

    [Fact]
    public async Task 差額ゼロなら調整取引は作成されない()
    {
        var safe = CreateSafe(30000, 5000);
        _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

        var dto = new DenominationCheckRequestDto(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000
        await CreateUseCase().ExecuteAsync(1, dto);

        _txRepo.Verify(r => r.AddAsync(It.IsAny<PettyCashTransaction>()), Times.Never);
    }

    [Fact]
    public async Task 金庫が見つからない場合は例外()
    {
        _safeRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Safe?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99, new DenominationCheckRequestDto(0, 0, 0, 0, 0, 0, 0, 0, 0)));
    }
}

public class UpdatePettyCashDenominationCheckUseCaseTests
{
    [Fact]
    public async Task 修正時の帳簿額も小口残高になる()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(48000, 17100);
        var check = PettyCashDenominationCheck.CreateForSafe(1, new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0), 17100, DateTime.UtcNow);

        var safeRepo = new Mock<ISafeRepository>();
        safeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(safe);
        var checkRepo = new Mock<IPettyCashDenominationCheckRepository>();
        checkRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(check);

        var useCase = new UpdatePettyCashDenominationCheckUseCase(checkRepo.Object, safeRepo.Object, Mock.Of<IUnitOfWork>());
        var result = await useCase.ExecuteAsync(5, new DenominationCheckRequestDto(1, 1, 2, 0, 1, 0, 0, 0, 0)); // 17100

        Assert.Equal(17100, result.ExpectedAmount);
        Assert.Equal(0, result.Difference);
    }
}

using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;

namespace PettyCash.Application.Tests.DenominationChecks;

public class CheckChangeBagUseCaseTests
{
    private readonly Mock<IChangeBagRepository> _bagRepo = new();
    private readonly Mock<IDenominationCheckRepository> _checkRepo = new();
    private readonly Mock<IVendorTransactionRepository> _txRepo = new();
    private readonly Mock<ISequenceNumberService> _seqService = new();

    private CheckChangeBagUseCase CreateUseCase() =>
        new(_bagRepo.Object, _checkRepo.Object, _txRepo.Object, _seqService.Object);

    private static DenominationCheckRequestDto MakeDenomDto(int count10000 = 0, int count1000 = 0) =>
        new(count10000, 0, count1000, 0, 0, 0, 0, 0, 0);

    [Fact]
    public async Task 差額ゼロなら調整取引は作成されない()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var dto = MakeDenomDto(count10000: 1); // 10000円 = 帳簿と一致
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(0, result.Difference);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Never);
    }

    [Fact]
    public async Task 差額ありなら調整取引が保存される()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        var dto = MakeDenomDto(count1000: 9); // 9000円 = 1000円不足
        var result = await CreateUseCase().ExecuteAsync(1, dto);

        Assert.Equal(-1000, result.Difference);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<VendorTransaction>()), Times.Once);
        _seqService.Verify(s => s.AssignAsync(It.IsAny<VendorTransaction>()), Times.Once);
    }

    [Fact]
    public async Task バッグが見つからない場合は例外()
    {
        _bagRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ChangeBag?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(99, MakeDenomDto()));
    }

    [Fact]
    public async Task 有高チェックが保存される()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        _bagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bag);

        await CreateUseCase().ExecuteAsync(1, MakeDenomDto(count10000: 1));

        _checkRepo.Verify(r => r.AddAsync(It.IsAny<DenominationCheck>()), Times.Once);
        _seqService.Verify(s => s.AssignAsync(It.IsAny<DenominationCheck>()), Times.Once);
    }
}

using Moq;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.Tests.PrepBags;

public class CreatePrepBagUseCaseTests
{
    private readonly Mock<ICashBagRepository> _cashBagRepo = new();
    private readonly Mock<IPrepBagRepository> _prepRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Denomination Denom5000 => new(0, 1, 0, 0, 0, 0, 0, 0, 0);

    private CreatePrepBagUseCase CreateUseCase() =>
        new(_cashBagRepo.Object, _prepRepo.Object, _unitOfWork.Object);

    // ── 正常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task 正常作成でprepRepoAddとunitOfWorkSaveが呼ばれる()
    {
        var cb = CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, Denom5000);
        _cashBagRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cb);
        var dto = new CreatePrepBagRequestDto(1, [1]);

        var result = await CreateUseCase().ExecuteAsync(dto);

        Assert.Equal("Preparing", result.Status);
        _prepRepo.Verify(r => r.AddAsync(It.IsAny<PrepBag>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── 異常系 ───────────────────────────────────────────────────

    [Fact]
    public async Task CashBag未存在でKeyNotFoundExceptionでprepRepoは呼ばれない()
    {
        _cashBagRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CashBag?)null);
        var dto = new CreatePrepBagRequestDto(1, [99]);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateUseCase().ExecuteAsync(dto));

        _prepRepo.Verify(r => r.AddAsync(It.IsAny<PrepBag>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CashBagIds空でArgumentExceptionでprepRepoは呼ばれない()
    {
        var dto = new CreatePrepBagRequestDto(1, []);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateUseCase().ExecuteAsync(dto));

        _prepRepo.Verify(r => r.AddAsync(It.IsAny<PrepBag>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

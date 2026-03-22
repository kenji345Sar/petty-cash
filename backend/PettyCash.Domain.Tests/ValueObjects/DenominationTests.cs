using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.ValueObjects;

public class DenominationTests
{
    [Fact]
    public void TotalAmount_各金種の合計が正しく計算される()
    {
        var denom = new Denomination(1, 1, 1, 1, 1, 1, 1, 1, 1);

        Assert.Equal(16666, denom.TotalAmount);
    }

    [Fact]
    public void TotalAmount_万札のみ()
    {
        var denom = new Denomination(3, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.Equal(30000, denom.TotalAmount);
    }

    [Fact]
    public void TotalAmount_小銭のみ()
    {
        var denom = new Denomination(0, 0, 0, 2, 3, 1, 5, 2, 10);

        Assert.Equal(1420, denom.TotalAmount);
    }

    [Fact]
    public void TotalAmount_全てゼロなら0円()
    {
        var denom = new Denomination(0, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.Equal(0, denom.TotalAmount);
    }

    [Fact]
    public void コンストラクタ_負の枚数は例外()
    {
        Assert.Throws<ArgumentException>(() =>
            new Denomination(-1, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void 同じ枚数のDenominationは等値()
    {
        var a = new Denomination(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var b = new Denomination(1, 2, 3, 4, 5, 6, 7, 8, 9);

        Assert.Equal(a, b);
    }

    [Fact]
    public void 異なる枚数のDenominationは非等値()
    {
        var a = new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0);
        var b = new Denomination(2, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.NotEqual(a, b);
    }
}

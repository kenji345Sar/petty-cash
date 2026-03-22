namespace PettyCash.Domain.Shared.ValueObjects;

public record Denomination
{
    public int Count10000 { get; }
    public int Count5000 { get; }
    public int Count1000 { get; }
    public int Count500 { get; }
    public int Count100 { get; }
    public int Count50 { get; }
    public int Count10 { get; }
    public int Count5 { get; }
    public int Count1 { get; }

    public Denomination(
        int count10000, int count5000, int count1000,
        int count500, int count100, int count50,
        int count10, int count5, int count1)
    {
        if (count10000 < 0 || count5000 < 0 || count1000 < 0 ||
            count500 < 0 || count100 < 0 || count50 < 0 ||
            count10 < 0 || count5 < 0 || count1 < 0)
        {
            throw new ArgumentException("金種の枚数は0以上である必要があります。");
        }

        Count10000 = count10000;
        Count5000 = count5000;
        Count1000 = count1000;
        Count500 = count500;
        Count100 = count100;
        Count50 = count50;
        Count10 = count10;
        Count5 = count5;
        Count1 = count1;
    }

    public int TotalAmount =>
        Count10000 * 10000
        + Count5000 * 5000
        + Count1000 * 1000
        + Count500 * 500
        + Count100 * 100
        + Count50 * 50
        + Count10 * 10
        + Count5 * 5
        + Count1 * 1;
}

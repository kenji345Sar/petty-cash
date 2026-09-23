# 出金の残高チェック — 修正前の状態

2026-09-23 に直した「出金してよい上限の判定」について、**直す前がどうなっていたか**だけを記録する。
どう直したかは、このファイルには書かない。

---

## 1. ドメイン — 判定は1種類しかなかった

`Safe` が持つ出金判定は `EnsureCanWithdraw` の1つだけで、**合計残高**と比べていた。

```csharp
// backend/PettyCash.Domain/Safe/Safe.cs
public void EnsureCanWithdraw(int amount)
{
    if (amount <= 0)
        throw new ArgumentException("出金額は1以上である必要があります。");
    if (amount > CurrentBalance)
        throw new InvalidOperationException(
            $"残高不足です。現在残高: {CurrentBalance}円、出金額: {amount}円");
}
```

`CurrentBalance` は、金庫に残高をセットするときに両方を足した値。

```csharp
public void SetBalances(int vendorBalance, int pettyCashBalance)
{
    VendorBalance = vendorBalance;
    PettyCashBalance = pettyCashBalance;
    CurrentBalance = vendorBalance + pettyCashBalance;   // 売上金 + 小口
}
```

クラスのコメントにも、どちらの残高で判定するかは書かれていなかった。

```
/// 【業務ルール】
///   - 金庫名は必須
///   - 残高は業者残高（VendorBalance）と小口残高（PettyCashBalance）に分かれる
///   - 合計残高（CurrentBalance）は両者の合算
///   - 残高を超える出金はできない        ← どの残高かを決めていない
```

## 2. 呼び出し側 — 3か所とも同じメソッドを呼んでいた

小口の出金も、売上金の出金も、区別なく同じ `EnsureCanWithdraw` を呼んでいた。

| UseCase | 業務 | 手順のどこ |
|---|---|---|
| CreatePettyCashTransactionUseCase | 小口の出金登録 | 取引を作った後、保存の前 |
| ReversePettyCashTransactionUseCase | 小口の赤伝（出金になる場合） | 赤伝を作った後、保存の前 |
| ReverseVendorTransactionUseCase | 売上金の赤伝（出金になる場合） | 赤伝を作った後、保存の前 |

```csharp
// CreatePettyCashTransactionUseCase.cs
if (type == TransactionType.Withdrawal)
{
    var safe = await safeRepository.GetByIdAsync(dto.SafeId)
        ?? throw new KeyNotFoundException($"金庫(ID={dto.SafeId})が見つかりません。");
    safe.EnsureCanWithdraw(transaction.Amount);
}
```

```csharp
// ReversePettyCashTransactionUseCase.cs / ReverseVendorTransactionUseCase.cs
if (reversal.Type == TransactionType.Withdrawal)
{
    var safe = await safeRepository.GetByIdAsync(original.SafeId)
        ?? throw new KeyNotFoundException($"金庫(ID={original.SafeId})が見つかりません。");
    safe.EnsureCanWithdraw(reversal.Amount);
}
```

## 3. その結果、画面ではこう動いていた

銀座の金庫（売上金 48,000円 / 小口 2,000円）で、小口タブから 3,000円を出金する場合。

```
画面に出ている小口残高    2,000円
判定に使われた残高       50,000円（48,000 + 2,000）
  → 3,000 ≦ 50,000 なので通る
登録後の小口残高        −1,000円
```

小口現金と売上金は別の出納なのに、売上金があるおかげで小口の出金が通っていた。
赤伝（入金の取り消し＝出金になる）でも同じことが起きていた。

## 4. テストも合計残高の前提で書かれていた

ドメインのテストは、合計残高で判定することを「正しい動き」として固定していた。

```csharp
[Fact]
public void EnsureCanWithdraw_残高内なら例外なし()
{
    var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
    safe.SetBalances(30000, 5000);      // 売上金 30,000 / 小口 5,000

    safe.EnsureCanWithdraw(35000);      // 合計ちょうど。例外が出なければOK
}

[Fact]
public void EnsureCanWithdraw_残高超過は例外()
{
    var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
    safe.SetBalances(30000, 5000);

    Assert.Throws<InvalidOperationException>(() =>
        safe.EnsureCanWithdraw(35001));  // 合計 +1円で初めて例外
}
```

UseCase 側のテストも同じで、小口 2,000円の金庫から 3,000円を出金して**成功することを期待**していた。

```csharp
[Fact]
public async Task 出金時に残高チェックが行われる()
{
    var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
    safe.SetBalances(3000, 2000);       // 合計5000
    _safeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(safe);

    var dto = new CreatePettyCashTransactionRequestDto(1, "Withdrawal", 3000, "出金", DateTime.UtcNow);
    var result = await CreateUseCase().ExecuteAsync(dto);

    Assert.Equal("Withdrawal", result.Type);   // 通ることを確認していた
    Assert.Equal(3000, result.Amount);
}
```

テストが通っていたのは、テスト自体が合計残高の前提で書かれていたため。

## 5. 仕様書にも書かれていなかった

`spec/03-petty-cash.md` の「6. 業務ルール」は見出しだけで中身が空だった。
出金の上限がどの残高かは、仕様としてどこにも書かれておらず、コードの `CurrentBalance` だけが事実だった。

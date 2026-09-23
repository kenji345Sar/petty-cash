# 現行構成から ES+CQRS にすると何が変わるか

現行コード（ES+CQRS なし）を基準に、ES+CQRS を入れたときに追加・変更される箇所をまとめる。
内容は、以前 petty-cash で実装していた ES+CQRS（2026-06-18 に削除）の構成に基づく。

---

## 1. 追加されるもの

| 層 | 追加されるもの | 役割 |
|---|---|---|
| Domain | `IEventStore` | 「イベントを記録できる」という契約 |
| Domain | `IProjectionService` | 「Read Model を更新できる」という契約 |
| Infrastructure | `Services/EventStore.cs` | `domain_events` テーブルに INSERT |
| Infrastructure | `Services/ProjectionService.cs` | Read Model テーブル（`*_ledger_view` / `safe_balances`）を更新 |
| Infrastructure | `ReadModels/` | Read Model のエンティティ（`DomainEvent`, `SafeBalance`, `VendorLedgerEntry`, `PettyCashLedgerEntry`） |

### 追加されるテーブル

| DbSet プロパティ | テーブル名 | 分類 |
|---|---|---|
| `DomainEvents` | `domain_events` | イベントストア（JSONB の payload） |
| `SafeBalances` | `safe_balances` | 残高（Read Model） |
| `VendorLedgerEntries` | `vendor_ledger_view` | 売上金出納帳（Read Model） |
| `PettyCashLedgerEntries` | `petty_cash_ledger_view` | 小口出納帳（Read Model） |

---

## 2. UseCase（書き込み）の変化

現行の書き込み UseCase は `Repository.AddAsync` の後に、そのまま `SaveChangesAsync` でコミットする。
ES+CQRS では、その間にイベント記録とプロジェクションが入る。

```csharp
// 現行
await sequenceNumberService.AssignAsync(transaction);
await balanceService.AssignBalanceAsync(transaction);
await transactionRepository.AddAsync(transaction);
await unitOfWork.SaveChangesAsync();

// ES+CQRS
await sequenceNumberService.AssignAsync(transaction);
await balanceService.AssignBalanceAsync(transaction);
await transactionRepository.AddAsync(transaction);
await eventStore.AppendAsync(transaction);          // 追加: イベント記録
await projectionService.ProjectAsync(transaction);  // 追加: Read Model 更新
await unitOfWork.SaveChangesAsync();                // すべて同一トランザクションでコミット
```

取引を生む UseCase すべてが対象になる。

| UseCase | 記録するイベント |
|---|---|
| DepositBagUseCase（両替金バッグ入金） | VendorMoneyDeposited |
| MoveBagToRegisterUseCase（両替金バッグ → レジ） | VendorMoneyWithdrawn |
| DepositCashBagUseCase（売上バッグ入金） | VendorMoneyDeposited |
| HandOverPrepBagUseCase（準備バッグ引渡） | VendorMoneyWithdrawn |
| CheckChangeBagUseCase / CheckCashBagUseCase（差額ありのとき） | VendorBalanceAdjusted |
| CreatePettyCashTransactionUseCase（小口入出金） | PettyCashDeposited / PettyCashWithdrawn |
| CheckSafeUseCase（差額ありのとき） | 調整イベント |
| Reverse*TransactionUseCase（赤伝） | 逆方向の入出金イベント |

### イベント記録の中身

```sql
INSERT INTO domain_events (aggregate_type, aggregate_id, event_type, payload, created_at)
VALUES ('Safe', 1, 'PettyCashWithdrawn', '{"amount":1000,...}', now())
```

取引の処理結果には影響しない。監査ログや状態の再構築のための記録。

### プロジェクションの中身

```sql
INSERT INTO petty_cash_ledger_view (...) VALUES (...)
UPDATE safe_balances SET petty_cash_balance = ? WHERE safe_id = ?
```

### コミットの範囲

```
AddAsync(transaction)    ─┐
AppendAsync(event)       ─┤ → BEGIN; INSERT ...; INSERT ...; UPDATE ...; COMMIT;
ProjectAsync(projection) ─┘
```

### DB の変化（例: 両替金バッグ入金）

```
現行:
  change_bags        : 1行INSERT
  vendor_transactions: 1行INSERT（type = Deposit）

ES+CQRS:
  change_bags        : 1行INSERT
  vendor_transactions: 1行INSERT（type = Deposit）
  vendor_ledger_view : 1行INSERT（Read Model）
  safe_balances      : vendor_balance を UPDATE（Read Model）
  domain_events      : 1行INSERT（event_type = VendorMoneyDeposited）
```

---

## 3. 読み込みの変化

| 読むもの | 現行 | ES+CQRS |
|---|---|---|
| 残高（ヘッダー・金庫一覧） | 取引テーブルの最新行の `balance`（`SafeRepository`） | `safe_balances` |
| 売上金出納帳 | `vendor_transactions`（`VendorLedgerQueryService`） | `vendor_ledger_view` |
| 小口出納帳 | `petty_cash_transactions`（`PettyCashLedgerQueryService`） | `petty_cash_ledger_view` |

ES+CQRS では **すべて Read Model テーブルからのみ読む（取引テーブルは読まない）**。
QueryService のインターフェース（`IVendorLedgerQueryService` など）は現行にもあるので、実装の読み先を差し替えるだけで済む。

---

## 4. DI 登録とテストの変化

```csharp
// Program.cs（追加）
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddScoped<IProjectionService, ProjectionService>();
```

```csharp
// テスト: UseCase のコンストラクタ引数が増える
var useCase = new CreatePettyCashTransactionUseCase(
    Mock.Of<IPettyCashTransactionRepository>(),
    Mock.Of<ISafeRepository>(),
    Mock.Of<ISequenceNumberService>(),
    Mock.Of<IBalanceService>(),
    Mock.Of<IProjectionService>(),               // 追加
    Mock.Of<IEventStore>(),                      // 追加: イベントを記録しないモック
    Mock.Of<IUnitOfWork>()
);
```

---

## 5. 変わらないもの

- **残高は取引行に保存する**（`BalanceService.AssignBalanceAsync`）。Read Model はその値を読みやすい形に写すだけで、残高の計算方法は変わらない。
- **業務ルールはドメイン層にある**（`PettyCashTransaction.Create`、`Safe.EnsureCanWithdrawPettyCash` など）。ES+CQRS は保存と読み込みの仕組みを変えるもので、業務の判断には影響しない。

## 6. 以前の実装で追加していた制約

- **過去日付の登録は禁止**（ES の原則として、時系列順に追記のみ）。現行の両替金バッグ・売上バッグ入金はリクエストの日付をそのまま使うので、ES を入れるならここの扱いを決める必要がある。

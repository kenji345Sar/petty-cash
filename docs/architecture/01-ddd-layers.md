# DDD レイヤーアーキテクチャ解説

## 全体構成

```
PettyCash.Api（API層）
  └── Controllers/           … HTTPリクエストを受けてUseCaseを呼ぶだけ

PettyCash.Application（アプリケーション層）
  ├── UseCases/              … ドメインとインフラを仲介する
  ├── Dtos/                  … 外部とのデータ受け渡し用
  └── UseCases/Queries/      … 出納帳読み込み用インターフェース（QueryService）

PettyCash.Domain（ドメイン層）
  ├── Vendor/                … 売上金ドメイン（エンティティ、リポジトリIF）
  ├── PettyCash/             … 小口ドメイン（エンティティ、リポジトリIF）
  ├── Safe/                  … 金庫集約
  └── Shared/                … 共有（ドメインサービスIF、値オブジェクト）

PettyCash.Infrastructure（インフラ層）
  ├── Data/                  … DbContext（DB接続）
  ├── Repositories/          … リポジトリ実装（DB読み書き）
  ├── Services/              … ドメインサービス実装（採番・残高計算）
  └── Queries/               … QueryService実装（出納帳の読み込み）
```

---

## 各層の責務

### API層（PettyCash.Api）

**HTTPの入口。ビジネスロジックは持たない。**

```csharp
// PettyCashTransactionsController.cs
[HttpPost]
public async Task<ActionResult> Create(CreatePettyCashTransactionRequestDto dto)
{
    var result = await createPettyCashTransactionUseCase.ExecuteAsync(dto);
    return CreatedAtAction(...);
}
```

やること：HTTPリクエストを受けて、UseCaseに渡して、結果を返すだけ。

### アプリケーション層（PettyCash.Application）

**ドメインとインフラを仲介する。処理の流れを組み立てる。**

```csharp
// CreatePettyCashTransactionUseCase.cs
public async Task<PettyCashTransactionDto> ExecuteAsync(...)
{
    // 1. ドメイン操作（エンティティ生成）
    var transaction = PettyCashTransaction.Create(safeId, type, amount, ...);

    // 2. ドメインサービス呼び出し
    await sequenceNumberService.AssignAsync(transaction);
    await balanceService.AssignBalanceAsync(transaction);

    // 3. インフラ操作（DB保存の予約）
    await transactionRepository.AddAsync(transaction);

    // 4. コミット
    await unitOfWork.SaveChangesAsync();

    // 5. DTOに変換して返す
    return new PettyCashTransactionDto(...);
}
```

UseCaseが全てを**つなぐ**役割。ドメインもインフラも直接は繋がっていない。

### ドメイン層（PettyCash.Domain）

**ビジネスルールの中心。DBやHTTPを一切知らない。**

含まれるもの：

```
エンティティ:
  VendorTransaction.cs      … 売上金取引のルール（ファクトリメソッド等）
  PettyCashTransaction.cs   … 小口取引のルール
  Safe.cs                   … 金庫（残高チェック等）
  ChangeBag.cs              … 両替金バッグ（状態遷移等）

値オブジェクト:
  Denomination.cs           … 金種（10000円×3枚、5000円×2枚...）

列挙型:
  TransactionType.cs        … 入金/出金/調整

リポジトリ インターフェース（契約だけ）:
  ISafeRepository.cs        … 「金庫を取得・保存できる」という契約
  IChangeBagRepository.cs   … 「バッグを取得・保存できる」という契約

ドメインサービス インターフェース（契約だけ）:
  IBalanceService.cs        … 「残高を計算できる」という契約
  ISequenceNumberService.cs … 「採番できる」という契約
  IUnitOfWork.cs            … 「コミットできる」という契約
```

重要なのは、ドメイン層にはインターフェース（契約）だけがあり、**実装がない**こと。
「どのDBに保存するか」「SQLをどう書くか」はドメイン層の関心事ではない。

### インフラ層（PettyCash.Infrastructure）

**技術的な実装。DBアクセスの具体的なコード。**

```
リポジトリ実装（ドメイン層のインターフェースを実装）:
  SafeRepository.cs         … ISafeRepository の実装（PostgreSQLにアクセス）
  ChangeBagRepository.cs    … IChangeBagRepository の実装

ドメインサービス実装:
  BalanceService.cs         … IBalanceService の実装（直前の取引行のbalanceを取得して加算）
  SequenceNumberService.cs  … ISequenceNumberService の実装（SQLでMAX+1取得）

QueryService実装:
  VendorLedgerQueryService.cs      … vendor_transactions から売上金出納帳を取得
  PettyCashLedgerQueryService.cs   … petty_cash_transactions から小口出納帳を取得

DbContext:
  PettyCashDbContext.cs     … EF Core のDB接続・テーブルマッピング
```

---

## 依存関係のルール

```
API層 → Application層 → Domain層
                ↑
        Infrastructure層
```

- **Domain層は何にも依存しない**（最も内側）
- Application層はDomain層に依存する
- Infrastructure層はDomain層に依存する（インターフェースの実装を提供）
- API層はApplication層に依存する

### 依存性の反転（DI）

ドメイン層がインフラ層に依存しない仕組み：

```
通常の依存:
  UseCase → Repository実装（PostgreSQL）   ← 上位が下位に依存 ✕

依存性の反転:
  UseCase → IRepository（インターフェース） ← 抽象に依存 ○
                ↑ 実装
  Repository実装（PostgreSQL）              ← 下位が上位に依存 ○
```

Program.csでDI登録することで、実行時にインターフェースと実装が紐づく：

```csharp
// Program.cs
builder.Services.AddScoped<ISafeRepository, SafeRepository>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
// → UseCaseが IBalanceService を要求すると、BalanceService が注入される
```

テスト時は実装をモックに差し替えられる：

```csharp
// テスト
var useCase = new CreatePettyCashTransactionUseCase(
    Mock.Of<IPettyCashTransactionRepository>(),  // DBに保存しないモック
    Mock.Of<ISafeRepository>(),
    Mock.Of<ISequenceNumberService>(),
    Mock.Of<IBalanceService>(),
    Mock.Of<IUnitOfWork>()                       // コミットしないモック
);
```

---

## UseCaseの役割 — なぜUseCaseが仲介するのか

```
✕ エンティティが直接DBを操作する:
  ChangeBag.Save()  → SQL実行   ← エンティティがDBを知ってしまう

○ UseCaseが仲介する:
  UseCase
    ├── ChangeBag.CreateDeposit()       … エンティティはビジネスルールだけ
    └── bagRepository.AddAsync(bag)     … DBへの保存はRepositoryに任せる
```

この分離により：
- **エンティティはビジネスルールに集中**（DB・HTTP・UIの知識不要）
- **テストが簡単**（DBなしでドメインロジックをテスト可能）
- **DB変更の影響が局所的**（PostgreSQL→別DBでもドメイン層は変更不要）

---

## UseCase にない手順

「何をどの順番でやるか」は UseCase にある、が基本。ただし例外が3つある。

| 場所 | 例 | なぜそこにあるか |
|---|---|---|
| ドメイン（集約の中） | `ChangeBag.CreateDeposit()` はバッグと入金取引を同時に作る。`ChangeBag.MoveToRegister()` は状態を変えて出金取引を返す | 「バッグと取引は必ずセットで生まれる」という業務の決まりなので、外に出すと守れなくなる |
| インフラ | `SafeRepository.GetByIdAsync()` は金庫を読んだあと `LoadBalances()` で残高を読み直してセットする | 「残高をどう組み立てて読むか」は技術的な都合。UseCase は「金庫を1件取得する」としか知らない |
| 画面（フロントエンド） | 登録後にタブ内のデータを取り直して再描画する（[api-screens/frontend-api-flow-detail.md](../api-screens/frontend-api-flow-detail.md)） | 画面の更新の仕方はサーバーの関心事ではない |

UseCase を読んでも処理が追いきれないときは、この3か所を見る。

---

## 実際の処理フロー（小口入金の例）

```
1. フロント → POST /api/petty-cash-transactions

2. API層: Controller がリクエストを受ける
   → CreatePettyCashTransactionUseCase.ExecuteAsync(dto) を呼ぶ

3. Application層: UseCase が処理を組み立てる
   → PettyCashTransaction.Create()          Domain層のエンティティ生成
   → sequenceNumberService.AssignAsync()     Domain層のサービス（IF）→ Infrastructure層（実装）
   → balanceService.AssignBalanceAsync()     同上
   → transactionRepository.AddAsync()        Domain層のリポジトリ（IF）→ Infrastructure層（実装）
   → unitOfWork.SaveChangesAsync()           コミット

4. Infrastructure層: 各実装がDBにアクセス
   → petty_cash_transactions の直前の balance を読んで新しい残高を計算
   → petty_cash_transactions にINSERT（残高は取引行の balance 列に保存）

5. Application層: UseCase がDTOに変換して返す
   → PettyCashTransactionDto

6. API層: Controller がHTTPレスポンスとして返す
   → 201 Created
```

---

## フォルダ構成と配置ルール

| 何を作ったか | 配置先 | 理由 |
|---|---|---|
| エンティティ（VendorTransaction等） | Domain層 | ビジネスルールだから |
| 値オブジェクト（Denomination） | Domain層 | ビジネスルールだから |
| リポジトリIF（ISafeRepository） | Domain層 | 契約（何ができるか）はドメインの関心事 |
| ドメインサービスIF（IBalanceService等） | Domain層 | 同上 |
| リポジトリ実装（SafeRepository） | Infrastructure層 | DBアクセスは技術的関心事 |
| ドメインサービス実装（BalanceService等） | Infrastructure層 | 同上 |
| UseCase | Application層 | ドメインとインフラの仲介 |
| DTO | Application層 | 外部とのデータ受け渡し |
| QueryServiceIF | Application層 | DTOを返すのでDomain層には置けない |
| QueryService実装 | Infrastructure層 | DBアクセスは技術的関心事 |
| Controller | API層 | HTTPの入口 |

---

## DB テーブルと役割

| テーブル | 役割 | 読み書き |
|---------|------|---------|
| safes | 金庫マスタ（残高は持たず、取引テーブルの最新行から読む） | 読み書き |
| change_bags | 両替金バッグ | 読み書き |
| cash_bags | 売上バッグ | 読み書き |
| prep_bags | 準備バッグ | 読み書き |
| vendor_transactions | 売上金取引（残高は各行の balance 列） | 読み書き |
| petty_cash_transactions | 小口取引（残高は各行の balance 列） | 読み書き |
| vendor_denomination_checks | 売上金有高チェック記録 | 読み書き |
| safe_denomination_checks | 小口有高チェック記録 | 読み書き |

残高の持ち方は [05-balance-design.md](05-balance-design.md) を参照。

---

イベントソーシング（ES+CQRS）を入れた場合に各層へ何が加わるかは [event-sourcing/changes-from-current.md](../event-sourcing/changes-from-current.md) を参照。

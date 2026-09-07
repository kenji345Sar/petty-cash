# 業者タブ 仕様

> 金庫（店舗）の業者向け現金を、バッグで管理しながら出納を記録する画面。
> 実装: `frontend/src/components/VendorTab.tsx`（バッグ: `BagList.tsx` / `CashBagList.tsx`）
> 共通概念（金種・有高チェック・赤伝・残高・期間フィルタ）は [common.md](./common.md) を参照。

## 1. 画面構成

<!-- 期間ナビ / バッグ管理（折りたたみ）/ 業者出納帳テーブル / 各モーダル。
     バッグ管理は既定で開いている。 -->

## 2. バッグ管理

業者側の現金は3種類のバッグで管理する。各バッグの操作は業者出納帳の取引として自動記録される。

### 2.1 両替金バッグ（ChangeBag, 表示 CA-xxx）

業者から受け取った両替金をひとまとめにした袋。金庫で保管し、必要に応じてレジへ移動する。

**状態遷移**

| 状態 | 遷移先 | 操作 | 条件 |
|---|---|---|---|
| InSafe（初期） | MovedToRegister | MoveToRegister | 1回のみ |

**業務ルール**

- 入金時は金種表の入力が必須（金額の直接入力は不可）
- 金額は金種の合計から自動算出される
- レジへの移動は1回のみ（二重移動は例外）
- 入金時・移動時にそれぞれ業者取引（VendorTransaction）が自動生成される

> ⚠️ **仕様確認事項（2026-06-24）**: `UpdateAmount` は `MovedToRegister` 後も状態ガードなしで呼び出せる。
> 有高チェック後の差額更新シナリオで使われているが、レジ移動後に呼び出すと帳簿上の金額と実移動金額がずれる可能性がある。
> ガードを追加するか仕様として許容するか未確定。

### 2.2 売上バッグ（CashBag, 表示 BAG-xxx）

レジの売上金を金庫へ入金するための袋。複数の売上バッグをまとめて業者への準備バッグとする。

**状態遷移**

| 状態 | 遷移先 | 操作 | 条件 |
|---|---|---|---|
| MovedToSafe（初期） | PrepBag割当 | PrepBag.Create | PrepBagId == null のもののみ |

**業務ルール**

- 入金時は金種表の入力が必須（金額ゼロの金種表は不可）
- 1つの売上バッグは1つの準備バッグにしか含められない
- 入金時に業者取引（VendorTransaction Deposit）が自動生成される

### 2.3 準備バッグ（PrepBag, 表示 準備-xxx）

複数の売上バッグをまとめて業者へ引き渡すための単位。

**状態遷移**

| 遷移 | 操作 | 条件 |
|---|---|---|
| (新規) → Preparing | Create | CashBag 1枚以上 |
| Preparing → HandedOver | MarkHandedOver | 1回のみ |
| Preparing → Cancelled | Cancel | 1回のみ |
| **Cancelled → HandedOver** | MarkHandedOver | **禁止（例外）** |
| HandedOver → Cancelled | Cancel | **禁止（例外）** |

**業務ルール**

- 1つ以上の売上バッグが必要
- 既に別の準備バッグに含まれている売上バッグは含められない（`PrepBagId != null` チェック）
- 合計金額は売上バッグの合計から自動算出
- 引渡時に出金取引（VendorTransaction Withdrawal）が自動生成される
- キャンセル後の売上バッグは再割当可能になる（`CashBags` が Clear される）

> ✅ **不具合修正済み（2026-06-24）**: `PrepBag.MarkHandedOver` が `Cancelled` 状態からも呼び出せる不具合を修正。
> `Cancelled` チェックを追加し、取消済みの準備バッグへの引渡は `InvalidOperationException` を送出するようにした。

## 3. 業者出納帳（一覧）

<!-- 取引＋有高チェックを番号順に統合表示。
     列: 番号/種別/金額/残高/摘要/バッグ/日時。
     バッグ列の表記ルール（CA-/BAG-/準備- と金額）。金種内訳リンク、赤伝ボタン。 -->

## 4. 有高チェック（業者）

<!-- バッグ種別ごとにチェック（change/cash/prep）。expectedAmount の取り方が種別で異なる。
     詳細ロジックは common.md、ここは業者固有の点。 -->

## 5. 赤伝（修正）

赤伝の基本（逆転ルール・追記型・残高の積み方）は [common.md](./common.md#3-赤伝逆仕訳) を参照。ここでは業者固有の制約を記す。

### 現状の挙動と制約

業者の出納帳取引は**すべてバッグ操作の副産物**（`MoveBagToRegister` / `MoveCashBagToSafe` が生成）であり、直接の入出金登録は存在しない。
一方、赤伝は `SafeId / Type / Amount / Description / CreatedAt` だけを逆転コピーし、**バッグID（changeBagId / cashBagId / prepBagId）は引き継がない**。

このため業者の赤伝には次の制約がある：

- 赤伝行は**どのバッグにも紐づかない**（出納帳のバッグ列は「-」表示）。
- 赤伝は**金額・残高だけを戻し、バッグの状態（移動済み・入金済み等）は戻さない**。
- 結果として、赤伝後は「帳簿は戻ったがバッグは移動済みのまま」という不整合が起こりうる。

### 暫定方針（C案 / 2026-06-19 時点）

発生源（バッグ操作）と修正点（出納帳）がズレているため、**本来は修正もバッグ操作側で行うべき**。ただしバッグ移動の取消（undo）が未実装で、赤伝を外すと修正手段が無くなるため、当面は次の暫定運用とする：

- 赤伝ボタンは**当面残す**（唯一の修正経路のため）。
- UI で「**バッグ状態は戻りません**」と警告を表示する（実装済み: `VendorTab.tsx` の赤伝モーダル）。
- 不整合が起きた場合は、バッグ側を手動で整える運用でカバーする。

### 将来方針（あるべき姿）

バッグ移動の取消（undo）を新設し、**バッグ状態と出納帳取引をセットで戻す**ように寄せる。これが入ったら業者出納帳の赤伝ボタンは廃止し、修正はバッグ操作の取消に一本化する。

| バッグ種別 | 現状の取消手段 |
|---|---|
| 両替金 (ChangeBag) | なし（要新設） |
| 売上 (CashBag) | なし（要新設） |
| 準備 (PrepBag) | Cancel あり |

## 6. 業務ルール詳細

### 6.1 シナリオ別保証内容

#### PrepBag作成（CreatePrepBagUseCase）

| ルール | 実装 | テスト |
|---|---|---|
| CashBag 1枚以上必須 | PrepBag.Create | ✅ |
| 別PrepBag割当済み CashBag 不可 | PrepBag.Create | ❌ ユニットテスト不可（EF Core FK依存） |
| TotalAmount = CashBag 合計 | PrepBag.Create | ✅ |
| 作成後 Preparing 状態 | PrepBag.Create | ✅ |
| prepRepo.Add + unitOfWork.Save | Application | ✅ |
| CashBag 未存在 → KeyNotFoundException | Application | ✅ |

#### PrepBagキャンセル（CancelPrepBagUseCase）

| ルール | 実装 | テスト |
|---|---|---|
| Preparing → Cancelled | PrepBag.Cancel | ✅ |
| キャンセル後 CashBags 空 | PrepBag.Cancel | ✅ |
| HandedOver → キャンセル不可 | PrepBag.Cancel | ✅ |
| 二重キャンセル不可 | PrepBag.Cancel | ✅ |
| **Cancelled → HandedOver 不可** | PrepBag.MarkHandedOver | ✅（修正済み） |
| prepRepo.Update + unitOfWork.Save | Application | ✅ |
| 未存在 ID → KeyNotFoundException | Application | ✅ |
| Domain 例外時に Save 呼ばれない | Application | ✅ |

#### PrepBag引渡（HandOverPrepBagUseCase）

| ルール | 実装 | テスト |
|---|---|---|
| Preparing → HandedOver | PrepBag.MarkHandedOver | ✅ |
| 出金取引 Withdrawal 自動生成 | PrepBag.MarkHandedOver | ✅ |
| 二重引渡不可 | PrepBag.MarkHandedOver | ✅ |
| Cancelled 状態から引渡不可 | PrepBag.MarkHandedOver | ✅（修正済み） |
| txRepo.Add + unitOfWork.Save | Application | ✅ |
| 未存在 ID → KeyNotFoundException | Application | ✅ |
| Domain 例外時に Add 呼ばれない | Application | ✅ |

#### CashBag入金（DepositCashBagUseCase）

| ルール | 実装 | テスト |
|---|---|---|
| 金種表必須 | CashBag.CreateDeposit | ✅ |
| 金種合計ゼロ不可 | CashBag.CreateDeposit | ✅ |
| 初期状態 MovedToSafe | CashBag.CreateDeposit | ✅ |
| VendorTransaction(Deposit) 自動生成 | CashBag.CreateDeposit | ✅ |
| cashBagRepo.Add + unitOfWork.Save | Application | ✅ |
| Domain 例外時に Add 呼ばれない | Application | ✅ |

#### ChangeBagレジ移動（MoveBagToRegisterUseCase）

| ルール | 実装 | テスト |
|---|---|---|
| InSafe → MovedToRegister | ChangeBag.MoveToRegister | ✅ |
| 出金取引 Withdrawal 自動生成 | ChangeBag.MoveToRegister | ✅ |
| 二重移動不可 | ChangeBag.MoveToRegister | ✅ |
| bagRepo.Update + unitOfWork.Save | Application | ✅ |
| 未存在 ID → KeyNotFoundException | Application | ✅ |
| Domain 例外時に Update 呼ばれない | Application | ✅ |
| **MovedToRegister 後の UpdateAmount** | 状態ガードなし | ⚠️（仕様確認中） |

#### VendorTransaction赤伝（ReverseVendorTransactionUseCase）

| ルール | 実装 | テスト |
|---|---|---|
| Deposit の赤伝 → Withdrawal | VendorTransaction.CreateReversal | ✅ |
| Withdrawal の赤伝 → Deposit | VendorTransaction.CreateReversal | ✅ |
| Adjustment(≥0) の赤伝 → Withdrawal | VendorTransaction.CreateReversal | ✅ |
| Adjustment(<0) の赤伝 → Deposit（絶対値） | VendorTransaction.CreateReversal | ✅ |
| Deposit 赤伝時は残高チェックあり | Application | ✅ |
| Withdrawal 赤伝時は残高チェックなし | Application | ✅ |
| 残高不足 → 例外、Add 呼ばれない | Application | ✅ |
| 未存在 ID → KeyNotFoundException | Application | ✅ |
| 正常時 txRepo.Add + unitOfWork.Save | Application | ✅ |
| 例外時 unitOfWork.Save 呼ばれない | Application | ✅ |
| 二重赤伝 | なし | ⚠️（仕様未確定） |

## 7. API

<!-- getVendorDashboard / depositBag / moveBagToRegister / depositCashBag /
     handOverPrepBag / checkChangeBag / checkCashBag / checkPrepBag /
     updateVendorDenominationCheck / reverseVendorTransaction
     api-screens/api-endpoints.md から業者分を抜粋。 -->

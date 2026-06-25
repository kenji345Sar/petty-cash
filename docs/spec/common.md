# 共通仕様 — 小口・業者で共通する概念

> 小口タブ・業者タブの両方で使う概念をここに一本化する（重複説明を避けるため）。
> 各タブ固有の点は [petty-cash.md](./petty-cash.md) / [vendor.md](./vendor.md) に書く。

## 1. 金種

<!-- 紙幣・硬貨の種類（10000/5000/1000/500/100/50/10/5/1）と枚数。
     金種表で金額をセットする使い方、金種内訳モーダルの表示。
     定義: frontend/src/shared/denomination.ts (DENOM_ITEMS) -->

## 2. 有高チェック

<!-- 実際の現金を金種で数え、帳簿残高(expectedAmount)と照合して差額を出す操作。
     金種表との違いは denomination-vs-check.md を集約。
     チェックは取引と同様に出納帳へ番号付きで並ぶ。後から編集可能。 -->

## 3. 赤伝（逆仕訳）

過去取引を打ち消す仕組み。**元取引は削除せず、反対方向の取引を末尾に追加する**（追記型）。
履歴・監査のため、訂正しても元の記録を残すのが目的。

実装: `ReversePettyCashTransactionUseCase` / `ReverseVendorTransactionUseCase`、逆転ロジックは Domain の `CreateReversal`。

### 逆転ルール（小口・業者共通）

| 元取引 | 赤伝で追加される取引 | 金額 |
|---|---|---|
| 入金 (Deposit) | 出金 (Withdrawal) | 元と同額 |
| 出金 (Withdrawal) | 入金 (Deposit) | 元と同額 |
| 調整 (Adjustment, ≥0) | 出金 (Withdrawal) | 元と同額 |
| 調整 (Adjustment, <0) | 入金 (Deposit) | **絶対値** |

> 実装: `PettyCashTransaction.CreateReversal` / `VendorTransaction.CreateReversal`（switch式で上記4パターンを判定）

### 重要な性質

- 赤伝は**新しい連番・新しい行**として時系列の末尾に積まれる。元取引やその後の行の `balance` は書き換えない。
- 入金の赤伝は「出金」になるため、**残高が不足していると失敗する**（`EnsureCanWithdraw`）。過去の入金を取り消したくても、その分が既に使われていると赤伝できない。
- 摘要の既定値は `#<元の番号>の修正（赤伝）`。

### 二重赤伝（仕様未確定）

同一取引IDに対して複数回赤伝を打つことの可否は **仕様未確定**（2026-06-24 時点）。

- 現実装: Domain に「赤伝済み」状態を持たないため防止していない
- 運用上問題が発生した場合: Domain への赤伝済みフラグ追加または Application での重複チェックを追加する

> ⚠️ 業者の赤伝には固有の制約がある（バッグ状態を戻さない）。[vendor.md](./vendor.md#5-赤伝修正) を参照。

## 4. 残高（running balance）

各取引が「その時点の残高」を**取引行ごとに保持**する。出納帳の残高列はこの値をそのまま表示する。

**設計方針**: 残高は金庫(Safe)ではなく**取引(Transaction)が持つ**。金庫が持つのは現在残高のスナップショット（`vendorBalance` / `pettyCashBalance`）のみ。

**計算タイミング**: 書き込み時に1回だけ計算して保存する（読み取り時に再計算しない）。
実装: `BalanceService.AssignBalanceAsync`
- 直前の残高を「全件合計」ではなく**最新1行の `balance` を読む**（`ORDER BY created_at DESC, id DESC LIMIT 1`）。
- そこに今回の増減（出金は `-amount`、それ以外は `+amount`）を足して新しい残高とする。

**読み取り**: QueryService は保存済みの `balance` をそのまま返すだけ（再計算なし）。

> 旧 ES+CQRS 構成では Read Model 経由で残高を読んでいたが、その層を削除しても「残高は取引行に保存」という事実は不変。詳細は [docs/archive/](../archive/README.md)。

## 5. 期間フィルタ

<!-- 月送り（«/»）と期間選択（custom from-to）。出納帳・バッグ一覧の絞り込みに共通利用。
     月ナビの既定は当月。 -->

## 6. 取引種別

<!-- 入金(Deposit) / 出金(Withdrawal) / 調整(Adjustment) / 有高(check)。
     各種別の意味と、調整が発生する場面。 -->

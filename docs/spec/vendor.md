# 業者タブ 仕様

> 金庫（店舗）の業者向け現金を、バッグで管理しながら出納を記録する画面。
> 実装: `frontend/src/components/VendorTab.tsx`（バッグ: `BagList.tsx` / `CashBagList.tsx`）
> 共通概念（金種・有高チェック・赤伝・残高・期間フィルタ）は [common.md](./common.md) を参照。

<!-- 枠組みのみ。集約元: docs/architecture/business-flow.md, domain-rules.md,
     api-screens/vendor-screen-data-flow.md, display-logic.md -->

## 1. 画面構成

<!-- 期間ナビ / バッグ管理（折りたたみ）/ 業者出納帳テーブル / 各モーダル。
     バッグ管理は既定で開いている。 -->

## 2. バッグ管理

<!-- 業者側の現金のまとまり。3種類ある。役割と相互関係をここで定義する。 -->

### 2.1 釣銭バッグ（ChangeBag, 表示 CA-xxx）
<!-- 用途、作成〜入金の流れ、有高チェック対象。 -->

### 2.2 売上バッグ（CashBag, 表示 BAG-xxx）
<!-- 用途、作成〜入金の流れ。 -->

### 2.3 準備バッグ（PrepBag, 表示 準備-xxx）
<!-- 複数の売上バッグをまとめる。cashBagIds の合計。受け渡し(HandOver)の流れ。 -->

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
| 釣銭 (ChangeBag) | なし（要新設） |
| 売上 (CashBag) | なし（要新設） |
| 準備 (PrepBag) | Cancel あり |

## 6. 業務ルール

<!-- バッグの状態遷移、入金済みバッグの扱いなど。domain-rules.md から集約。 -->

## 7. API

<!-- getVendorDashboard / depositBag / moveBagToRegister / depositCashBag /
     handOverPrepBag / checkChangeBag / checkCashBag / checkPrepBag /
     updateVendorDenominationCheck / reverseVendorTransaction
     api-screens/api-endpoints.md から業者分を抜粋。 -->

# API エンドポイント一覧

ベースURL: `http://localhost:5141/api`

## 金庫（Safes）

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/safes` | 金庫一覧取得 |
| POST | `/api/safes` | 金庫作成 |

## 小口（PettyCash）

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/petty-cash-transactions?safeId=` | 小口取引一覧 |
| POST | `/api/petty-cash-transactions` | 小口入出金登録 |

## 業者 — 両替金バッグ（ChangeBags）

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/bags?safeId=` | 両替金バッグ一覧 |
| POST | `/api/bags/deposit` | 両替金バッグ入金 |
| POST | `/api/bags/{id}/move` | レジへ移動 |

## 業者 — 売上バッグ（CashBags）

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/cashbags?safeId=` | 売上バッグ一覧 |
| POST | `/api/cashbags/deposit` | 売上バッグ入金 |

## 業者 — 準備バッグ（PrepBags）

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/prepbags?safeId=` | 準備バッグ一覧 |
| POST | `/api/prepbags` | 準備バッグ作成（複数CashBagをまとめる） |
| POST | `/api/prepbags/{id}/handover` | 業者へ引渡 |
| POST | `/api/prepbags/{id}/cancel` | 取消（CashBagを元に戻す） |

## 業者 — 取引（VendorTransactions）

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/vendor-transactions?safeId=` | 業者取引一覧 |

## 有高チェック（DenominationChecks）

### 取得

| メソッド | エンドポイント | 用途 |
|---|---|---|
| GET | `/api/denominationchecks/vendor?safeId=` | 業者側チェック一覧 |
| GET | `/api/denominationchecks/safe?safeId=` | 小口側チェック一覧 |

### 新規チェック

| メソッド | エンドポイント | 用途 |
|---|---|---|
| POST | `/api/denominationchecks/safe/{safeId}` | 金庫（小口）の有高チェック |
| POST | `/api/denominationchecks/changebag/{bagId}` | 両替金バッグの有高チェック |
| POST | `/api/denominationchecks/cashbag/{bagId}` | 売上バッグの有高チェック |
| POST | `/api/denominationchecks/prepbag/{bagId}` | 準備バッグの有高チェック |

### チェック修正

| メソッド | エンドポイント | 用途 |
|---|---|---|
| PUT | `/api/denominationchecks/vendor/{id}` | 業者側チェック修正 |
| PUT | `/api/denominationchecks/safe/{id}` | 小口側チェック修正 |

合計 **21エンドポイント**

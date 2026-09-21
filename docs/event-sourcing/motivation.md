# なぜ ES+CQRS を入れるのか

現行構成（ES+CQRS なし）の課題と、Read Model を入れると何が変わるかを整理する。

---

## 現行構成の課題 — Read Model が無い

取引テーブル（`vendor_transactions` / `petty_cash_transactions`）は、取引を1行ずつ追記していく「イベントストア相当」のテーブルになっている。
しかし、書き込みも読み込みも同じテーブルに対して行っている。

```
現行:
  vendor_transactions       ←── 書き込み（取引の記録）
  vendor_transactions       ←── 読み込み（出納帳表示・残高取得）★同じテーブル

  petty_cash_transactions   ←── 書き込み（取引の記録）
  petty_cash_transactions   ←── 読み込み（出納帳表示・残高取得）★同じテーブル

CQRS にすると:
  vendor_transactions       ←── 書き込み（イベント記録）
       ↓ プロジェクション
  vendor_ledger_view（Read Model） ←── 読み込み（出納帳表示）
  safe_balances（Read Model）      ←── 読み込み（残高取得）
```

## Read Model を導入すると

- **書き込み**: 今まで通り取引テーブルに INSERT
- **プロジェクション**: INSERT 後に Read Model テーブルを更新
- **読み込み**: Read Model テーブルからのみ読む（イベントストアは読まない）
- **メリット**: 読み込みが高速になり、書き込みと読み込みの構造をそれぞれ独立して最適化できる

具体的にどのコードが変わるかは [changes-from-current.md](changes-from-current.md) を参照。

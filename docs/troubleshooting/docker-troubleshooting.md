# Docker トラブルシューティング記録

Docker 環境構築時に発生した問題と解決策のまとめ。

---

## 1. テストプロジェクトの .NET バージョン不一致

**エラー:**
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0.
```

**原因:**
テストプロジェクト（`PettyCash.Domain.Tests`, `PettyCash.Application.Tests`）が `net10.0` をターゲットにしているが、DockerのSDK イメージは `dotnet/sdk:9.0` のため対応していない。

**解決:**
Dockerfile の `dotnet restore` を API プロジェクトのみに限定。

```dockerfile
# 変更前
RUN dotnet restore

# 変更後
RUN dotnet restore PettyCash.Api/PettyCash.Api.csproj
```

---

## 2. TypeScript ビルドエラー（本番ビルド時）

**エラー:**
```
error TS6133: 'api' is declared but its value is never read.
error TS2339: Property 'createTransaction' does not exist on type ...
error TS2741: Property 'allBags' is missing in type ...
error TS2551: Property 'updateDenominationCheck' does not exist ... Did you mean 'updateVendorDenominationCheck'?
```

**原因:**
開発モード（`vite dev`）では TypeScript の型チェックがスキップされるため気づかなかったが、本番ビルド（`tsc -b && vite build`）で厳密にチェックされて発覚。

**解決:**
- `DenominationReportPage.tsx`: 未使用の `api`, `Denomination` import と `onUpdate` 引数を削除
- `TransactionList.tsx`: `api.createTransaction` → `api.createPettyCashTransaction` に修正
- `TransactionList.tsx`: `CashBagList` に `allBags` prop を追加
- `BagList.tsx`, `CashBagList.tsx`, `TransactionList.tsx`: `api.updateDenominationCheck` → `api.updateVendorDenominationCheck` に修正

**教訓:**
開発時でも定期的に `npm run build`（`tsc -b`）を実行して型エラーを早期に検出すべき。

---

## 3. 旧テーブル参照エラー（新規DB起動時）

**エラー:**
```
relation "denomination_checks" does not exist
```

**原因:**
`Program.cs` のマイグレーションコードで `ALTER TABLE denomination_checks` が無条件に実行されていた。Docker の PostgreSQL は空のDBから開始するため、旧テーブル `denomination_checks` が存在しない。

**解決:**
`ALTER TABLE denomination_checks ADD COLUMN IF NOT EXISTS sequence_number ...` を、旧テーブル存在チェック（`hasOldDenomTable > 0`）のブロック内に移動。

---

## 4. Chrome が localhost を HTTPS にリダイレクトする

**症状:**
ブラウザで `http://localhost` にアクセスすると自動的に `https://localhost` にリダイレクトされ、接続拒否（ERR_CONNECTION_REFUSED）になる。

**原因:**
Chrome が HSTS（HTTP Strict Transport Security）により `localhost` を HTTPS に強制リダイレクトしている。

**解決:**
ポートを `80` から `8080` に変更して回避。

```yaml
# docker-compose.prod.yml
ports:
  - "8080:80"  # 80 → 8080 に変更
```

**代替策（Chrome設定をクリアする方法）:**
1. `chrome://net-internals/#hsts` を開く
2. 「Delete domain security policies」に `localhost` を入力して Delete

---

## 5. CORS エラー（本番モード）

**エラー:**
```
Access to fetch at 'http://localhost:5141/api/safes' from origin 'http://localhost:8080'
has been blocked by CORS policy
```

**原因:**
フロントエンド（`localhost:8080`）からバックエンド（`localhost:5141`）への直接リクエストはクロスオリジンとなり、バックエンドの CORS 設定に `localhost:8080` が含まれていなかった。

**解決:**
フロントエンドの API_BASE をバックエンドの直接URLではなく、nginx プロキシ経由の相対パスに変更。

```yaml
# docker-compose.prod.yml
args:
  VITE_API_BASE: "/api"  # http://localhost:5141/api → /api に変更
```

nginx が `/api/` へのリクエストを `backend:5141` にプロキシするため、同一オリジンとなり CORS 問題が解消される。

```nginx
# nginx.conf
location /api/ {
    proxy_pass http://backend:5141/api/;
}
```

**ポイント:**
本番環境では「フロントエンドからバックエンドへ直接アクセス」ではなく「nginx でリバースプロキシ」するのがベストプラクティス。CORS の設定不要で、セキュリティも向上する。

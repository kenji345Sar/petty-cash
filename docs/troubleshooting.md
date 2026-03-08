# 小口現金管理システム - トラブルシューティング記録

## 1. .NET バージョン不一致

**症状:** プロジェクトが .NET 10 で作成されたが、インストール済みの安定版は .NET 9.0

**対応:**
- 全プロジェクト（Domain, Application, Infrastructure, Api）の `TargetFramework` を `net10.0` → `net9.0` に変更
- NuGet パッケージも 9.x 系にダウングレード
  - `Npgsql.EntityFrameworkCore.PostgreSQL`: 10.0.0 → 9.0.4
  - `Microsoft.AspNetCore.OpenApi`: 10.0.2 → 9.0.3
  - `Microsoft.EntityFrameworkCore.Design`: 10.0.3 → 9.0.3

**対象ファイル:**
- `PettyCash.Domain/PettyCash.Domain.csproj`
- `PettyCash.Application/PettyCash.Application.csproj`
- `PettyCash.Infrastructure/PettyCash.Infrastructure.csproj`
- `PettyCash.Api/PettyCash.Api.csproj`

---

## 2. PostgreSQL 未インストール・接続拒否

**症状:**
```
Npgsql.NpgsqlException (0x80004005): Failed to connect to 127.0.0.1:5432
System.Net.Sockets.SocketException (61): Connection refused
```

**原因:** PostgreSQL がマシンにインストールされていなかった

**対応:**
```bash
brew install postgresql@16
brew services start postgresql@16
```

**DB・ユーザー作成:**
```bash
/usr/local/opt/postgresql@16/bin/psql -U $(whoami) -d postgres -c "CREATE USER postgres WITH SUPERUSER PASSWORD 'postgres';"
/usr/local/opt/postgresql@16/bin/psql -U $(whoami) -d postgres -c "CREATE DATABASE petty_cash OWNER postgres;"
```

**接続文字列（appsettings.Development.json）:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=petty_cash;Username=postgres;Password=postgres"
}
```

---

## 3. ポート競合（Address already in use）

**症状:**
```
System.IO.IOException: Failed to bind to address http://127.0.0.1:5141: address already in use.
```

**原因:** 前回の dotnet run プロセスが残っていた

**対応:**
```bash
lsof -ti:5141        # ポートを使用しているプロセスIDを確認
kill <プロセスID>      # プロセスを停止
```

**備考:** launchSettings.json でバックエンドのポートは `5141` に設定されている

---

## 4. Node.js バージョン不足（Vite 起動失敗）

**症状:**
```
You are using Node.js 18.16.0. Vite requires Node.js version 20.19+ or 22.12+.
TypeError: crypto.hash is not a function
```

**原因:** nvm のデフォルトが Node.js 18.16.0 で、Vite 7 が要求する 20.19+ を満たしていなかった

**対応:**
```bash
source ~/.nvm/nvm.sh && nvm use 22    # Node.js 22 に切替
nvm alias default 22.16.0             # デフォルトを変更
```

**注意:** 既に開いているターミナルにはデフォルト変更が反映されない。新しいターミナルを開くか、`source ~/.nvm/nvm.sh && nvm use 22` を実行する必要がある

---

## 5. TypeScript 型インポートエラー（Vite + esbuild）

**症状:**
```
Uncaught SyntaxError: The requested module '/src/types/index.ts'
does not provide an export named 'ChangeBag' (at client.ts:1:10)
```

**原因:**
- `src/types/index.ts` は interface（型）のみをエクスポートしていた
- Vite は esbuild でファイルを個別にトランスパイルするため、型のみのファイルは中身が空になる
- ブラウザが空モジュールから `ChangeBag` をインポートしようとして失敗

**対応:**
1. `types/index.ts` の型定義を `api/client.ts`（ランタイムコードがあるファイル）に移動
2. 全コンポーネントの import 先を `../types` → `../api/client` に変更
3. 型の import には `import type` を使用

**変更前:**
```typescript
// src/api/client.ts
import { ChangeBag, Denomination, Transaction } from "../types";
```

**変更後:**
```typescript
// src/api/client.ts（型定義もこのファイル内に配置）
export interface ChangeBag { ... }
export interface Transaction { ... }
export const api = { ... };

// src/components/BagList.tsx
import { api } from "../api/client";
import type { ChangeBag, Denomination } from "../api/client";
```

**tsconfig.app.json の変更:**
- `verbatimModuleSyntax`: true → false
- `isolatedModules`: true を追加
- `erasableSyntaxOnly`: true → false

---

## 6. 画面レイアウト（上部余白が大きい）

**症状:** ページコンテンツが画面の上下中央に表示され、上部に大きな空白がある

**原因:** Vite テンプレートのデフォルト `index.css` に以下の設定があった
```css
body {
  display: flex;
  place-items: center;    /* 上下中央寄せ */
  min-height: 100vh;      /* 画面全体の高さ */
}
```

**対応:** `src/index.css` をリセット
```css
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
body {
  font-family: "Helvetica Neue", Arial, sans-serif;
  color: #213547;
  background-color: #ffffff;
  min-width: 320px;
}
```

---

## 起動手順まとめ

**バックエンド（ターミナル1）:**
```bash
cd /Users/apple/Desktop/Site/DDD/petty-cash/backend/PettyCash.Api && dotnet run
```

**フロントエンド（ターミナル2）:**
```bash
source ~/.nvm/nvm.sh && nvm use 22 && cd /Users/apple/Desktop/Site/DDD/petty-cash/frontend && npm run dev
```

**ブラウザ:** http://localhost:5173

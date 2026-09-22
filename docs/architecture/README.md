# architecture — DDD でどう実現しているか

このシステムがドメイン駆動設計（DDD）で、業務をどうコードにしているかを説明する。
「何を作っているか」（業務・画面・ルール）は [spec/](../spec/README.md) を先に読むとわかりやすい。

## 読む順番

| 順 | ファイル | わかること |
|---|---|---|
| 1 | [01-ddd-layers.md](01-ddd-layers.md) | 4つの層（API / Application / Domain / Infrastructure）の役割、依存の向き、DB テーブル |
| 2 | [02-domain-model.md](02-domain-model.md) | ドメインが守っている業務ルールと、その在り処。金種と有高チェックの設計判断 |
| 3 | [03-flow-to-code.md](03-flow-to-code.md) | 業務の操作ごとに、Controller → UseCase → Domain → DB と処理がどう流れるか |
| 4 | [04-unit-of-work.md](04-unit-of-work.md) | 複数の変更を1トランザクションでまとめて保存する仕組み |
| 5 | [05-balance-design.md](05-balance-design.md) | 残高を取引の各行に持たせる理由と、その変遷 |

1〜2 で「構造」、3 で「流れ」、4〜5 で「横断的な仕組み」がわかる。

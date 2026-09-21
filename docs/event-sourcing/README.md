# event-sourcing — イベントソーシング（ES+CQRS）への対応

このフォルダは、petty-cash に ES+CQRS を適用するための資料をまとめたもの。
**現行コードは ES+CQRS を使っていない。** 現行の構成は [docs/architecture/](../architecture/) を参照。

ES 対応の実装は別リポジトリで行う予定。ここはその前提となる「なぜ必要か」「以前どう実装したか」「現行から何が変わるか」を残している。

| ファイル | 内容 |
|---|---|
| [motivation.md](motivation.md) | なぜ ES+CQRS を入れるのか（現行構成の課題） |
| [changes-from-current.md](changes-from-current.md) | 現行構成から ES+CQRS にすると、コード・DB・テストのどこが変わるか |
| [es-cqrs-architecture.md](es-cqrs-architecture.md) | 以前の実装の処理フロー全体図 |
| [es-cqrs-poc.md](es-cqrs-poc.md) | 以前の PoC 実装の説明 |
| [read-model-design.md](read-model-design.md) | Read Model の設計 |

以前の実装は 2026-06-18（commit cd386ed）でコードから削除した。上の3本（es-cqrs-architecture / es-cqrs-poc / read-model-design）はその時点のコードを説明したもので、現行コードとは一致しない。

現行の仕様は [docs/spec/](../spec/README.md) を参照。

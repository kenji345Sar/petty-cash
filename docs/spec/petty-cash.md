# 小口タブ 仕様

> 金庫（店舗）の小口現金の入出金と有高チェックを管理する画面。
> 実装: `frontend/src/components/PettyCashTab.tsx`
> 共通概念（金種・有高チェック・赤伝・残高・期間フィルタ）は [common.md](./common.md) を参照。

<!-- 枠組みのみ。各節の中身はこれから埋める。
     集約元: docs/architecture/business-flow.md, domain-rules.md, api-screens/display-logic.md -->

## 1. 画面構成

<!-- 期間ナビ / アクションバー（入出金登録・有高チェック）/ 入力フォーム / 小口出納帳テーブル / 各モーダル
     の配置を1枚で示す。実装の見た目に対応させる。 -->

## 2. 入出金登録

<!-- 入金/出金の選択、金額、金種表（任意）、備考。
     既定の摘要（小口入金/小口出金）。登録後にlist表示へ戻る挙動。 -->

## 3. 有高チェック（小口）

<!-- 金種で実残高を数え、帳簿残高(pettyCashBalance)と照合。差額の記録。
     詳細ロジックは common.md の有高チェック節を参照、ここは小口固有の点だけ。 -->

## 4. 小口出納帳（一覧）

<!-- 取引＋有高チェックを番号(sequenceNumber)順に統合表示。
     列: 番号/種別/金額/残高/摘要/日時。種別: 入金・出金・調整・有高。
     金種内訳リンク、赤伝ボタン。 -->

## 5. 赤伝（修正）

<!-- 出納帳の各取引から赤伝作成。元取引を打ち消す逆取引を追加。
     詳細は common.md、ここは小口固有の点だけ。 -->

## 6. 業務ルール

<!-- 過去日付の扱い、マイナス残高の可否など。domain-rules.md から該当分を集約。 -->

## 7. API

<!-- この画面が使うエンドポイント。api-screens/api-endpoints.md から小口分を抜粋。
     getPettyCashDashboard / createPettyCashTransaction / checkSafe /
     updatePettyCashDenominationCheck / reversePettyCashTransaction -->

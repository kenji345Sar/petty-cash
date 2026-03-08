import { useState } from "react";
import { api } from "../api/client";
import type { Transaction, DenominationCheck, ChangeBag, CashBag, PrepBag } from "../api/client";
import { DenominationCheckForm } from "./DenominationCheckForm";

interface Props {
  transactions: Transaction[];
  denomChecks: DenominationCheck[];
  bags: ChangeBag[];
  cashBags: CashBag[];
  prepBags: PrepBag[];
  onUpdate: () => void;
}

type Period = "thisMonth" | "lastMonth" | "custom";

function getMonthRange(offset: number): [string, string] {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  const end = new Date(now.getFullYear(), now.getMonth() + offset + 1, 0);
  return [
    start.toISOString().slice(0, 10),
    end.toISOString().slice(0, 10),
  ];
}

type Row =
  | { kind: "tx"; data: Transaction; at: string }
  | { kind: "check"; data: DenominationCheck; at: string };

function formatBag(
  changeBagId: number | null,
  cashBagId: number | null,
  prepBagId: number | null | undefined,
  bags: ChangeBag[],
  cashBags: CashBag[],
  prepBags: PrepBag[],
) {
  if (changeBagId) {
    const bag = bags.find((b) => b.id === changeBagId);
    return bag
      ? `釣り銭#${changeBagId}${bag.description ? ` (${bag.description})` : ""} ${bag.totalAmount.toLocaleString()}円`
      : `釣り銭#${changeBagId}`;
  }
  if (cashBagId) {
    const bag = cashBags.find((b) => b.id === cashBagId);
    return bag
      ? `キャッシュ#${cashBagId}${bag.description ? ` (${bag.description})` : ""} ${bag.totalAmount.toLocaleString()}円`
      : `キャッシュ#${cashBagId}`;
  }
  if (prepBagId) {
    const pb = prepBags.find((p) => p.id === prepBagId);
    return pb
      ? `準備#${prepBagId} (${pb.cashBagIds.map((id) => `CB#${id}`).join(",")} / ${pb.totalAmount.toLocaleString()}円)`
      : `準備#${prepBagId}`;
  }
  return "-";
}

export function TransactionList({ transactions, denomChecks, bags, cashBags, prepBags, onUpdate }: Props) {
  const [editCheck, setEditCheck] = useState<DenominationCheck | null>(null);
  const [showForm, setShowForm] = useState<"petty" | "vendor" | null>(null);
  const [txType, setTxType] = useState<"Deposit" | "Withdrawal">("Deposit");
  const [amount, setAmount] = useState(0);
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const label = showForm === "petty"
        ? (txType === "Deposit" ? "小口入金" : "小口出金")
        : (txType === "Deposit" ? "業者入金" : "業者出金");
      const desc = description || label;
      await api.createTransaction({ type: txType, amount, description: desc, date });
      setAmount(0);
      setDescription("");
      setDate(new Date().toISOString().slice(0, 10));
      setTxType("Deposit");
      setShowForm(null);
      onUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };
  const editBagType = editCheck?.changeBagId ? "change" as const : editCheck?.prepBagId ? "prep" as const : "cash" as const;
  const editBagId = editCheck?.changeBagId ?? editCheck?.cashBagId ?? editCheck?.prepBagId ?? 0;
  const editExpected = editCheck?.changeBagId
    ? bags.find((b) => b.id === editCheck.changeBagId)?.totalAmount ?? 0
    : editCheck?.prepBagId
    ? prepBags.find((p) => p.id === editCheck.prepBagId)?.totalAmount ?? 0
    : cashBags.find((b) => b.id === editCheck?.cashBagId)?.totalAmount ?? 0;

  const [period, setPeriod] = useState<Period>("thisMonth");
  const [thisMonth] = useState(() => getMonthRange(0));
  const [lastMonth] = useState(() => getMonthRange(-1));
  const [customFrom, setCustomFrom] = useState(thisMonth[0]);
  const [customTo, setCustomTo] = useState(thisMonth[1]);

  const [from, to] = period === "thisMonth" ? thisMonth : period === "lastMonth" ? lastMonth : [customFrom, customTo];

  const inRange = (dateStr: string) => {
    const d = dateStr.slice(0, 10);
    return d >= from && d <= to;
  };

  const rows: Row[] = [
    ...transactions.filter((t) => inRange(t.createdAt)).map((t) => ({ kind: "tx" as const, data: t, at: t.createdAt })),
    ...denomChecks.filter((c) => inRange(c.createdAt)).map((c) => ({ kind: "check" as const, data: c, at: c.createdAt })),
  ].sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <h2 style={{ margin: 0 }}>出納帳</h2>
          <button className="btn-primary" onClick={() => { setShowForm(showForm === "petty" ? null : "petty"); setTxType("Deposit"); }}>
            {showForm === "petty" ? "閉じる" : "小口登録"}
          </button>
          <button className="btn-primary" onClick={() => { setShowForm(showForm === "vendor" ? null : "vendor"); setTxType("Deposit"); }}>
            {showForm === "vendor" ? "閉じる" : "業者登録"}
          </button>
        </div>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <button
            className={period === "lastMonth" ? "btn-period active" : "btn-period"}
            onClick={() => setPeriod("lastMonth")}
          >
            先月
          </button>
          <button
            className={period === "thisMonth" ? "btn-period active" : "btn-period"}
            onClick={() => setPeriod("thisMonth")}
          >
            今月
          </button>
          <button
            className={period === "custom" ? "btn-period active" : "btn-period"}
            onClick={() => setPeriod("custom")}
          >
            期間選択
          </button>
          {period === "custom" && (
            <>
              <input type="date" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} style={{ padding: "4px 8px", borderRadius: 4, border: "1px solid #ccc" }} />
              <span>〜</span>
              <input type="date" value={customTo} onChange={(e) => setCustomTo(e.target.value)} style={{ padding: "4px 8px", borderRadius: 4, border: "1px solid #ccc" }} />
            </>
          )}
        </div>
      </div>

      {showForm && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3>{showForm === "petty" ? "小口登録" : "業者登録"}</h3>
          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <button
              className={txType === "Deposit" ? "btn-period active" : "btn-period"}
              onClick={() => setTxType("Deposit")}
            >
              入金
            </button>
            <button
              className={txType === "Withdrawal" ? "btn-period active" : "btn-period"}
              onClick={() => setTxType("Withdrawal")}
            >
              出金
            </button>
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <label>金額（円）</label>
              <input
                type="number"
                min="1"
                value={amount || ""}
                onChange={(e) => setAmount(Math.max(0, parseInt(e.target.value) || 0))}
                style={{ marginLeft: 8, width: 160 }}
              />
            </div>
            <div>
              <label>備考</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                style={{ marginLeft: 8, width: 300 }}
                placeholder={showForm === "petty"
                  ? (txType === "Deposit" ? "例: レジから金庫へ" : "例: 金庫からレジへ")
                  : (txType === "Deposit" ? "例: 釣り銭配達" : "例: 売上引渡")}
              />
            </div>
            <div>
              <label>日付</label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                style={{ marginLeft: 8 }}
              />
            </div>
          </div>
          <button className="btn-primary" onClick={handleSubmit} disabled={loading || amount <= 0} style={{ marginTop: 12 }}>
            {loading ? "処理中..." : (txType === "Deposit" ? "入金する" : "出金する")}
          </button>
        </div>
      )}

      <table>
        <thead>
          <tr>
            <th>種別</th>
            <th>金額</th>
            <th>摘要</th>
            <th>バッグ</th>
            <th>日時</th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td colSpan={5} style={{ textAlign: "center" }}>記録がありません</td>
            </tr>
          ) : (
            rows.map((row) =>
              row.kind === "tx" ? (
                <tr key={`tx-${row.data.id}`}>
                  <td>
                    <span className={`type ${row.data.type === "Deposit" ? "type-deposit" : "type-withdrawal"}`}>
                      {row.data.type === "Deposit" ? "入金" : "出金"}
                    </span>
                  </td>
                  <td>{row.data.amount.toLocaleString()}円</td>
                  <td>{row.data.description}</td>
                  <td>{formatBag(row.data.changeBagId, row.data.cashBagId, row.data.prepBagId, bags, cashBags, prepBags)}</td>
                  <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
                </tr>
              ) : (
                <tr key={`chk-${row.data.id}`} style={{ background: "#f0f7ff", cursor: "pointer" }} onClick={() => setEditCheck(row.data)}>
                  <td>
                    <span className="type type-check">有高</span>
                  </td>
                  <td>{row.data.checkedAmount.toLocaleString()}円</td>
                  <td>
                    有高: {row.data.checkedAmount.toLocaleString()}円 / 帳簿: {row.data.expectedAmount.toLocaleString()}円
                    <span style={{ color: row.data.difference === 0 ? "green" : "red", marginLeft: 8 }}>
                      （差額: {row.data.difference >= 0 ? "+" : ""}{row.data.difference.toLocaleString()}円）
                    </span>
                  </td>
                  <td>{formatBag(row.data.changeBagId, row.data.cashBagId, null, bags, cashBags, prepBags)}</td>
                  <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
                </tr>
              )
            )
          )}
        </tbody>
      </table>

      {editCheck && (
        <DenominationCheckForm
          bagId={editBagId}
          bagType={editBagType}
          expectedAmount={editExpected}
          onSubmit={(id, denom) =>
            editBagType === "change" ? api.checkChangeBag(id, denom) : editBagType === "prep" ? api.checkPrepBag(id, denom) : api.checkCashBag(id, denom)
          }
          onClose={() => setEditCheck(null)}
          onDone={onUpdate}
          editCheck={editCheck}
          onUpdate={(id, denom) => api.updateDenominationCheck(id, denom)}
        />
      )}
    </div>
  );
}

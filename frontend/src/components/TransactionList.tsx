import { useState } from "react";
import { api } from "../api/client";
import type { Transaction, DenominationCheck, ChangeBag, CashBag, PrepBag, Denomination } from "../api/client";
import { DenominationCheckForm } from "./DenominationCheckForm";
import { DenominationCheckPage } from "./DenominationCheckPage";
import { BagList } from "./BagList";
import { CashBagList } from "./CashBagList";
import { DenominationReportPage } from "./DenominationReportPage";

const DENOM_ITEMS = [
  { key: "count10000" as const, label: "1万円", value: 10000 },
  { key: "count5000" as const, label: "5千円", value: 5000 },
  { key: "count1000" as const, label: "千円", value: 1000 },
  { key: "count500" as const, label: "500円", value: 500 },
  { key: "count100" as const, label: "100円", value: 100 },
  { key: "count50" as const, label: "50円", value: 50 },
  { key: "count10" as const, label: "10円", value: 10 },
  { key: "count5" as const, label: "5円", value: 5 },
  { key: "count1" as const, label: "1円", value: 1 },
];

const emptyDenom = (): Denomination => ({
  count10000: 0, count5000: 0, count1000: 0, count500: 0, count100: 0, count50: 0, count10: 0, count5: 0, count1: 0,
});

const denomTotal = (d: Denomination) =>
  DENOM_ITEMS.reduce((sum, item) => sum + d[item.key] * item.value, 0);

interface Props {
  safeId: number;
  transactions: Transaction[];
  denomChecks: DenominationCheck[];
  bags: ChangeBag[];
  cashBags: CashBag[];
  prepBags: PrepBag[];
  onUpdate: () => void;
}

type View = "list" | "petty" | "vendor" | "denomCheck" | "denomReport" | "bagManagement";
type Period = "thisMonth" | "lastMonth" | "custom";

function getMonthRange(offset: number): [string, string] {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  const end = new Date(now.getFullYear(), now.getMonth() + offset + 1, 0);
  return [start.toISOString().slice(0, 10), end.toISOString().slice(0, 10)];
}

type Row =
  | { kind: "tx"; data: Transaction; at: string }
  | { kind: "check"; data: DenominationCheck; at: string };

const formatBagNo = (prefix: string, seq: number | null) =>
  seq ? `${prefix}-${String(seq).padStart(3, "0")}` : null;

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
    const no = formatBagNo("CA", bag?.depositSequenceNumber ?? null) ?? `CA-?`;
    return bag
      ? `${no} ${bag.totalAmount.toLocaleString()}円`
      : no;
  }
  if (cashBagId) {
    const bag = cashBags.find((b) => b.id === cashBagId);
    const no = formatBagNo("BAG", bag?.sequenceNumber ?? null) ?? `BAG-?`;
    return bag
      ? `${no} ${bag.totalAmount.toLocaleString()}円`
      : no;
  }
  if (prepBagId) {
    const pb = prepBags.find((p) => p.id === prepBagId);
    return pb
      ? `準備#${prepBagId} (${pb.cashBagIds.map((id) => `CB#${id}`).join(",")} / ${pb.totalAmount.toLocaleString()}円)`
      : `準備#${prepBagId}`;
  }
  return "-";
}

export function TransactionList({ safeId, transactions, denomChecks, bags, cashBags, prepBags, onUpdate }: Props) {
  const [view, setView] = useState<View>("list");
  const [editCheck, setEditCheck] = useState<DenominationCheck | null>(null);
  const [txType, setTxType] = useState<"Deposit" | "Withdrawal">("Deposit");
  const [amount, setAmount] = useState(0);
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [loading, setLoading] = useState(false);
  const [useDenom, setUseDenom] = useState(false);
  const [denom, setDenom] = useState<Denomination>(emptyDenom);
  const [viewDenomTx, setViewDenomTx] = useState<Transaction | null>(null);

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
  ].sort((a, b) => {
    const seqA = a.data.sequenceNumber;
    const seqB = b.data.sequenceNumber;
    return seqB - seqA;
  });

  const denomAmount = denomTotal(denom);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const formType = view === "petty" ? "petty" : "vendor";
      const label = formType === "petty"
        ? (txType === "Deposit" ? "小口入金" : "小口出金")
        : (txType === "Deposit" ? "業者入金" : "業者出金");
      const desc = description || label;
      const finalAmount = useDenom ? denomAmount : amount;
      await api.createTransaction({
        safeId, type: txType, amount: finalAmount, description: desc, date,
        ...(useDenom ? { denomination: denom } : {}),
      });
      setAmount(0);
      setDescription("");
      setDate(new Date().toISOString().slice(0, 10));
      setTxType("Deposit");
      setDenom(emptyDenom());
      setUseDenom(false);
      setView("list");
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

  const showForm = view === "petty" || view === "vendor";

  const toggleView = (v: View) => {
    setView(view === v ? "list" : v);
    setTxType("Deposit");
  };

  return (
    <div>
      {/* Header */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
        <h2 style={{ margin: 0 }}>出納帳</h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <button className={period === "lastMonth" ? "btn-period active" : "btn-period"} onClick={() => setPeriod("lastMonth")}>先月</button>
          <button className={period === "thisMonth" ? "btn-period active" : "btn-period"} onClick={() => setPeriod("thisMonth")}>今月</button>
          <button className={period === "custom" ? "btn-period active" : "btn-period"} onClick={() => setPeriod("custom")}>期間選択</button>
          {period === "custom" && (
            <>
              <input type="date" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} style={{ padding: "4px 8px", borderRadius: 4, border: "1px solid #ccc" }} />
              <span>〜</span>
              <input type="date" value={customTo} onChange={(e) => setCustomTo(e.target.value)} style={{ padding: "4px 8px", borderRadius: 4, border: "1px solid #ccc" }} />
            </>
          )}
        </div>
      </div>

      {/* Action bar (always visible) */}
      <div className="action-bar">
        <button className={view === "petty" ? "btn-action active" : "btn-action"} onClick={() => toggleView("petty")}>小口登録</button>
        <button className={view === "vendor" ? "btn-action active" : "btn-action"} onClick={() => toggleView("vendor")}>業者登録</button>
        <button className={view === "denomCheck" ? "btn-action active" : "btn-action"} onClick={() => toggleView("denomCheck")}>有高チェック</button>
        <button className={view === "denomReport" ? "btn-action active" : "btn-action"} onClick={() => toggleView("denomReport")}>金種表一覧</button>
        <button className={view === "bagManagement" ? "btn-action active" : "btn-action"} onClick={() => toggleView("bagManagement")}>バッグ管理</button>
      </div>

      {/* Content area */}
      {showForm && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3>{view === "petty" ? "小口登録" : "業者登録"}</h3>
          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <button className={txType === "Deposit" ? "btn-period active" : "btn-period"} onClick={() => setTxType("Deposit")}>入金</button>
            <button className={txType === "Withdrawal" ? "btn-period active" : "btn-period"} onClick={() => setTxType("Withdrawal")}>出金</button>
          </div>
          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <button className={!useDenom ? "btn-period active" : "btn-period"} onClick={() => setUseDenom(false)}>金額入力</button>
            <button className={useDenom ? "btn-period active" : "btn-period"} onClick={() => setUseDenom(true)}>金種入力</button>
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            {useDenom ? (
              <div>
                <div className="denomination-grid">
                  {DENOM_ITEMS.map((item) => (
                    <div className="denomination-row" key={item.key}>
                      <label>{item.label}</label>
                      <input
                        type="number" min="0"
                        value={denom[item.key] || ""}
                        onChange={(e) => setDenom({ ...denom, [item.key]: Math.max(0, parseInt(e.target.value) || 0) })}
                      />
                      <span>{(denom[item.key] * item.value).toLocaleString()}円</span>
                    </div>
                  ))}
                </div>
                <div className="total-row">
                  <strong>合計: {denomAmount.toLocaleString()}円</strong>
                </div>
              </div>
            ) : (
              <div>
                <label>金額（円）</label>
                <input
                  type="number" min="1" value={amount || ""}
                  onChange={(e) => setAmount(Math.max(0, parseInt(e.target.value) || 0))}
                  style={{ marginLeft: 8, width: 160 }}
                />
              </div>
            )}
            <div>
              <label>備考</label>
              <input
                type="text" value={description}
                onChange={(e) => setDescription(e.target.value)}
                style={{ marginLeft: 8, width: 300 }}
                placeholder={view === "petty"
                  ? (txType === "Deposit" ? "例: レジから金庫へ" : "例: 金庫からレジへ")
                  : (txType === "Deposit" ? "例: 釣り銭配達" : "例: 売上引渡")}
              />
            </div>
            <div>
              <label>日付</label>
              <input type="date" value={date} onChange={(e) => setDate(e.target.value)} style={{ marginLeft: 8 }} />
            </div>
          </div>
          <button className="btn-primary" onClick={handleSubmit} disabled={loading || (useDenom ? denomAmount <= 0 : amount <= 0)} style={{ marginTop: 12 }}>
            {loading ? "処理中..." : (txType === "Deposit" ? "入金する" : "出金する")}
          </button>
        </div>
      )}

      {view === "denomCheck" && (
        <DenominationCheckPage bags={bags} cashBags={cashBags} prepBags={prepBags} onDone={onUpdate} />
      )}

      {view === "denomReport" && (
        <DenominationReportPage denomChecks={denomChecks} bags={bags} cashBags={cashBags} prepBags={prepBags} onUpdate={onUpdate} />
      )}

      {view === "bagManagement" && (
        <>
          <BagList safeId={safeId} bags={bags} denomChecks={denomChecks} onUpdate={onUpdate} />
          <hr style={{ margin: "32px 0" }} />
          <CashBagList safeId={safeId} bags={cashBags} prepBags={prepBags} denomChecks={denomChecks} onUpdate={onUpdate} />
        </>
      )}

      {(view === "list" || showForm) && (
        <table>
          <thead>
            <tr>
              <th>番号</th>
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
                <td colSpan={6} style={{ textAlign: "center" }}>記録がありません</td>
              </tr>
            ) : (
              rows.map((row) =>
                row.kind === "tx" ? (
                  <tr key={`tx-${row.data.id}`}>
                    <td>{row.data.sequenceNumber}</td>
                    <td>
                      <span className={`type ${row.data.type === "Deposit" ? "type-deposit" : "type-withdrawal"}`}>
                        {row.data.type === "Deposit" ? "入金" : "出金"}
                      </span>
                      {row.data.denomination && (
                        <button
                          onClick={() => setViewDenomTx(row.data)}
                          style={{ marginLeft: 4, background: "none", border: "none", color: "#2563eb", cursor: "pointer", fontSize: "0.8rem", textDecoration: "underline" }}
                        >金種</button>
                      )}
                    </td>
                    <td>{row.data.amount.toLocaleString()}円</td>
                    <td>{row.data.description}</td>
                    <td>{formatBag(row.data.changeBagId, row.data.cashBagId, row.data.prepBagId, bags, cashBags, prepBags)}</td>
                    <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
                  </tr>
                ) : (
                  <tr key={`chk-${row.data.id}`} style={{ background: "#f0f7ff", cursor: "pointer" }} onClick={() => setEditCheck(row.data)}>
                    <td>{row.data.sequenceNumber}</td>
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
                    <td>{formatBag(row.data.changeBagId, row.data.cashBagId, row.data.prepBagId, bags, cashBags, prepBags)}</td>
                    <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
                  </tr>
                )
              )
            )}
          </tbody>
        </table>
      )}

      {viewDenomTx?.denomination && (
        <div style={{
          position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
          background: "rgba(0,0,0,0.4)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
        }}>
          <div className="card" style={{ background: "white", minWidth: 400, maxWidth: 480 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>金種内訳</h3>
              <button onClick={() => setViewDenomTx(null)} style={{ background: "none", border: "none", fontSize: 20, cursor: "pointer" }}>✕</button>
            </div>
            <p style={{ marginBottom: 8 }}>
              {viewDenomTx.type === "Deposit" ? "入金" : "出金"}: <strong>{viewDenomTx.amount.toLocaleString()}円</strong>
              {viewDenomTx.description && <span style={{ marginLeft: 8, color: "#666" }}>({viewDenomTx.description})</span>}
            </p>
            <table style={{ width: "100%" }}>
              <thead>
                <tr><th>金種</th><th>枚数</th><th>小計</th></tr>
              </thead>
              <tbody>
                {DENOM_ITEMS.map((item) => {
                  const count = viewDenomTx.denomination![item.key];
                  if (count === 0) return null;
                  return (
                    <tr key={item.key}>
                      <td>{item.label}</td>
                      <td>{count}</td>
                      <td style={{ textAlign: "right" }}>{(count * item.value).toLocaleString()}円</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            <div style={{ marginTop: 12, textAlign: "right", fontWeight: "bold" }}>
              合計: {viewDenomTx.amount.toLocaleString()}円
            </div>
            <button className="btn-primary" onClick={() => setViewDenomTx(null)} style={{ marginTop: 12 }}>閉じる</button>
          </div>
        </div>
      )}

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

import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { PettyCashTransaction, DenominationCheck, Denomination } from "../api/client";
import { DENOM_ITEMS } from "../shared/denomination";
import { DenominationCheckForm } from "./DenominationCheckForm";
import { DenominationInput } from "./DenominationInput";

interface Props {
  safeId: number;
  safeBalance: number;
  onUpdate: () => void;
}

type View = "list" | "input";
type Period = "thisMonth" | "lastMonth" | "custom";

function getMonthRange(offset: number): [string, string] {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  const end = new Date(now.getFullYear(), now.getMonth() + offset + 1, 0);
  return [start.toISOString().slice(0, 10), end.toISOString().slice(0, 10)];
}

type Row =
  | { kind: "tx"; data: PettyCashTransaction; at: string }
  | { kind: "check"; data: DenominationCheck; at: string };

export function PettyCashTab({ safeId, safeBalance, onUpdate }: Props) {
  const [view, setView] = useState<View>("list");
  const [transactions, setTransactions] = useState<PettyCashTransaction[]>([]);
  const [denomChecks, setDenomChecks] = useState<DenominationCheck[]>([]);

  // Form
  const [txType, setTxType] = useState<"Deposit" | "Withdrawal">("Deposit");
  const [amount, setAmount] = useState(0);
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [loading, setLoading] = useState(false);
  const [showDenomInput, setShowDenomInput] = useState(false);
  const [selectedDenom, setSelectedDenom] = useState<Denomination | null>(null);

  // Period
  const [period, setPeriod] = useState<Period>("thisMonth");
  const [thisMonth] = useState(() => getMonthRange(0));
  const [lastMonth] = useState(() => getMonthRange(-1));
  const [customFrom, setCustomFrom] = useState(thisMonth[0]);
  const [customTo, setCustomTo] = useState(thisMonth[1]);

  // Modals
  const [showDenomCheck, setShowDenomCheck] = useState(false);
  const [editCheck, setEditCheck] = useState<DenominationCheck | null>(null);
  const [viewDenomTx, setViewDenomTx] = useState<PettyCashTransaction | null>(null);

  const loadData = async () => {
    const [txData, checksData] = await Promise.all([
      api.getPettyCashTransactions(safeId),
      api.getPettyCashDenominationChecks(safeId),
    ]);
    setTransactions(txData);
    setDenomChecks(checksData);
  };

  useEffect(() => { loadData(); }, [safeId]);

  const handleUpdate = () => { loadData(); onUpdate(); };

  const [from, to] = period === "thisMonth" ? thisMonth : period === "lastMonth" ? lastMonth : [customFrom, customTo];
  const inRange = (dateStr: string) => {
    const d = dateStr.slice(0, 10);
    return d >= from && d <= to;
  };

  const rows: Row[] = [
    ...transactions.filter(t => inRange(t.createdAt)).map(t => ({ kind: "tx" as const, data: t, at: t.createdAt })),
    ...denomChecks.filter(c => inRange(c.createdAt)).map(c => ({ kind: "check" as const, data: c, at: c.createdAt })),
  ].sort((a, b) => b.data.sequenceNumber - a.data.sequenceNumber);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const label = txType === "Deposit" ? "小口入金" : "小口出金";
      const desc = description || label;
      await api.createPettyCashTransaction({
        safeId, type: txType, amount, description: desc, date,
        ...(selectedDenom ? { denomination: selectedDenom } : {}),
      });
      setAmount(0);
      setDescription("");
      setDate(new Date().toISOString().slice(0, 10));
      setTxType("Deposit");
      setSelectedDenom(null);
      setView("list");
      handleUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      {/* Header */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
        <h2 style={{ margin: 0 }}>小口出納帳</h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <button className={period === "lastMonth" ? "btn-period active" : "btn-period"} onClick={() => setPeriod("lastMonth")}>先月</button>
          <button className={period === "thisMonth" ? "btn-period active" : "btn-period"} onClick={() => setPeriod("thisMonth")}>今月</button>
          <button className={period === "custom" ? "btn-period active" : "btn-period"} onClick={() => setPeriod("custom")}>期間選択</button>
          {period === "custom" && (
            <>
              <input type="date" value={customFrom} onChange={e => setCustomFrom(e.target.value)} style={{ padding: "4px 8px", borderRadius: 4, border: "1px solid #ccc" }} />
              <span>〜</span>
              <input type="date" value={customTo} onChange={e => setCustomTo(e.target.value)} style={{ padding: "4px 8px", borderRadius: 4, border: "1px solid #ccc" }} />
            </>
          )}
        </div>
      </div>

      {/* Action bar */}
      <div className="action-bar">
        <button className={view === "input" ? "btn-action active" : "btn-action"} onClick={() => setView(view === "input" ? "list" : "input")}>入出金登録</button>
        <button className="btn-action" onClick={() => setShowDenomCheck(true)}>有高チェック</button>
      </div>

      {/* Input form */}
      {view === "input" && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3>小口 入出金登録</h3>
          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <button className={txType === "Deposit" ? "btn-period active" : "btn-period"} onClick={() => setTxType("Deposit")}>入金</button>
            <button className={txType === "Withdrawal" ? "btn-period active" : "btn-period"} onClick={() => setTxType("Withdrawal")}>出金</button>
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <label>金額（円）</label>
              <input
                type="number" min="1" value={amount || ""}
                onChange={e => { setAmount(Math.max(0, parseInt(e.target.value) || 0)); setSelectedDenom(null); }}
                style={{ width: 160 }}
              />
              <button
                className="btn-action"
                onClick={() => setShowDenomInput(true)}
                style={{ padding: "6px 14px", fontSize: "0.85rem" }}
              >
                金種表
              </button>
              {selectedDenom && (
                <span style={{ fontSize: "0.85rem", color: "#2563eb" }}>（金種入力済み）</span>
              )}
            </div>
            <div>
              <label>備考</label>
              <input type="text" value={description} onChange={e => setDescription(e.target.value)} style={{ marginLeft: 8, width: 300 }}
                placeholder={txType === "Deposit" ? "例: レジから金庫へ" : "例: 金庫からレジへ"} />
            </div>
            <div>
              <label>日付</label>
              <input type="date" value={date} onChange={e => setDate(e.target.value)} style={{ marginLeft: 8 }} />
            </div>
          </div>
          <button className="btn-primary" onClick={handleSubmit} disabled={loading || amount <= 0} style={{ marginTop: 12 }}>
            {loading ? "処理中..." : (txType === "Deposit" ? "入金する" : "出金する")}
          </button>
        </div>
      )}

      {/* Ledger table */}
      <table>
        <thead>
          <tr>
            <th>番号</th>
            <th>種別</th>
            <th>金額</th>
            <th>摘要</th>
            <th>日時</th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr><td colSpan={5} style={{ textAlign: "center" }}>記録がありません</td></tr>
          ) : rows.map(row =>
            row.kind === "tx" ? (
              <tr key={`tx-${row.data.id}`}>
                <td>{row.data.sequenceNumber}</td>
                <td>
                  <span className={`type ${row.data.type === "Adjustment" ? "type-adjustment" : row.data.type === "Deposit" ? "type-deposit" : "type-withdrawal"}`}>
                    {row.data.type === "Adjustment" ? "調整" : row.data.type === "Deposit" ? "入金" : "出金"}
                  </span>
                  {row.data.denomination && (
                    <button onClick={() => setViewDenomTx(row.data)}
                      style={{ marginLeft: 4, background: "none", border: "none", color: "#2563eb", cursor: "pointer", fontSize: "0.8rem", textDecoration: "underline" }}>金種</button>
                  )}
                </td>
                <td>{row.data.amount.toLocaleString()}円</td>
                <td>{row.data.description}</td>
                <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
              </tr>
            ) : (
              <tr key={`chk-${row.data.id}`} style={{ background: "#f0f7ff", cursor: "pointer" }} onClick={() => setEditCheck(row.data)}>
                <td>{row.data.sequenceNumber}</td>
                <td><span className="type type-check">有高</span></td>
                <td>{row.data.checkedAmount.toLocaleString()}円</td>
                <td>
                  有高: {row.data.checkedAmount.toLocaleString()}円 / 帳簿: {row.data.expectedAmount.toLocaleString()}円
                  <span style={{ color: row.data.difference === 0 ? "green" : "red", marginLeft: 8 }}>
                    （差額: {row.data.difference >= 0 ? "+" : ""}{row.data.difference.toLocaleString()}円）
                  </span>
                </td>
                <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
              </tr>
            )
          )}
        </tbody>
      </table>

      {/* Denomination detail modal */}
      {viewDenomTx?.denomination && (
        <div style={{ position: "fixed", top: 0, left: 0, right: 0, bottom: 0, background: "rgba(0,0,0,0.4)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000 }}>
          <div className="card" style={{ background: "white", minWidth: 400, maxWidth: 480 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>金種内訳</h3>
              <button onClick={() => setViewDenomTx(null)} style={{ background: "none", border: "none", fontSize: 20, cursor: "pointer" }}>✕</button>
            </div>
            <p style={{ marginBottom: 8 }}>
              {viewDenomTx.type === "Adjustment" ? "調整" : viewDenomTx.type === "Deposit" ? "入金" : "出金"}: <strong>{viewDenomTx.amount.toLocaleString()}円</strong>
              {viewDenomTx.description && <span style={{ marginLeft: 8, color: "#666" }}>({viewDenomTx.description})</span>}
            </p>
            <table style={{ width: "100%" }}>
              <thead><tr><th>金種</th><th>枚数</th><th>小計</th></tr></thead>
              <tbody>
                {DENOM_ITEMS.map(item => {
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
            <div style={{ marginTop: 12, textAlign: "right", fontWeight: "bold" }}>合計: {viewDenomTx.amount.toLocaleString()}円</div>
            <button className="btn-primary" onClick={() => setViewDenomTx(null)} style={{ marginTop: 12 }}>閉じる</button>
          </div>
        </div>
      )}

      {/* Safe denomination check modal */}
      {showDenomCheck && (() => {
        const safeChecks = [...denomChecks].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
        const lastCheck = safeChecks.length > 0 ? safeChecks[0] : null;
        const initDenom = lastCheck
          ? { count10000: lastCheck.count10000, count5000: lastCheck.count5000, count1000: lastCheck.count1000, count500: lastCheck.count500, count100: lastCheck.count100, count50: lastCheck.count50, count10: lastCheck.count10, count5: lastCheck.count5, count1: lastCheck.count1 }
          : undefined;
        return (
          <DenominationCheckForm
            bagId={safeId}
            bagType="safe"
            expectedAmount={safeBalance}
            onSubmit={(id, denom) => api.checkSafe(id, denom)}
            onClose={() => setShowDenomCheck(false)}
            onDone={handleUpdate}
            initialDenom={initDenom}
          />
        );
      })()}

      {/* Denomination input modal (金種表→金額セット) */}
      {showDenomInput && (
        <DenominationInput
          initialDenom={selectedDenom ?? undefined}
          onComplete={(denom, total) => {
            setSelectedDenom(denom);
            setAmount(total);
            setShowDenomInput(false);
          }}
          onCancel={() => setShowDenomInput(false)}
        />
      )}

      {/* Edit denomination check modal */}
      {editCheck && (
        <DenominationCheckForm
          bagId={safeId}
          bagType="safe"
          expectedAmount={safeBalance}
          onSubmit={(id, denom) => api.checkSafe(id, denom)}
          onClose={() => setEditCheck(null)}
          onDone={handleUpdate}
          editCheck={editCheck}
          onUpdate={(id, denom) => api.updatePettyCashDenominationCheck(id, denom)}
        />
      )}
    </div>
  );
}

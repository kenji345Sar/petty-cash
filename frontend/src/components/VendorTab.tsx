import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { VendorTransaction, DenominationCheck, ChangeBag, CashBag, PrepBag } from "../api/client";
import { DENOM_ITEMS } from "../shared/denomination";
import { BagList } from "./BagList";
import { CashBagList } from "./CashBagList";
import { DenominationCheckForm } from "./DenominationCheckForm";

interface Props {
  safeId: number;
  onUpdate: () => void;
}

type Period = "thisMonth" | "lastMonth" | "custom";

function getMonthRange(offset: number): [string, string] {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  const end = new Date(now.getFullYear(), now.getMonth() + offset + 1, 0);
  return [start.toISOString().slice(0, 10), end.toISOString().slice(0, 10)];
}

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
    const bag = bags.find(b => b.id === changeBagId);
    const no = formatBagNo("CA", bag?.depositSequenceNumber ?? null) ?? `CA-?`;
    return bag ? `${no} ${bag.totalAmount.toLocaleString()}円` : no;
  }
  if (cashBagId) {
    const bag = cashBags.find(b => b.id === cashBagId);
    const no = formatBagNo("BAG", bag?.sequenceNumber ?? null) ?? `BAG-?`;
    return bag ? `${no} ${bag.totalAmount.toLocaleString()}円` : no;
  }
  if (prepBagId) {
    const pb = prepBags.find(p => p.id === prepBagId);
    return pb
      ? `準備#${prepBagId} (${pb.cashBagIds.map(id => `CB#${id}`).join(",")} / ${pb.totalAmount.toLocaleString()}円)`
      : `準備#${prepBagId}`;
  }
  return "-";
}

type Row =
  | { kind: "tx"; data: VendorTransaction; at: string }
  | { kind: "check"; data: DenominationCheck; at: string };

export function VendorTab({ safeId, onUpdate }: Props) {
  const [bags, setBags] = useState<ChangeBag[]>([]);
  const [cashBags, setCashBags] = useState<CashBag[]>([]);
  const [prepBags, setPrepBags] = useState<PrepBag[]>([]);
  const [transactions, setVendorTransactions] = useState<VendorTransaction[]>([]);
  const [denomChecks, setDenomChecks] = useState<DenominationCheck[]>([]);

  // Period
  const [period, setPeriod] = useState<Period>("thisMonth");
  const [thisMonth] = useState(() => getMonthRange(0));
  const [lastMonth] = useState(() => getMonthRange(-1));
  const [customFrom, setCustomFrom] = useState(thisMonth[0]);
  const [customTo, setCustomTo] = useState(thisMonth[1]);

  // Modals
  const [editCheck, setEditCheck] = useState<DenominationCheck | null>(null);
  const [viewDenomTx, setViewDenomTx] = useState<VendorTransaction | null>(null);

  const loadData = async () => {
    const [bagsData, cashBagsData, txData, checksData, prepBagsData] = await Promise.all([
      api.getBags(safeId),
      api.getCashBags(safeId),
      api.getVendorTransactions(safeId),
      api.getDenominationChecks(safeId),
      api.getPrepBags(safeId),
    ]);
    setBags(bagsData);
    setCashBags(cashBagsData);
    setVendorTransactions(txData);
    setDenomChecks(checksData.filter(c => c.changeBagId || c.cashBagId || c.prepBagId));
    setPrepBags(prepBagsData);
  };

  useEffect(() => { loadData(); }, [safeId]);

  const handleUpdate = () => { loadData(); onUpdate(); };

  // Ledger filtering
  const [from, to] = period === "thisMonth" ? thisMonth : period === "lastMonth" ? lastMonth : [customFrom, customTo];
  const inRange = (dateStr: string) => {
    const d = dateStr.slice(0, 10);
    return d >= from && d <= to;
  };

  const rows: Row[] = [
    ...transactions.filter(t => inRange(t.createdAt)).map(t => ({ kind: "tx" as const, data: t, at: t.createdAt })),
    ...denomChecks.filter(c => inRange(c.createdAt)).map(c => ({ kind: "check" as const, data: c, at: c.createdAt })),
  ].sort((a, b) => b.data.sequenceNumber - a.data.sequenceNumber);

  // Edit check helpers
  const editBagType = editCheck?.changeBagId ? "change" as const : editCheck?.prepBagId ? "prep" as const : "cash" as const;
  const editBagId = editCheck?.changeBagId ?? editCheck?.cashBagId ?? editCheck?.prepBagId ?? 0;
  const editExpected = editCheck?.changeBagId
    ? bags.find(b => b.id === editCheck.changeBagId)?.totalAmount ?? 0
    : editCheck?.prepBagId
    ? prepBags.find(p => p.id === editCheck.prepBagId)?.totalAmount ?? 0
    : cashBags.find(b => b.id === editCheck?.cashBagId)?.totalAmount ?? 0;

  // Period-filtered data for bag lists
  const filteredBags = bags.filter(b => inRange(b.createdAt));
  const filteredCashBags = cashBags.filter(b => inRange(b.createdAt));
  const filteredPrepBags = prepBags.filter(p => inRange(p.createdAt));

  return (
    <div>
      {/* ===== 期間フィルタ（全体共通） ===== */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>業者管理</h2>
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

      {/* ===== バッグ管理 ===== */}
      <BagList safeId={safeId} bags={filteredBags} denomChecks={denomChecks} onUpdate={handleUpdate} />
      <hr style={{ margin: "32px 0" }} />
      <CashBagList safeId={safeId} bags={filteredCashBags} prepBags={filteredPrepBags} denomChecks={denomChecks} onUpdate={handleUpdate} />

      {/* ===== 業者出納帳 ===== */}
      <div style={{ marginTop: 40 }}>
        <h2 style={{ margin: 0, marginBottom: 12 }}>業者出納帳</h2>
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
              <tr><td colSpan={6} style={{ textAlign: "center" }}>記録がありません</td></tr>
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
                  <td>{formatBag(row.data.changeBagId, row.data.cashBagId, row.data.prepBagId, bags, cashBags, prepBags)}</td>
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
                  <td>{formatBag(row.data.changeBagId, row.data.cashBagId, row.data.prepBagId, bags, cashBags, prepBags)}</td>
                  <td>{new Date(row.data.createdAt).toLocaleString("ja-JP")}</td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>

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

      {/* Edit denomination check modal */}
      {editCheck && (
        <DenominationCheckForm
          bagId={editBagId}
          bagType={editBagType}
          expectedAmount={editExpected}
          onSubmit={(id, denom) =>
            editBagType === "change" ? api.checkChangeBag(id, denom) : editBagType === "prep" ? api.checkPrepBag(id, denom) : api.checkCashBag(id, denom)
          }
          onClose={() => setEditCheck(null)}
          onDone={handleUpdate}
          editCheck={editCheck}
          onUpdate={(id, denom) => api.updateDenominationCheck(id, denom)}
        />
      )}
    </div>
  );
}

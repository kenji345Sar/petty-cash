import { useState } from "react";
import { api } from "../api/client";
import type { Denomination, DenominationCheck, ChangeBag, CashBag, PrepBag } from "../api/client";

interface Props {
  bags: ChangeBag[];
  cashBags: CashBag[];
  prepBags: PrepBag[];
  onDone: () => void;
}

const DENOMINATIONS = [
  { key: "count10000", label: "1万円", value: 10000 },
  { key: "count5000", label: "5千円", value: 5000 },
  { key: "count1000", label: "千円", value: 1000 },
  { key: "count500", label: "500円", value: 500, perSet: 50 },
  { key: "count100", label: "100円", value: 100, perSet: 50 },
  { key: "count50", label: "50円", value: 50, perSet: 50 },
  { key: "count10", label: "10円", value: 10, perSet: 50 },
  { key: "count5", label: "5円", value: 5, perSet: 50 },
  { key: "count1", label: "1円", value: 1, perSet: 50 },
] as const;

type BagType = "change" | "cash" | "prep";

const zeroCounts = (): Denomination => ({
  count10000: 0, count5000: 0, count1000: 0, count500: 0,
  count100: 0, count50: 0, count10: 0, count5: 0, count1: 0,
});

export function DenominationCheckPage({ bags, cashBags, prepBags, onDone }: Props) {
  const [bagType, setBagType] = useState<BagType>("cash");
  const [selectedBagId, setSelectedBagId] = useState<number | null>(null);
  const [counts, setCounts] = useState(zeroCounts);
  const [sets, setSets] = useState(zeroCounts);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<DenominationCheck | null>(null);

  const fmtId = (prefix: string, id: number) => `${prefix}-${String(id).padStart(3, "0")}`;
  const bagOptions = bagType === "change"
    ? bags.map((b) => ({ id: b.id, label: `${fmtId("CA", b.id)} ${b.description || ""} (${b.totalAmount.toLocaleString()}円)`, amount: b.totalAmount }))
    : bagType === "cash"
    ? cashBags.map((b) => ({ id: b.id, label: `${fmtId("BAG", b.id)} ${b.description || ""} (${b.totalAmount.toLocaleString()}円)`, amount: b.totalAmount }))
    : prepBags.filter((p) => p.status === "Preparing").map((p) => ({ id: p.id, label: `${fmtId("準備", p.id)} (${p.totalAmount.toLocaleString()}円)`, amount: p.totalAmount }));

  const selectedBag = bagOptions.find((b) => b.id === selectedBagId);
  const expectedAmount = selectedBag?.amount ?? 0;

  const totalCounts = (key: string) => {
    const k = key as keyof Denomination;
    const d = DENOMINATIONS.find((d) => d.key === key)!;
    return counts[k] + ("perSet" in d ? sets[k] * d.perSet : 0);
  };

  const total = DENOMINATIONS.reduce((sum, d) => sum + totalCounts(d.key) * d.value, 0);
  const difference = selectedBag ? total - expectedAmount : 0;

  const handleChangeBagType = (type: BagType) => {
    setBagType(type);
    setSelectedBagId(null);
    setCounts(zeroCounts());
    setSets(zeroCounts());
    setResult(null);
  };

  const handleSelectBag = (id: number | null) => {
    setSelectedBagId(id);
    setCounts(zeroCounts());
    setSets(zeroCounts());
    setResult(null);
  };

  const handleSubmit = async () => {
    if (!selectedBagId) return;
    setLoading(true);
    try {
      const merged: Denomination = { ...counts };
      for (const d of DENOMINATIONS) {
        const k = d.key as keyof Denomination;
        merged[k] = counts[k] + ("perSet" in d ? sets[k] * d.perSet : 0);
      }
      let check: DenominationCheck;
      if (bagType === "change") {
        check = await api.checkChangeBag(selectedBagId, merged);
      } else if (bagType === "cash") {
        check = await api.checkCashBag(selectedBagId, merged);
      } else {
        check = await api.checkPrepBag(selectedBagId, merged);
      }
      setResult(check);
      onDone();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    setCounts(zeroCounts());
    setSets(zeroCounts());
    setResult(null);
  };

  return (
    <div>
      <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 16 }}>
        <label style={{ fontWeight: "bold" }}>バッグ種別:</label>
        <div style={{ display: "flex", gap: 4 }}>
          {([["change", "両替金"], ["cash", "キャッシュ"], ["prep", "準備"]] as const).map(([type, label]) => (
            <button
              key={type}
              className={bagType === type ? "btn-period active" : "btn-period"}
              onClick={() => handleChangeBagType(type)}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 20 }}>
        <label style={{ fontWeight: "bold" }}>バッグ選択:</label>
        <select
          value={selectedBagId ?? ""}
          onChange={(e) => handleSelectBag(e.target.value ? Number(e.target.value) : null)}
          style={{ padding: "6px 12px", borderRadius: 4, border: "1px solid #ccc", minWidth: 300 }}
        >
          <option value="">-- 選択してください --</option>
          {bagOptions.map((b) => (
            <option key={b.id} value={b.id}>{b.label}</option>
          ))}
        </select>
      </div>

      {selectedBag && !result && (
        <div className="card">
          <p style={{ marginBottom: 12 }}>帳簿金額: <strong>{expectedAmount.toLocaleString()}円</strong></p>
          <table style={{ width: "100%" }}>
            <thead>
              <tr>
                <th>金種</th>
                <th>セット</th>
                <th>バラ</th>
                <th>小計</th>
              </tr>
            </thead>
            <tbody>
              {DENOMINATIONS.map((d) => {
                const k = d.key as keyof Denomination;
                const hasSet = "perSet" in d;
                return (
                  <tr key={d.key}>
                    <td>{d.label}</td>
                    <td>
                      {hasSet ? (
                        <>
                          <input
                            type="number" min="0" value={sets[k] || ""}
                            onChange={(e) => setSets({ ...sets, [d.key]: Math.max(0, parseInt(e.target.value) || 0) })}
                            style={{ width: 60 }}
                          />
                          <span style={{ fontSize: "0.8rem", color: "#666", marginLeft: 2 }}>×{d.perSet}</span>
                        </>
                      ) : (
                        <span style={{ color: "#aaa" }}>-</span>
                      )}
                    </td>
                    <td>
                      <input
                        type="number" min="0" value={counts[k] || ""}
                        onChange={(e) => setCounts({ ...counts, [d.key]: Math.max(0, parseInt(e.target.value) || 0) })}
                        style={{ width: 60 }}
                      />
                    </td>
                    <td style={{ textAlign: "right" }}>{(totalCounts(d.key) * d.value).toLocaleString()}円</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <div style={{ marginTop: 12, padding: "8px 0", borderTop: "2px solid #333" }}>
            <p><strong>合計: {total.toLocaleString()}円</strong></p>
            <p style={{ color: difference === 0 ? "green" : "red" }}>
              差額: {difference >= 0 ? "+" : ""}{difference.toLocaleString()}円
            </p>
          </div>
          <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
            <button className="btn-primary" onClick={handleSubmit} disabled={loading}>
              {loading ? "処理中..." : "チェック結果を保存"}
            </button>
            <button onClick={handleReset} style={{ padding: "8px 16px" }}>リセット</button>
          </div>
        </div>
      )}

      {result && (
        <div className="card">
          <h3>チェック結果</h3>
          <p>有高: <strong>{result.checkedAmount.toLocaleString()}円</strong></p>
          <p>帳簿: <strong>{result.expectedAmount.toLocaleString()}円</strong></p>
          <p style={{ color: result.difference === 0 ? "green" : "red", fontWeight: "bold" }}>
            差額: {result.difference >= 0 ? "+" : ""}{result.difference.toLocaleString()}円
            {result.difference === 0 ? " （一致）" : " （不一致）"}
          </p>
          <button className="btn-primary" onClick={handleReset} style={{ marginTop: 12 }}>
            続けてチェック
          </button>
        </div>
      )}

      {!selectedBag && (
        <p style={{ color: "#888", marginTop: 20 }}>バッグを選択してください。</p>
      )}
    </div>
  );
}

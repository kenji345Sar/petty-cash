import { useState } from "react";
import type { Denomination, DenominationCheck } from "../api/client";

interface Props {
  bagId: number;
  bagType: "change" | "cash" | "prep" | "safe";
  expectedAmount: number;
  onSubmit: (bagId: number, denomination: Denomination) => Promise<DenominationCheck>;
  onClose: () => void;
  onDone: () => void;
  editCheck?: DenominationCheck;
  onUpdate?: (id: number, denomination: Denomination) => Promise<DenominationCheck>;
  initialDenom?: Denomination;
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

const zeroDenom = (): Denomination =>
  ({ count10000: 0, count5000: 0, count1000: 0, count500: 0, count100: 0, count50: 0, count10: 0, count5: 0, count1: 0 });

const denomFromCheck = (check: DenominationCheck): Denomination => ({
  count10000: check.count10000,
  count5000: check.count5000,
  count1000: check.count1000,
  count500: check.count500,
  count100: check.count100,
  count50: check.count50,
  count10: check.count10,
  count5: check.count5,
  count1: check.count1,
});

const initialCounts = (check?: DenominationCheck, denom?: Denomination): Denomination =>
  check ? denomFromCheck(check) : denom ? { ...denom } : zeroDenom();

const initialSets = () =>
  ({ count10000: 0, count5000: 0, count1000: 0, count500: 0, count100: 0, count50: 0, count10: 0, count5: 0, count1: 0 });

export function DenominationCheckForm({ bagId, bagType, expectedAmount, onSubmit, onClose, onDone, editCheck, onUpdate, initialDenom }: Props) {
  const [counts, setCounts] = useState(() => initialCounts(editCheck, initialDenom));
  const [sets, setSets] = useState(initialSets);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<DenominationCheck | null>(null);

  const isEdit = !!editCheck;

  const totalCounts = (key: string) => {
    const k = key as keyof Denomination;
    const d = DENOMINATIONS.find((d) => d.key === key)!;
    return counts[k] + ("perSet" in d ? sets[k] * d.perSet : 0);
  };

  const total = DENOMINATIONS.reduce((sum, d) => sum + totalCounts(d.key) * d.value, 0);
  const difference = total - expectedAmount;

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const merged: Denomination = { ...counts };
      for (const d of DENOMINATIONS) {
        const k = d.key as keyof Denomination;
        merged[k] = counts[k] + ("perSet" in d ? sets[k] * d.perSet : 0);
      }
      let check: DenominationCheck;
      if (isEdit && onUpdate) {
        check = await onUpdate(editCheck.id, merged);
      } else {
        check = await onSubmit(bagId, merged);
      }
      setResult(check);
      onDone();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
      background: "rgba(0,0,0,0.4)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
    }}>
      <div className="card" style={{ background: "white", minWidth: 480, maxWidth: 580, maxHeight: "90vh", overflow: "auto" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
          <h3 style={{ margin: 0 }}>
            {isEdit ? "有高修正" : "有高チェック"}（{bagType === "safe" ? "金庫全体" : bagType === "change" ? `CA-${String(bagId).padStart(3, "0")}` : bagType === "cash" ? `BAG-${String(bagId).padStart(3, "0")}` : `準備-${String(bagId).padStart(3, "0")}`}）
          </h3>
          <button onClick={onClose} style={{ background: "none", border: "none", fontSize: 20, cursor: "pointer" }}>✕</button>
        </div>

        {result ? (
          <div>
            <p>有高: <strong>{result.checkedAmount.toLocaleString()}円</strong></p>
            <p>帳簿: <strong>{result.expectedAmount.toLocaleString()}円</strong></p>
            <p style={{ color: result.difference === 0 ? "green" : "red", fontWeight: "bold" }}>
              差額: {result.difference >= 0 ? "+" : ""}{result.difference.toLocaleString()}円
              {result.difference === 0 ? " （一致）" : " （不一致）"}
            </p>
            <button className="btn-primary" onClick={onClose} style={{ marginTop: 12 }}>閉じる</button>
          </div>
        ) : (
          <>
            <p style={{ marginBottom: 8 }}>帳簿金額: <strong>{expectedAmount.toLocaleString()}円</strong></p>
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
                              type="number"
                              min="0"
                              value={sets[k] || ""}
                              onChange={(e) => setSets({ ...sets, [d.key]: Math.max(0, parseInt(e.target.value) || 0) })}
                              style={{ width: 60 }}
                            />
                            <span style={{ fontSize: "0.8rem", color: "#666", marginLeft: 2 }}>
                              ×{d.perSet}
                            </span>
                          </>
                        ) : (
                          <span style={{ color: "#aaa" }}>-</span>
                        )}
                      </td>
                      <td>
                        <input
                          type="number"
                          min="0"
                          value={counts[k] || ""}
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
                {loading ? "処理中..." : isEdit ? "修正を保存" : "チェック結果を保存"}
              </button>
              <button onClick={onClose} style={{ padding: "8px 16px" }}>キャンセル</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

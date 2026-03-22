import { useState } from "react";
import type { Denomination } from "../api/client";
import { DENOM_ITEMS, emptyDenom, denomTotal } from "../shared/denomination";

interface Props {
  onComplete: (denomination: Denomination, totalAmount: number) => void;
  onCancel: () => void;
  initialDenom?: Denomination;
}

export function DenominationInput({ onComplete, onCancel, initialDenom }: Props) {
  const [denom, setDenom] = useState<Denomination>(() => initialDenom ?? emptyDenom());

  const total = denomTotal(denom);

  return (
    <div style={{
      position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
      background: "rgba(0,0,0,0.4)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
    }}>
      <div className="card" style={{ background: "white", minWidth: 480, maxWidth: 580, maxHeight: "90vh", overflow: "auto" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
          <h3 style={{ margin: 0 }}>金種表入力</h3>
          <button onClick={onCancel} style={{ background: "none", border: "none", fontSize: 20, cursor: "pointer" }}>✕</button>
        </div>

        <table style={{ width: "100%" }}>
          <thead>
            <tr>
              <th>金種</th>
              <th>枚数</th>
              <th>小計</th>
            </tr>
          </thead>
          <tbody>
            {DENOM_ITEMS.map(item => {
              const k = item.key as keyof Denomination;
              return (
                <tr key={item.key}>
                  <td>{item.label}</td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      value={denom[k] || ""}
                      onChange={e => setDenom({ ...denom, [k]: Math.max(0, parseInt(e.target.value) || 0) })}
                      style={{ width: 80 }}
                    />
                  </td>
                  <td style={{ textAlign: "right" }}>{(denom[k] * item.value).toLocaleString()}円</td>
                </tr>
              );
            })}
          </tbody>
        </table>

        <div style={{ marginTop: 12, padding: "8px 0", borderTop: "2px solid #333" }}>
          <strong>合計: {total.toLocaleString()}円</strong>
        </div>

        <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
          <button
            className="btn-primary"
            onClick={() => onComplete(denom, total)}
            disabled={total <= 0}
          >
            確定
          </button>
          <button onClick={onCancel} style={{ padding: "8px 16px" }}>キャンセル</button>
        </div>
      </div>
    </div>
  );
}

import { useState } from "react";
import { api } from "../api/client";
import type { Denomination, DenominationCheck, ChangeBag, CashBag, PrepBag } from "../api/client";

interface Props {
  denomChecks: DenominationCheck[];
  bags: ChangeBag[];
  cashBags: CashBag[];
  prepBags: PrepBag[];
  onUpdate: () => void;
}

const DENOM_LABELS = [
  { key: "count10000", label: "1万" },
  { key: "count5000", label: "5千" },
  { key: "count1000", label: "千" },
  { key: "count500", label: "500" },
  { key: "count100", label: "100" },
  { key: "count50", label: "50" },
  { key: "count10", label: "10" },
  { key: "count5", label: "5" },
  { key: "count1", label: "1" },
] as const;

function bagLabel(check: DenominationCheck, bags: ChangeBag[], cashBags: CashBag[], prepBags: PrepBag[]) {
  if (check.changeBagId) {
    const b = bags.find((b) => b.id === check.changeBagId);
    return `釣り銭#${check.changeBagId}${b ? ` (${b.totalAmount.toLocaleString()}円)` : ""}`;
  }
  if (check.cashBagId) {
    const b = cashBags.find((b) => b.id === check.cashBagId);
    return `キャッシュ#${check.cashBagId}${b ? ` (${b.totalAmount.toLocaleString()}円)` : ""}`;
  }
  if (check.prepBagId) {
    const p = prepBags.find((p) => p.id === check.prepBagId);
    return `準備#${check.prepBagId}${p ? ` (${p.totalAmount.toLocaleString()}円)` : ""}`;
  }
  return "-";
}

function denomSummary(check: DenominationCheck) {
  return DENOM_LABELS
    .map((d) => ({ label: d.label, count: check[d.key as keyof DenominationCheck] as number }))
    .filter((d) => d.count > 0)
    .map((d) => `${d.label}×${d.count}`)
    .join(" ");
}

export function DenominationReportPage({ denomChecks, bags, cashBags, prepBags, onUpdate }: Props) {
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const sorted = [...denomChecks].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  );

  return (
    <div>
      {sorted.length === 0 ? (
        <p style={{ color: "#888" }}>有高チェック記録がありません。</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>バッグ</th>
              <th>有高</th>
              <th>帳簿</th>
              <th>差額</th>
              <th>金種内訳</th>
              <th>日時</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((check) => (
              <tr
                key={check.id}
                style={{ cursor: "pointer", background: expandedId === check.id ? "#f0f7ff" : undefined }}
                onClick={() => setExpandedId(expandedId === check.id ? null : check.id)}
              >
                <td>{check.id}</td>
                <td>{bagLabel(check, bags, cashBags, prepBags)}</td>
                <td>{check.checkedAmount.toLocaleString()}円</td>
                <td>{check.expectedAmount.toLocaleString()}円</td>
                <td>
                  <span style={{ color: check.difference === 0 ? "green" : "red", fontWeight: "bold" }}>
                    {check.difference >= 0 ? "+" : ""}{check.difference.toLocaleString()}円
                  </span>
                </td>
                <td style={{ fontSize: "0.8rem", color: "#666" }}>
                  {denomSummary(check)}
                </td>
                <td style={{ fontSize: "0.85rem" }}>
                  {new Date(check.createdAt).toLocaleString("ja-JP")}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

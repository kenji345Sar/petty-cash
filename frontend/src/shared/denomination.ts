import type { Denomination } from "../api/client";

export const DENOM_ITEMS = [
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

export const emptyDenom = (): Denomination => ({
  count10000: 0, count5000: 0, count1000: 0, count500: 0,
  count100: 0, count50: 0, count10: 0, count5: 0, count1: 0,
});

export const denomTotal = (d: Denomination) =>
  DENOM_ITEMS.reduce((sum, item) => sum + d[item.key] * item.value, 0);

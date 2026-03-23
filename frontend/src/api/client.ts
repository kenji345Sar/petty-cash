export interface Denomination {
  count10000: number;
  count5000: number;
  count1000: number;
  count500: number;
  count100: number;
  count50: number;
  count10: number;
  count5: number;
  count1: number;
}

export interface Safe {
  id: number;
  name: string;
  description: string;
  currentBalance: number;
  vendorBalance: number;
  pettyCashBalance: number;
  createdAt: string;
}

export interface DepositRequest {
  safeId: number;
  amount: number;
  description: string;
  date: string;
  denomination?: Denomination;
}

export interface CreatePettyCashTransactionRequest {
  safeId: number;
  type: "Deposit" | "Withdrawal";
  amount: number;
  description: string;
  date: string;
  denomination?: Denomination;
}

export interface ChangeBag {
  id: number;
  totalAmount: number;
  description: string;
  status: "InSafe" | "MovedToRegister";
  createdAt: string;
  movedAt: string | null;
  depositSequenceNumber: number | null;
  withdrawalSequenceNumber: number | null;
  denomination: Denomination | null;
}

export interface CashBag {
  id: number;
  totalAmount: number;
  description: string;
  status: "AtRegister" | "MovedToSafe";
  createdAt: string;
  movedAt: string | null;
  sequenceNumber: number | null;
  denomination: Denomination | null;
}

export interface VendorTransaction {
  id: number;
  sequenceNumber: number;
  changeBagId: number | null;
  cashBagId: number | null;
  prepBagId: number | null;
  type: "Deposit" | "Withdrawal" | "Adjustment";
  amount: number;
  description: string;
  createdAt: string;
  denomination: Denomination | null;
}

export interface PettyCashTransaction {
  id: number;
  sequenceNumber: number;
  type: "Deposit" | "Withdrawal" | "Adjustment";
  amount: number;
  description: string;
  createdAt: string;
  denomination: Denomination | null;
}

export interface DenominationCheck {
  id: number;
  sequenceNumber: number;
  changeBagId: number | null;
  cashBagId: number | null;
  prepBagId: number | null;
  count10000: number;
  count5000: number;
  count1000: number;
  count500: number;
  count100: number;
  count50: number;
  count10: number;
  count5: number;
  count1: number;
  checkedAmount: number;
  expectedAmount: number;
  difference: number;
  createdAt: string;
}

export interface PrepBag {
  id: number;
  totalAmount: number;
  status: "Preparing" | "HandedOver";
  createdAt: string;
  handedOverAt: string | null;
  cashBagIds: number[];
}

const API_BASE = "http://localhost:5141/api";

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: { "Content-Type": "application/json", ...options?.headers },
  });
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: res.statusText }));
    throw new Error(error.message || res.statusText);
  }
  return res.json();
}

export const api = {
  getSafes: () => fetchJson<Safe[]>(`${API_BASE}/safes`),

  createSafe: (req: { name: string; description: string }) =>
    fetchJson<Safe>(`${API_BASE}/safes`, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  getBags: (safeId: number) => fetchJson<ChangeBag[]>(`${API_BASE}/bags?safeId=${safeId}`),

  depositBag: (req: DepositRequest) =>
    fetchJson<ChangeBag>(`${API_BASE}/bags/deposit`, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  moveBagToRegister: (id: number) =>
    fetchJson<VendorTransaction>(`${API_BASE}/bags/${id}/move`, {
      method: "POST",
    }),

  getCashBags: (safeId: number) => fetchJson<CashBag[]>(`${API_BASE}/cashbags?safeId=${safeId}`),

  depositCashBag: (req: DepositRequest) =>
    fetchJson<CashBag>(`${API_BASE}/cashbags/deposit`, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  moveCashBagToSafe: (id: number) =>
    fetchJson<VendorTransaction>(`${API_BASE}/cashbags/${id}/move`, {
      method: "POST",
    }),

  getVendorTransactions: (safeId: number) =>
    fetchJson<VendorTransaction[]>(`${API_BASE}/vendor-transactions?safeId=${safeId}`),

  getPettyCashTransactions: (safeId: number) =>
    fetchJson<PettyCashTransaction[]>(`${API_BASE}/petty-cash-transactions?safeId=${safeId}`),

  createPettyCashTransaction: (req: CreatePettyCashTransactionRequest) =>
    fetchJson<PettyCashTransaction>(`${API_BASE}/petty-cash-transactions`, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  checkSafe: (safeId: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/safe/${safeId}`, {
      method: "POST",
      body: JSON.stringify(denomination),
    }),

  checkChangeBag: (bagId: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/changebag/${bagId}`, {
      method: "POST",
      body: JSON.stringify(denomination),
    }),

  checkCashBag: (bagId: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/cashbag/${bagId}`, {
      method: "POST",
      body: JSON.stringify(denomination),
    }),

  checkPrepBag: (bagId: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/prepbag/${bagId}`, {
      method: "POST",
      body: JSON.stringify(denomination),
    }),

  getVendorDenominationChecks: (safeId: number) =>
    fetchJson<DenominationCheck[]>(`${API_BASE}/denominationchecks/vendor?safeId=${safeId}`),

  getPettyCashDenominationChecks: (safeId: number) =>
    fetchJson<DenominationCheck[]>(`${API_BASE}/denominationchecks/safe?safeId=${safeId}`),

  updateVendorDenominationCheck: (id: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/vendor/${id}`, {
      method: "PUT",
      body: JSON.stringify(denomination),
    }),

  updatePettyCashDenominationCheck: (id: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/safe/${id}`, {
      method: "PUT",
      body: JSON.stringify(denomination),
    }),

  getPrepBags: (safeId: number) => fetchJson<PrepBag[]>(`${API_BASE}/prepbags?safeId=${safeId}`),

  createPrepBag: (safeId: number, cashBagIds: number[]) =>
    fetchJson<PrepBag>(`${API_BASE}/prepbags`, {
      method: "POST",
      body: JSON.stringify({ safeId, cashBagIds }),
    }),

  handOverPrepBag: (id: number) =>
    fetchJson<PrepBag>(`${API_BASE}/prepbags/${id}/handover`, {
      method: "POST",
    }),

  cancelPrepBag: (id: number) =>
    fetchJson<PrepBag>(`${API_BASE}/prepbags/${id}/cancel`, {
      method: "POST",
    }),
};

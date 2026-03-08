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

export interface DepositRequest {
  amount: number;
  description: string;
  date: string;
}

export interface CreateTransactionRequest {
  type: "Deposit" | "Withdrawal";
  amount: number;
  description: string;
  date: string;
}

export interface ChangeBag {
  id: number;
  totalAmount: number;
  description: string;
  status: "InSafe" | "MovedToRegister";
  createdAt: string;
  movedAt: string | null;
}

export interface CashBag {
  id: number;
  totalAmount: number;
  description: string;
  status: "AtRegister" | "MovedToSafe";
  createdAt: string;
  movedAt: string | null;
}

export interface Transaction {
  id: number;
  changeBagId: number | null;
  cashBagId: number | null;
  prepBagId: number | null;
  type: "Deposit" | "Withdrawal";
  amount: number;
  description: string;
  createdAt: string;
}

export interface DenominationCheck {
  id: number;
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
  getBags: () => fetchJson<ChangeBag[]>(`${API_BASE}/bags`),

  depositBag: (req: DepositRequest) =>
    fetchJson<ChangeBag>(`${API_BASE}/bags/deposit`, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  moveBagToRegister: (id: number) =>
    fetchJson<Transaction>(`${API_BASE}/bags/${id}/move`, {
      method: "POST",
    }),

  getCashBags: () => fetchJson<CashBag[]>(`${API_BASE}/cashbags`),

  depositCashBag: (req: DepositRequest) =>
    fetchJson<CashBag>(`${API_BASE}/cashbags/deposit`, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  moveCashBagToSafe: (id: number) =>
    fetchJson<Transaction>(`${API_BASE}/cashbags/${id}/move`, {
      method: "POST",
    }),

  getTransactions: () =>
    fetchJson<Transaction[]>(`${API_BASE}/transactions`),

  createTransaction: (req: CreateTransactionRequest) =>
    fetchJson<Transaction>(`${API_BASE}/transactions`, {
      method: "POST",
      body: JSON.stringify(req),
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

  getDenominationChecks: () =>
    fetchJson<DenominationCheck[]>(`${API_BASE}/denominationchecks`),

  updateDenominationCheck: (id: number, denomination: Denomination) =>
    fetchJson<DenominationCheck>(`${API_BASE}/denominationchecks/${id}`, {
      method: "PUT",
      body: JSON.stringify(denomination),
    }),

  getPrepBags: () => fetchJson<PrepBag[]>(`${API_BASE}/prepbags`),

  createPrepBag: (cashBagIds: number[]) =>
    fetchJson<PrepBag>(`${API_BASE}/prepbags`, {
      method: "POST",
      body: JSON.stringify({ cashBagIds }),
    }),

  handOverPrepBag: (id: number) =>
    fetchJson<PrepBag>(`${API_BASE}/prepbags/${id}/handover`, {
      method: "POST",
    }),
};

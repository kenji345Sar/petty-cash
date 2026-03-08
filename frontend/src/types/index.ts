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

export interface ChangeBag {
  id: number;
  denomination: Denomination;
  totalAmount: number;
  status: "InSafe" | "MovedToRegister";
  createdAt: string;
  movedAt: string | null;
}

export interface Transaction {
  id: number;
  changeBagId: number;
  type: "Deposit" | "Withdrawal";
  amount: number;
  description: string;
  createdAt: string;
}

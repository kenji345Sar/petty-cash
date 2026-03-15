import { useEffect, useState } from "react";
import { api } from "./api/client";
import type { Safe, ChangeBag, CashBag, Transaction, DenominationCheck, PrepBag } from "./api/client";
import { TransactionList } from "./components/TransactionList";
import "./App.css";

function App() {
  const [safes, setSafes] = useState<Safe[]>([]);
  const [selectedSafeId, setSelectedSafeId] = useState<number | null>(null);
  const [bags, setBags] = useState<ChangeBag[]>([]);
  const [cashBags, setCashBags] = useState<CashBag[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [denomChecks, setDenomChecks] = useState<DenominationCheck[]>([]);
  const [prepBags, setPrepBags] = useState<PrepBag[]>([]);

  const loadSafes = async () => {
    const data = await api.getSafes();
    setSafes(data);
    if (data.length > 0 && selectedSafeId === null) {
      setSelectedSafeId(data[0].id);
    }
  };

  const loadData = async (safeId: number) => {
    const [bagsData, cashBagsData, txData, checksData, prepBagsData] = await Promise.all([
      api.getBags(safeId),
      api.getCashBags(safeId),
      api.getTransactions(safeId),
      api.getDenominationChecks(safeId),
      api.getPrepBags(safeId),
    ]);
    setBags(bagsData);
    setCashBags(cashBagsData);
    setTransactions(txData);
    setDenomChecks(checksData);
    setPrepBags(prepBagsData);
  };

  useEffect(() => {
    loadSafes();
  }, []);

  useEffect(() => {
    if (selectedSafeId !== null) {
      loadData(selectedSafeId);
    }
  }, [selectedSafeId]);

  const selectedSafe = safes.find((s) => s.id === selectedSafeId);

  return (
    <div className="app">
      <header>
        <h1>小口現金管理システム</h1>
        {safes.length > 0 && (
          <div className="safe-selector">
            <select
              value={selectedSafeId ?? ""}
              onChange={(e) => setSelectedSafeId(Number(e.target.value))}
            >
              {safes.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            {selectedSafe && (
              <div className="safe-balance-group">
                <span className="safe-balance">
                  合計: {selectedSafe.currentBalance.toLocaleString()}円
                </span>
                <span className="safe-balance vendor">
                  業者: {selectedSafe.vendorBalance.toLocaleString()}円
                </span>
                <span className="safe-balance petty">
                  小口: {selectedSafe.pettyCashBalance.toLocaleString()}円
                </span>
              </div>
            )}
          </div>
        )}
      </header>
      <main>
        {selectedSafeId ? (
          <TransactionList
            safeId={selectedSafeId}
            transactions={transactions}
            denomChecks={denomChecks}
            bags={bags}
            cashBags={cashBags}
            prepBags={prepBags}
            onUpdate={() => { if (selectedSafeId) loadData(selectedSafeId); loadSafes(); }}
          />
        ) : (
          <p>金庫を選択してください</p>
        )}
      </main>
    </div>
  );
}

export default App;

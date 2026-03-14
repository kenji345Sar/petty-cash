import { useEffect, useState } from "react";
import { api } from "./api/client";
import type { ChangeBag, CashBag, Transaction, DenominationCheck, PrepBag } from "./api/client";
import { TransactionList } from "./components/TransactionList";
import "./App.css";

function App() {
  const [bags, setBags] = useState<ChangeBag[]>([]);
  const [cashBags, setCashBags] = useState<CashBag[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [denomChecks, setDenomChecks] = useState<DenominationCheck[]>([]);
  const [prepBags, setPrepBags] = useState<PrepBag[]>([]);

  const loadData = async () => {
    const [bagsData, cashBagsData, txData, checksData, prepBagsData] = await Promise.all([
      api.getBags(),
      api.getCashBags(),
      api.getTransactions(),
      api.getDenominationChecks(),
      api.getPrepBags(),
    ]);
    setBags(bagsData);
    setCashBags(cashBagsData);
    setTransactions(txData);
    setDenomChecks(checksData);
    setPrepBags(prepBagsData);
  };

  useEffect(() => {
    loadData();
  }, []);

  return (
    <div className="app">
      <header>
        <h1>小口現金管理システム</h1>
      </header>
      <main>
        <TransactionList
          transactions={transactions}
          denomChecks={denomChecks}
          bags={bags}
          cashBags={cashBags}
          prepBags={prepBags}
          onUpdate={loadData}
        />
      </main>
    </div>
  );
}

export default App;

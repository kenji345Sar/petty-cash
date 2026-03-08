import { useEffect, useState } from "react";
import { api } from "./api/client";
import type { ChangeBag, CashBag, Transaction, DenominationCheck, PrepBag } from "./api/client";
import { BagList } from "./components/BagList";
import { CashBagList } from "./components/CashBagList";
import { TransactionList } from "./components/TransactionList";
import "./App.css";

type Tab = "bags" | "transactions";

function App() {
  const [tab, setTab] = useState<Tab>("bags");
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
      <nav className="tabs">
        <button
          className={tab === "bags" ? "tab active" : "tab"}
          onClick={() => setTab("bags")}
        >
          バッグ管理
        </button>
        <button
          className={tab === "transactions" ? "tab active" : "tab"}
          onClick={() => setTab("transactions")}
        >
          出納帳
        </button>
      </nav>
      <main>
        {tab === "bags" ? (
          <>
            <BagList bags={bags} denomChecks={denomChecks} onUpdate={loadData} />
            <hr style={{ margin: "32px 0" }} />
            <CashBagList bags={cashBags} prepBags={prepBags} denomChecks={denomChecks} onUpdate={loadData} />
          </>
        ) : (
          <TransactionList transactions={transactions} denomChecks={denomChecks} bags={bags} cashBags={cashBags} prepBags={prepBags} onUpdate={loadData} />
        )}
      </main>
    </div>
  );
}

export default App;

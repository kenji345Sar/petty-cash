import { useEffect, useState } from "react";
import { api } from "./api/client";
import type { Safe } from "./api/client";
import { PettyCashTab } from "./components/PettyCashTab";
import { VendorTab } from "./components/VendorTab";
import "./App.css";

type Tab = "petty" | "vendor";

function App() {
  const [safes, setSafes] = useState<Safe[]>([]);
  const [selectedSafeId, setSelectedSafeId] = useState<number | null>(null);
  const [activeTab, setActiveTab] = useState<Tab>("petty");

  const loadSafes = async () => {
    const data = await api.getSafes();
    setSafes(data);
    if (data.length > 0 && selectedSafeId === null) {
      setSelectedSafeId(data[0].id);
    }
  };

  const handleSafeUpdate = (updatedSafe: Safe) => {
    setSafes(prev => prev.map(s => s.id === updatedSafe.id ? updatedSafe : s));
  };

  useEffect(() => {
    loadSafes();
  }, []);

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
        {selectedSafeId && (
          <div style={{ display: "flex", gap: 0, marginTop: 12 }}>
            <button
              onClick={() => setActiveTab("petty")}
              style={{
                padding: "10px 24px",
                border: "1px solid #ccc",
                borderBottom: activeTab === "petty" ? "2px solid #2563eb" : "1px solid #ccc",
                background: activeTab === "petty" ? "#fff" : "#f5f5f5",
                color: activeTab === "petty" ? "#2563eb" : "#666",
                fontWeight: activeTab === "petty" ? "bold" : "normal",
                cursor: "pointer",
                borderRadius: "8px 8px 0 0",
              }}
            >
              小口
            </button>
            <button
              onClick={() => setActiveTab("vendor")}
              style={{
                padding: "10px 24px",
                border: "1px solid #ccc",
                borderBottom: activeTab === "vendor" ? "2px solid #2563eb" : "1px solid #ccc",
                background: activeTab === "vendor" ? "#fff" : "#f5f5f5",
                color: activeTab === "vendor" ? "#2563eb" : "#666",
                fontWeight: activeTab === "vendor" ? "bold" : "normal",
                cursor: "pointer",
                borderRadius: "8px 8px 0 0",
              }}
            >
              業者
            </button>
          </div>
        )}
      </header>
      <main>
        {selectedSafeId && selectedSafe ? (
          activeTab === "petty" ? (
            <PettyCashTab
              safeId={selectedSafeId}
              onSafeUpdate={handleSafeUpdate}
            />
          ) : (
            <VendorTab
              safeId={selectedSafeId}
              onSafeUpdate={handleSafeUpdate}
            />
          )
        ) : (
          <p>金庫を選択してください</p>
        )}
      </main>
    </div>
  );
}

export default App;

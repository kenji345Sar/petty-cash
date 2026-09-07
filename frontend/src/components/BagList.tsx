import { Fragment, useState } from "react";
import { api } from "../api/client";
import type { ChangeBag, DenominationCheck, Denomination } from "../api/client";
import { DenominationCheckForm } from "./DenominationCheckForm";
import { DenominationInput } from "./DenominationInput";

interface Props {
  safeId: number;
  bags: ChangeBag[];
  denomChecks: DenominationCheck[];
  onUpdate: () => void;
}

function todayStr() {
  return new Date().toISOString().slice(0, 10);
}

export function BagList({ safeId, bags, denomChecks, onUpdate }: Props) {
  const checksByBag = (bagId: number) =>
    denomChecks
      .filter((c) => c.changeBagId === bagId)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  const [editCheck, setEditCheck] = useState<DenominationCheck | null>(null);
  const editBag = editCheck?.changeBagId ? bags.find((b) => b.id === editCheck.changeBagId) : null;

  const [showForm, setShowForm] = useState(false);
  const [amount, setAmount] = useState(0);
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(todayStr());
  const [loading, setLoading] = useState(false);
  const [checkBagId, setCheckBagId] = useState<number | null>(null);
  const checkBag = bags.find((b) => b.id === checkBagId);
  const [showDenomInput, setShowDenomInput] = useState(false);
  const [selectedDenom, setSelectedDenom] = useState<Denomination | null>(null);

  const handleDeposit = async () => {
    setLoading(true);
    try {
      await api.depositBag({
        safeId, amount, description, date,
        ...(selectedDenom ? { denomination: selectedDenom } : {}),
      });
      setAmount(0);
      setDescription("");
      setDate(todayStr());
      setSelectedDenom(null);
      setShowForm(false);
      onUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };

  const handleMove = async (id: number) => {
    if (!confirm("このバッグをレジへ移動しますか？")) return;
    try {
      await api.moveBagToRegister(id);
      onUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    }
  };

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <h3 style={{ margin: 0 }}>両替金バッグ一覧</h3>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? "閉じる" : "両替金追加"}
        </button>
      </div>

      {showForm && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3>入金処理</h3>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <label>金額</label>
              <span style={{ fontWeight: "bold", fontSize: "1.1rem" }}>{amount > 0 ? `${amount.toLocaleString()}円` : "未入力"}</span>
              <button className="btn-action" onClick={() => setShowDenomInput(true)} style={{ padding: "6px 14px", fontSize: "0.85rem" }}>金種表で入力</button>
            </div>
            <div>
              <label>備考</label>
              <input type="text" value={description} onChange={(e) => setDescription(e.target.value)} style={{ marginLeft: 8, width: 300 }} placeholder="例: 両替金準備金" />
            </div>
            <div>
              <label>日付</label>
              <input type="date" value={date} onChange={(e) => setDate(e.target.value)} style={{ marginLeft: 8 }} />
            </div>
          </div>
          <button className="btn-primary" onClick={handleDeposit} disabled={loading || amount <= 0} style={{ marginTop: 12 }}>
            {loading ? "処理中..." : "入金する"}
          </button>
        </div>
      )}

      <table>
        <thead>
          <tr>
            <th>番号</th>
            <th>合計金額</th>
            <th>備考</th>
            <th>状態</th>
            <th>入金日時</th>
            <th>移動日時</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          {bags.length === 0 ? (
            <tr>
              <td colSpan={7} style={{ textAlign: "center" }}>バッグがありません</td>
            </tr>
          ) : (
            bags.map((bag) => {
              const checks = checksByBag(bag.id);
              return (
                <Fragment key={bag.id}>
                  <tr>
                    <td>CA-{String(bag.id).padStart(3, "0")}</td>
                    <td>{bag.totalAmount.toLocaleString()}円</td>
                    <td>{bag.description}</td>
                    <td>
                      <span className={`status ${bag.status === "InSafe" ? "status-safe" : "status-moved"}`}>
                        {bag.status === "InSafe" ? "金庫内" : "レジへ移動済"}
                      </span>
                    </td>
                    <td>{new Date(bag.createdAt).toLocaleString("ja-JP")}</td>
                    <td>{bag.movedAt ? new Date(bag.movedAt).toLocaleString("ja-JP") : "-"}</td>
                    <td style={{ display: "flex", gap: 4 }}>
                      {bag.status === "InSafe" && (
                        <button className="btn-move" onClick={() => handleMove(bag.id)}>
                          移動
                        </button>
                      )}
                      <button className="btn-check" onClick={() => setCheckBagId(bag.id)}>
                        有高
                      </button>
                    </td>
                  </tr>
                  {checks.map((check) => (
                    <tr key={`chk-${check.id}`} style={{ background: "#f0f7ff" }}>
                      <td></td>
                      <td colSpan={4} style={{ fontSize: "0.9rem" }}>
                        <a href="#" onClick={(e) => { e.preventDefault(); setEditCheck(check); }} style={{ color: "#3b82f6", textDecoration: "underline", cursor: "pointer" }}>
                          有高
                        </a>
                        {" "}有高: {check.checkedAmount.toLocaleString()}円 / 帳簿: {check.expectedAmount.toLocaleString()}円
                        <span style={{ color: check.difference === 0 ? "green" : "red", marginLeft: 8 }}>
                          （差額: {check.difference >= 0 ? "+" : ""}{check.difference.toLocaleString()}円）
                        </span>
                        <span style={{ fontSize: "0.8rem", color: "#999", marginLeft: 8 }}>
                          {[
                            { label: "1万", count: check.count10000 },
                            { label: "5千", count: check.count5000 },
                            { label: "千", count: check.count1000 },
                            { label: "500", count: check.count500 },
                            { label: "100", count: check.count100 },
                            { label: "50", count: check.count50 },
                            { label: "10", count: check.count10 },
                            { label: "5", count: check.count5 },
                            { label: "1", count: check.count1 },
                          ].filter((d) => d.count > 0).map((d) => `${d.label}×${d.count}`).join(" ")}
                        </span>
                      </td>
                      <td colSpan={2} style={{ fontSize: "0.85rem", color: "#888" }}>
                        {new Date(check.createdAt).toLocaleString("ja-JP")}
                      </td>
                    </tr>
                  ))}
                </Fragment>
              );
            })
          )}
        </tbody>
      </table>

      {showDenomInput && (
        <DenominationInput
          initialDenom={selectedDenom ?? undefined}
          onComplete={(denom, total) => {
            setSelectedDenom(denom);
            setAmount(total);
            setShowDenomInput(false);
          }}
          onCancel={() => setShowDenomInput(false)}
        />
      )}

      {checkBag && (() => {
        const checks = checksByBag(checkBag.id);
        const lastCheck = checks.length > 0 ? checks[0] : null;
        const initDenom = lastCheck
          ? { count10000: lastCheck.count10000, count5000: lastCheck.count5000, count1000: lastCheck.count1000, count500: lastCheck.count500, count100: lastCheck.count100, count50: lastCheck.count50, count10: lastCheck.count10, count5: lastCheck.count5, count1: lastCheck.count1 }
          : checkBag.denomination ?? undefined;
        return (
          <DenominationCheckForm
            bagId={checkBag.id}
            bagType="change"
            expectedAmount={checkBag.totalAmount}
            onSubmit={(id, denom) => api.checkChangeBag(id, denom)}
            onClose={() => setCheckBagId(null)}
            onDone={onUpdate}
            initialDenom={initDenom}
          />
        );
      })()}

      {editBag && editCheck && (
        <DenominationCheckForm
          bagId={editBag.id}
          bagType="change"
          expectedAmount={editBag.totalAmount}
          onSubmit={(id, denom) => api.checkChangeBag(id, denom)}
          onClose={() => setEditCheck(null)}
          onDone={onUpdate}
          editCheck={editCheck}
          onUpdate={(id, denom) => api.updateVendorDenominationCheck(id, denom)}
        />
      )}
    </div>
  );
}

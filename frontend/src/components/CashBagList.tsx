import { Fragment, useState } from "react";
import { api } from "../api/client";
import type { CashBag, PrepBag, DenominationCheck } from "../api/client";
import { DenominationCheckForm } from "./DenominationCheckForm";

interface Props {
  safeId: number;
  bags: CashBag[];
  prepBags: PrepBag[];
  denomChecks: DenominationCheck[];
  onUpdate: () => void;
}

function todayStr() {
  return new Date().toISOString().slice(0, 10);
}

export function CashBagList({ safeId, bags, prepBags, denomChecks, onUpdate }: Props) {
  const checksByBag = (bagId: number) =>
    denomChecks
      .filter((c) => c.cashBagId === bagId)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  const checksByPrepBag = (bagId: number) =>
    denomChecks
      .filter((c) => c.prepBagId === bagId)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  const [editCheck, setEditCheck] = useState<DenominationCheck | null>(null);
  const editBag = editCheck?.cashBagId ? bags.find((b) => b.id === editCheck.cashBagId) : null;
  const editPrepBag = editCheck?.prepBagId ? prepBags.find((p) => p.id === editCheck.prepBagId) : null;
  const [checkPrepBagId, setCheckPrepBagId] = useState<number | null>(null);
  const checkPrepBag = prepBags.find((p) => p.id === checkPrepBagId);

  const [showForm, setShowForm] = useState(false);
  const [amount, setAmount] = useState(0);
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(todayStr());
  const [loading, setLoading] = useState(false);
  const [checkBagId, setCheckBagId] = useState<number | null>(null);
  const checkBag = bags.find((b) => b.id === checkBagId);
  const [selectMode, setSelectMode] = useState(false);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [creating, setCreating] = useState(false);
  const [prepSelectMode, setPrepSelectMode] = useState(false);
  const [prepSelected, setPrepSelected] = useState<Set<number>>(new Set());
  const [handingOver, setHandingOver] = useState(false);

  const assignedIds = new Set(prepBags.flatMap((p) => p.cashBagIds));
  const visibleBags = bags.filter((b) => !assignedIds.has(b.id));
  const visiblePrepBags = prepBags.filter((p) => p.status === "Preparing");

  const toggleSelect = (id: number) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleDeposit = async () => {
    setLoading(true);
    try {
      await api.depositCashBag({ safeId, amount, description, date });
      setAmount(0);
      setDescription("");
      setDate(todayStr());
      setShowForm(false);
      onUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };

  const handleCreatePrepBag = async () => {
    if (selected.size === 0) return;
    setCreating(true);
    try {
      await api.createPrepBag(safeId, [...selected]);
      setSelected(new Set());
      setSelectMode(false);
      onUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setCreating(false);
    }
  };

  const handleCancelSelect = () => {
    setSelectMode(false);
    setSelected(new Set());
  };

  const togglePrepSelect = (id: number) => {
    setPrepSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleCancelPrepSelect = () => {
    setPrepSelectMode(false);
    setPrepSelected(new Set());
  };

  const handleHandOverSelected = async () => {
    if (prepSelected.size === 0) return;
    setHandingOver(true);
    try {
      for (const id of prepSelected) {
        await api.handOverPrepBag(id);
      }
      setPrepSelected(new Set());
      setPrepSelectMode(false);
      onUpdate();
    } catch (e) {
      alert(e instanceof Error ? e.message : "エラーが発生しました");
    } finally {
      setHandingOver(false);
    }
  };

  const selectedTotal = bags
    .filter((b) => selected.has(b.id))
    .reduce((sum, b) => sum + b.totalAmount, 0);

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <h2>キャッシュバッグ一覧</h2>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? "閉じる" : "キャッシュバッグ追加"}
        </button>
      </div>

      {showForm && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3>入金処理</h3>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <label>金額（円）</label>
              <input
                type="number"
                min="1"
                value={amount || ""}
                onChange={(e) => setAmount(Math.max(0, parseInt(e.target.value) || 0))}
                style={{ marginLeft: 8, width: 160 }}
              />
            </div>
            <div>
              <label>備考</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                style={{ marginLeft: 8, width: 300 }}
                placeholder="例: 売上入金"
              />
            </div>
            <div>
              <label>日付</label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                style={{ marginLeft: 8 }}
              />
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
            <th>ID</th>
            <th>合計金額</th>
            <th>備考</th>
            <th>入金日時</th>
            <th>操作</th>
            <th>
              {!selectMode ? (
                <button className="btn-prep" style={{ fontSize: "0.85rem", padding: "6px 12px" }} onClick={() => setSelectMode(true)}>
                  CashBag→準備Bag
                </button>
              ) : (
                <button onClick={handleCancelSelect} style={{ fontSize: "0.85rem", padding: "6px 12px", cursor: "pointer", borderRadius: 4, border: "1px solid #ccc" }}>キャンセル</button>
              )}
            </th>
          </tr>
        </thead>
        <tbody>
          {visibleBags.length === 0 ? (
            <tr>
              <td colSpan={6} style={{ textAlign: "center" }}>バッグがありません</td>
            </tr>
          ) : (
            visibleBags.map((bag) => {
              const checks = checksByBag(bag.id);
              return (
                <Fragment key={bag.id}>
                  <tr style={selected.has(bag.id) ? { background: "#e8f4fd" } : undefined}>
                    <td>{bag.id}</td>
                    <td>{bag.totalAmount.toLocaleString()}円</td>
                    <td>{bag.description}</td>
                    <td>{new Date(bag.createdAt).toLocaleString("ja-JP")}</td>
                    <td>
                      <button className="btn-check" onClick={() => setCheckBagId(bag.id)}>
                        有高
                      </button>
                    </td>
                    <td style={{ textAlign: "center" }}>
                      {selectMode && (
                        <input
                          type="checkbox"
                          checked={selected.has(bag.id)}
                          onChange={() => toggleSelect(bag.id)}
                        />
                      )}
                    </td>
                  </tr>
                  {checks.map((check) => (
                    <tr key={`chk-${check.id}`} style={{ background: "#f0f7ff" }}>
                      <td></td>
                      <td colSpan={3} style={{ fontSize: "0.9rem" }}>
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

      {selectMode && selected.size > 0 && (
        <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 12 }}>
          <button className="btn-prep" onClick={handleCreatePrepBag} disabled={creating}>
            {creating ? "作成中..." : `準備バッグ作成（${selected.size}件 / ${selectedTotal.toLocaleString()}円）`}
          </button>
        </div>
      )}

      {visiblePrepBags.length > 0 && (
        <div style={{ marginTop: 32 }}>
          <h3>準備バッグ一覧</h3>
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>合計金額</th>
                <th>含むバッグ</th>
                <th>操作</th>
                <th>作成日時</th>
                <th>
                  {!prepSelectMode ? (
                    <button className="btn-move" style={{ fontSize: "0.85rem", padding: "6px 12px" }} onClick={() => setPrepSelectMode(true)}>
                      引渡
                    </button>
                  ) : (
                    <button onClick={handleCancelPrepSelect} style={{ fontSize: "0.85rem", padding: "6px 12px", cursor: "pointer", borderRadius: 4, border: "1px solid #ccc" }}>キャンセル</button>
                  )}
                </th>
              </tr>
            </thead>
            <tbody>
              {visiblePrepBags.map((pb) => {
                const prepChecks = checksByPrepBag(pb.id);
                return (
                  <Fragment key={pb.id}>
                    <tr style={prepSelected.has(pb.id) ? { background: "#fef3c7" } : undefined}>
                      <td>{pb.id}</td>
                      <td>{pb.totalAmount.toLocaleString()}円</td>
                      <td>{pb.cashBagIds.map((id) => `#${id}`).join(", ")}</td>
                      <td>
                        <button className="btn-check" onClick={() => setCheckPrepBagId(pb.id)}>
                          有高
                        </button>
                      </td>
                      <td>{new Date(pb.createdAt).toLocaleString("ja-JP")}</td>
                      <td style={{ textAlign: "center" }}>
                        {prepSelectMode && (
                          <input
                            type="checkbox"
                            checked={prepSelected.has(pb.id)}
                            onChange={() => togglePrepSelect(pb.id)}
                          />
                        )}
                      </td>
                    </tr>
                    {prepChecks.map((check) => (
                      <tr key={`pchk-${check.id}`} style={{ background: "#f0f7ff" }}>
                        <td></td>
                        <td colSpan={3} style={{ fontSize: "0.9rem" }}>
                          <a href="#" onClick={(e) => { e.preventDefault(); setEditCheck(check); }} style={{ color: "#3b82f6", textDecoration: "underline", cursor: "pointer" }}>
                            有高
                          </a>
                          {" "}有高: {check.checkedAmount.toLocaleString()}円 / 帳簿: {check.expectedAmount.toLocaleString()}円
                          <span style={{ color: check.difference === 0 ? "green" : "red", marginLeft: 8 }}>
                            （差額: {check.difference >= 0 ? "+" : ""}{check.difference.toLocaleString()}円）
                          </span>
                        </td>
                        <td colSpan={2} style={{ fontSize: "0.85rem", color: "#888" }}>
                          {new Date(check.createdAt).toLocaleString("ja-JP")}
                        </td>
                      </tr>
                    ))}
                  </Fragment>
                );
              })}
            </tbody>
          </table>

          {prepSelectMode && prepSelected.size > 0 && (
            <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 12 }}>
              <button className="btn-move" onClick={handleHandOverSelected} disabled={handingOver}>
                {handingOver ? "処理中..." : `引渡実行（${prepSelected.size}件）`}
              </button>
            </div>
          )}
        </div>
      )}

      {checkBag && (
        <DenominationCheckForm
          bagId={checkBag.id}
          bagType="cash"
          expectedAmount={checkBag.totalAmount}
          onSubmit={(id, denom) => api.checkCashBag(id, denom)}
          onClose={() => setCheckBagId(null)}
          onDone={onUpdate}
        />
      )}

      {editBag && editCheck && (
        <DenominationCheckForm
          bagId={editBag.id}
          bagType="cash"
          expectedAmount={editBag.totalAmount}
          onSubmit={(id, denom) => api.checkCashBag(id, denom)}
          onClose={() => setEditCheck(null)}
          onDone={onUpdate}
          editCheck={editCheck}
          onUpdate={(id, denom) => api.updateDenominationCheck(id, denom)}
        />
      )}

      {checkPrepBag && (
        <DenominationCheckForm
          bagId={checkPrepBag.id}
          bagType="prep"
          expectedAmount={checkPrepBag.totalAmount}
          onSubmit={(id, denom) => api.checkPrepBag(id, denom)}
          onClose={() => setCheckPrepBagId(null)}
          onDone={onUpdate}
        />
      )}

      {editPrepBag && editCheck && (
        <DenominationCheckForm
          bagId={editPrepBag.id}
          bagType="prep"
          expectedAmount={editPrepBag.totalAmount}
          onSubmit={(id, denom) => api.checkPrepBag(id, denom)}
          onClose={() => setEditCheck(null)}
          onDone={onUpdate}
          editCheck={editCheck}
          onUpdate={(id, denom) => api.updateDenominationCheck(id, denom)}
        />
      )}
    </div>
  );
}

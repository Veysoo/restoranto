import { FormEvent, useCallback, useEffect, useState } from 'react';
import { api, formatCurrency } from '../api';
import type { MenuCategory, MenuItem, TableSettings } from '../types';

const emptyTable = (nextNum: number): TableSettings => ({
  tableId: '00000000-0000-0000-0000-000000000000',
  tableNumber: nextNum,
  name: '',
  capacity: 4,
  section: 'İç Salon',
  isActive: true,
  displayOrder: nextNum,
});

const emptyItem = (categoryId: string): MenuItem => ({
  menuItemId: '',
  categoryId,
  name: '',
  description: '',
  price: 0,
  taxRate: 10,
  isAvailable: true,
});

export default function SettingsPage() {
  const [tab, setTab] = useState<'tables' | 'menu'>('menu');
  const [tables, setTables] = useState<TableSettings[]>([]);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [editTable, setEditTable] = useState<TableSettings | null>(null);
  const [editItem, setEditItem] = useState<MenuItem | null>(null);
  const [newCategoryName, setNewCategoryName] = useState('');
  const [newCategoryIcon, setNewCategoryIcon] = useState('🍽️');
  const [selectedCat, setSelectedCat] = useState('');
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');
  const [savingItem, setSavingItem] = useState(false);

  const load = useCallback(async () => {
    const [t, m] = await Promise.all([api.getSettingsTables(), api.getMenu()]);
    setTables(t);
    setCategories(m);
    setSelectedCat((prev) => prev || (m.length ? m[0].categoryId : ''));
  }, []);

  useEffect(() => { load().catch((e) => setError(e.message)); }, [load]);

  const flash = (text: string) => { setMsg(text); setTimeout(() => setMsg(''), 3000); };

  const saveTable = async (e: FormEvent) => {
    e.preventDefault();
    if (!editTable) return;
    try {
      await api.saveTable(editTable);
      setEditTable(null);
      await load();
      flash('✓ Masa kaydedildi.');
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
  };

  const deleteTable = async (id: string) => {
    if (!confirm('Bu masayı silmek istiyor musunuz?')) return;
    try {
      await api.deleteTable(id);
      await load();
      flash('✓ Masa silindi.');
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
  };

  const saveItem = async (e: FormEvent) => {
    e.preventDefault();
    if (!editItem) return;
    setSavingItem(true);
    try {
      if (editItem.menuItemId) {
        await api.updateMenuItem(editItem.menuItemId, editItem);
      } else {
        await api.createMenuItem(editItem);
      }
      setEditItem(null);
      await load();
      flash('✓ Ürün kaydedildi.');
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
    finally { setSavingItem(false); }
  };

  const deleteItem = async (id: string) => {
    if (!confirm('Bu ürünü silmek istiyor musunuz?')) return;
    try {
      await api.deleteMenuItem(id);
      await load();
      flash('✓ Ürün silindi.');
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
  };

  const toggleItem = async (id: string) => {
    try {
      await api.toggleMenuItem(id);
      await load();
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
  };

  const addCategory = async () => {
    if (!newCategoryName.trim()) return;
    try {
      const cat = await api.createCategory(newCategoryName.trim(), newCategoryIcon);
      setNewCategoryName('');
      setNewCategoryIcon('🍽️');
      await load();
      setSelectedCat(cat.categoryId);
      flash('✓ Kategori eklendi.');
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
  };

  const deleteCategory = async (id: string) => {
    if (!confirm('Bu kategoriyi ve tüm ürünlerini silmek istiyor musunuz?')) return;
    try {
      await api.deleteCategory(id);
      setSelectedCat('');
      await load();
      flash('✓ Kategori silindi.');
    } catch (err) { setError(err instanceof Error ? err.message : 'Hata'); }
  };

  const catItems = categories.find((c) => c.categoryId === selectedCat)?.items ?? [];
  const selectedCatObj = categories.find((c) => c.categoryId === selectedCat);

  return (
    <div>
      <div className="page-header">
        <h1 className="page-title">Yönetim</h1>
        <p className="page-subtitle">Masa ve menü ürünlerini yönetin</p>
      </div>

      {error && <div className="error-msg" onClick={() => setError('')}>{error} <small>(tıklayarak kapat)</small></div>}
      {msg && <div className="success-msg">{msg}</div>}

      <div style={{ marginBottom: 24 }}>
        <span className={`pill${tab === 'menu' ? ' active' : ''}`} onClick={() => setTab('menu')}>🍽️ Menü / Ürünler</span>
        <span className={`pill${tab === 'tables' ? ' active' : ''}`} onClick={() => setTab('tables')}>🏠 Masalar</span>
      </div>

      {/* ── TABLES TAB ── */}
      {tab === 'tables' && (
        <div className="card">
          <div className="section-header">
            <div>
              <h2>Masalar</h2>
              <p className="caption">{tables.length} aktif masa</p>
            </div>
            <button className="btn-primary" onClick={() => {
            const maxNum = tables.length > 0 ? Math.max(...tables.map((t) => t.tableNumber)) : 0;
            setEditTable(emptyTable(maxNum + 1));
          }}>+ Yeni Masa</button>
          </div>
          {tables.length === 0 ? (
            <p style={{ color: 'var(--text-secondary)', padding: '16px 0' }}>Henüz masa eklenmedi.</p>
          ) : (
            tables.map((t) => (
              <div key={t.tableId} className="list-row">
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600 }}>#{t.tableNumber} {t.name}</div>
                  <div className="caption">{t.section} · {t.capacity} kişilik</div>
                </div>
                <div className="row-actions">
                  <button className="btn-secondary" onClick={() => setEditTable({ ...t })}>Düzenle</button>
                  <button className="btn-ghost btn-danger" onClick={() => deleteTable(t.tableId)}>Sil</button>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {/* ── MENU TAB ── */}
      {tab === 'menu' && (
        <>
          {/* Add category */}
          <div className="card" style={{ marginBottom: 16 }}>
            <div className="section-header">
              <h3 style={{ fontSize: 15 }}>Yeni Kategori Ekle</h3>
            </div>
            <div className="inline-form">
              <input
                value={newCategoryIcon}
                onChange={(e) => setNewCategoryIcon(e.target.value)}
                style={{ maxWidth: 70, textAlign: 'center', fontSize: 20 }}
                placeholder="🍽️"
              />
              <input
                placeholder="Kategori adı (ör: Kahvaltılar)"
                value={newCategoryName}
                onChange={(e) => setNewCategoryName(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && addCategory()}
              />
              <button className="btn-primary" onClick={addCategory} disabled={!newCategoryName.trim()}>
                + Ekle
              </button>
            </div>
          </div>

          {/* Category selector */}
          <div style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 0, alignItems: 'center' }}>
            {categories.map((c) => (
              <span
                key={c.categoryId}
                className={`pill${selectedCat === c.categoryId ? ' active' : ''}`}
                onClick={() => setSelectedCat(c.categoryId)}
              >
                {c.icon} {c.name}
                <span style={{ marginLeft: 4, opacity: 0.5, fontSize: 10 }}>({c.items.length})</span>
              </span>
            ))}
          </div>

          {/* Items list */}
          {selectedCat && (
            <div className="card">
              <div className="section-header">
                <div>
                  <h2>{selectedCatObj?.icon} {selectedCatObj?.name}</h2>
                  <p className="caption">{catItems.length} ürün</p>
                </div>
                <div style={{ display: 'flex', gap: 8 }}>
                  <button className="btn-primary" onClick={() => setEditItem(emptyItem(selectedCat))}>
                    + Yeni Ürün
                  </button>
                  <button className="btn-ghost btn-danger" onClick={() => deleteCategory(selectedCat)}>
                    Kategoriyi Sil
                  </button>
                </div>
              </div>

              {catItems.length === 0 ? (
                <p style={{ color: 'var(--text-secondary)', padding: '16px 0' }}>
                  Bu kategoride ürün yok. "Yeni Ürün" butonuyla ekleyin.
                </p>
              ) : (
                catItems.map((item) => (
                  <div key={item.menuItemId} className="list-row" style={{ opacity: item.isAvailable ? 1 : 0.55 }}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8 }}>
                        {item.name}
                        {!item.isAvailable && (
                          <span style={{ fontSize: 10, background: 'rgba(100,116,139,0.2)', color: '#94a3b8', padding: '2px 6px', borderRadius: 4, fontWeight: 700 }}>
                            SATIŞTA DEĞİL
                          </span>
                        )}
                      </div>
                      <div className="caption mono">{formatCurrency(item.price)}</div>
                    </div>
                    <div className="row-actions">
                      <button
                        className="btn-secondary"
                        onClick={() => toggleItem(item.menuItemId)}
                        title={item.isAvailable ? 'Satıştan kaldır' : 'Satışa aç'}
                      >
                        {item.isAvailable ? '⏸ Kaldır' : '▶ Aktif Et'}
                      </button>
                      <button className="btn-secondary" onClick={() => setEditItem({ ...item })}>
                        Düzenle
                      </button>
                      <button className="btn-ghost btn-danger" onClick={() => deleteItem(item.menuItemId)}>
                        Sil
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </>
      )}

      {/* ── TABLE MODAL ── */}
      {editTable && (
        <div className="modal" onClick={() => setEditTable(null)}>
          <form className="card modal-box" onClick={(e) => e.stopPropagation()} onSubmit={saveTable}>
            <div className="modal-title">
              {editTable.tableId === '00000000-0000-0000-0000-000000000000' ? '+ Yeni Masa' : 'Masa Düzenle'}
            </div>
            <div className="form-group">
              <label>Masa No</label>
              <input type="number" required min={1} value={editTable.tableNumber}
                onChange={(e) => setEditTable({ ...editTable, tableNumber: +e.target.value })} />
            </div>
            <div className="form-group">
              <label>Masa Adı</label>
              <input required placeholder="ör: Bahçe 3" value={editTable.name}
                onChange={(e) => setEditTable({ ...editTable, name: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Bölüm</label>
              <input required placeholder="ör: İç Salon" value={editTable.section}
                onChange={(e) => setEditTable({ ...editTable, section: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Kapasite (kişi)</label>
              <input type="number" min={1} max={50} required value={editTable.capacity}
                onChange={(e) => setEditTable({ ...editTable, capacity: +e.target.value })} />
            </div>
            <div className="modal-actions">
              <button type="button" className="btn-ghost" onClick={() => setEditTable(null)}>İptal</button>
              <button type="submit" className="btn-primary">Kaydet</button>
            </div>
          </form>
        </div>
      )}

      {/* ── ITEM MODAL ── */}
      {editItem && (
        <div className="modal" onClick={() => setEditItem(null)}>
          <form className="card modal-box" onClick={(e) => e.stopPropagation()} onSubmit={saveItem}>
            <div className="modal-title">
              {editItem.menuItemId ? 'Ürün Düzenle' : '+ Yeni Ürün'}
            </div>
            <div className="form-group">
              <label>Ürün Adı</label>
              <input required placeholder="ör: Türk Kahvesi" value={editItem.name}
                onChange={(e) => setEditItem({ ...editItem, name: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Açıklama (opsiyonel)</label>
              <input placeholder="ör: Çift (double)" value={editItem.description ?? ''}
                onChange={(e) => setEditItem({ ...editItem, description: e.target.value })} />
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div className="form-group">
                <label>Fiyat (₺)</label>
                <input type="number" step="0.50" min={0} required value={editItem.price || ''}
                  onChange={(e) => setEditItem({ ...editItem, price: +e.target.value })} />
              </div>
              <div className="form-group">
                <label>KDV %</label>
                <input type="number" min={0} max={100} value={editItem.taxRate}
                  onChange={(e) => setEditItem({ ...editItem, taxRate: +e.target.value })} />
              </div>
            </div>
            <div className="modal-actions">
              <button type="button" className="btn-ghost" onClick={() => setEditItem(null)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={savingItem}>
                {savingItem ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

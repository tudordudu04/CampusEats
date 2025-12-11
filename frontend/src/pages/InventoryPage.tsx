import { useState, useEffect } from 'react'
import { InventoryApi } from '../services/api'
import { Package, Plus, AlertTriangle, Search, RefreshCw, Edit2, CalendarDays } from 'lucide-react'
import type { InventoryItemDto } from '../types'


export default function InventoryPage() {
    const [ingredients, setIngredients] = useState<InventoryItemDto[]>([])
    const [loading, setLoading] = useState(true)
    const [searchTerm, setSearchTerm] = useState('')
    const [showAddForm, setShowAddForm] = useState(false)
    
    // Adjust Stock State
    const [adjustingItem, setAdjustingItem] = useState<InventoryItemDto | null>(null)
    const [adjustQty, setAdjustQty] = useState('')
    const [adjustReason, setAdjustReason] = useState('')
    // StockTransactionType: 0=Restock, 1=Usage, 2=Waste, 3=Adjustment
    const [adjustType, setAdjustType] = useState<number>(0)

    // Form state
    const [newName, setNewName] = useState('')
    const [newUnit, setNewUnit] = useState('kg')
    const [newThreshold, setNewThreshold] = useState('5')

    const fetchInventory = async () => {
        setLoading(true)
        try {
            const data = await InventoryApi.getAll()
            setIngredients(data)
        } catch (error) {
            console.error("Failed to fetch inventory", error)
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        fetchInventory()
    }, [])

    const handleAddIngredient = async (e: React.FormEvent) => {
        e.preventDefault()
        try {
            await InventoryApi.create(
                newName,
                newUnit,
                parseFloat(newThreshold)
            )
            setShowAddForm(false)
            setNewName('')
            setNewUnit('kg')
            fetchInventory()
        } catch (error) {
            alert('Error creating ingredient')
        }
    }

    const handleAdjustStock = async (e: React.FormEvent) => {
        e.preventDefault()
        if (!adjustingItem) return
        console.log("Adjusting stock for", adjustingItem.id, " " , adjustingItem.name, "by", adjustQty, "as type", adjustType)

        try {
            await InventoryApi.adjustStock(
                adjustingItem.id,
                parseFloat(adjustQty),
                adjustType, // StockTransactionType
                adjustReason
            )
            setAdjustingItem(null)
            setAdjustQty('')
            setAdjustReason('')
            setAdjustType(0)
            fetchInventory()
        } catch (error) {
            console.error("Failed to adjust stock", error)
            alert("Eroare la actualizarea stocului")
        }
    }

    const filteredIngredients = ingredients.filter(item =>
        item.name.toLowerCase().includes(searchTerm.toLowerCase())
    )

    return (
        <div className="max-w-6xl mx-auto p-6">
            <div className="flex justify-between items-center mb-8">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-3">
                        <Package className="text-brand-600" />
                        Inventar Bucătărie
                    </h1>
                    <p className="text-gray-500 mt-1">Gestionează stocurile de ingrediente</p>
                </div>
                <button 
                    onClick={() => setShowAddForm(!showAddForm)}
                    className="bg-brand-600 text-white px-4 py-2 rounded-lg hover:bg-brand-700 flex items-center gap-2 transition-colors shadow-md"
                >
                    <Plus size={20} />
                    Adaugă Ingredient
                </button>
            </div>

            {/* Add Ingredient Form */}
            {showAddForm && (
                <div className="bg-white p-6 rounded-xl shadow-lg border border-gray-100 mb-8 animate-fade-in">
                    <h3 className="text-lg font-semibold mb-4">Ingredient Nou</h3>
                    <form onSubmit={handleAddIngredient} className="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
                        <div className="md:col-span-2">
                            <label className="block text-sm font-medium text-gray-700 mb-1">Nume</label>
                            <input
                                type="text"
                                required
                                value={newName}
                                onChange={e => setNewName(e.target.value)}
                                className="w-full p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                                placeholder="Ex: Făină"
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Unitate</label>
                            <select
                                value={newUnit}
                                onChange={e => setNewUnit(e.target.value)}
                                className="w-full p-2 border border-gray-300 rounded-lg"
                            >
                                <option value="kg">kg</option>
                                <option value="g">g</option>
                                <option value="l">l</option>
                                <option value="ml">ml</option>
                                <option value="buc">buc</option>
                            </select>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Prag alertă stoc</label>
                            <input
                                type="number"
                                required
                                value={newThreshold}
                                onChange={e => setNewThreshold(e.target.value)}
                                className="w-full p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                                placeholder="Ex: 5"
                            />
                        </div>
                        <button type="submit" className="bg-green-600 text-white p-2 rounded-lg hover:bg-green-700">
                            Salvează
                        </button>
                    </form>
                </div>
            )}

            {/* Adjust Stock Modal/Form Overlay */}
            {adjustingItem && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-xl shadow-xl max-w-md w-full p-6 animate-fade-in">
                        <h3 className="text-xl font-bold mb-4">Ajustare Stoc: {adjustingItem.name}</h3>
                        <form onSubmit={handleAdjustStock} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">Tip Tranzacție</label>
                                <select
                                    value={adjustType}
                                    onChange={e => setAdjustType(Number(e.target.value))}
                                    className="w-full p-2 border border-gray-300 rounded-lg"
                                >
                                    <option value={0}>Restock (Livrare)</option>
                                    <option value={1}>Usage (Consum)</option>
                                    <option value={2}>Waste (Pierdere)</option>
                                    <option value={3}>Adjustment (Corecție)</option>
                                </select>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Cantitate</label>
                                <div className="flex items-center gap-2">
                                    <input
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        required
                                        value={adjustQty}
                                        onChange={e => setAdjustQty(e.target.value)}
                                        className="flex-1 p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                                        placeholder="10"
                                    />
                                    <span className="text-gray-500 font-medium">{adjustingItem.unit}</span>
                                </div>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Motiv / Notă</label>
                                <input
                                    type="text"
                                    value={adjustReason}
                                    onChange={e => setAdjustReason(e.target.value)}
                                    className="w-full p-2 border border-gray-300 rounded-lg"
                                    placeholder="Ex: Livrare nouă, Expirat, Consumat"
                                />
                            </div>
                            <div className="flex justify-end gap-3 mt-6">
                                <button
                                    type="button"
                                    onClick={() => {
                                        setAdjustingItem(null)
                                        setAdjustType(0)
                                    }}
                                    className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg"
                                >
                                    Anulează
                                </button>
                                <button
                                    type="submit"
                                    className="px-4 py-2 bg-brand-600 text-white rounded-lg hover:bg-brand-700 shadow-sm"
                                >
                                    Actualizează
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Search and Filter */}
            <div className="bg-white rounded-t-xl border-b border-gray-200 p-4 flex justify-between items-center">
                <div className="relative w-full max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
                    <input 
                        type="text" 
                        placeholder="Caută ingredient..." 
                        value={searchTerm}
                        onChange={e => setSearchTerm(e.target.value)}
                        className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-full focus:outline-none focus:ring-2 focus:ring-brand-500"
                    />
                </div>
                <button onClick={fetchInventory} className="p-2 text-gray-500 hover:bg-gray-100 rounded-full" title="Refresh">
                    <RefreshCw size={20} />
                </button>
            </div>

            {/* Inventory Table */}
            <div className="bg-white shadow-sm rounded-b-xl overflow-hidden border border-gray-200">
                <table className="w-full text-left border-collapse">
                    <thead>
                        <tr className="bg-gray-50 text-gray-600 text-sm uppercase tracking-wider">
                            <th className="p-4 font-semibold">Ingredient</th>
                            <th className="p-4 font-semibold">Stoc Curent</th>
                            <th className="p-4 font-semibold">Unitate</th>
                            <th className="p-4 font-semibold">Ultima Actualizare</th>
                            <th className="p-4 font-semibold">Alertă Stoc</th>
                            <th className="p-4 font-semibold">Status</th>
                            <th className="p-4 font-semibold text-right">Acțiuni</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {loading ? (
                            <tr><td colSpan={7} className="p-8 text-center text-gray-500">Se încarcă stocurile...</td></tr>
                        ) : filteredIngredients.length === 0 ? (
                            <tr><td colSpan={7} className="p-8 text-center text-gray-500">Nu s-au găsit ingrediente.</td></tr>
                        ) : (
                            filteredIngredients.map(item => {
                                const isLowStock = item.currentStock <= item.lowStockThreshold;
                                let statusLabel;
                                if (isLowStock) {
                                    statusLabel = (
                                        <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-red-100 text-red-700">
                                            <AlertTriangle size={12} /> Stoc Critic
                                        </span>
                                    );
                                } else {
                                    statusLabel = (
                                        <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                                            În Stoc
                                        </span>
                                    );
                                }
                                return (
                                    <tr key={item.id} className="hover:bg-gray-50 transition-colors">
                                        <td className="p-4 font-medium text-gray-900">{item.name}</td>
                                        <td className="p-4">
                                            <span className="font-bold text-lg">{item.currentStock}</span>
                                        </td>
                                        <td className="p-4">{item.unit}</td>
                                        <td className="p-4 flex items-center gap-2 text-gray-500">
                                            <CalendarDays size={16} />
                                            {new Date(item.updatedAt).toLocaleDateString('ro-RO', {
                                                day: '2-digit',
                                                month: '2-digit',
                                                year: 'numeric',}
                                            )}
                                        </td>
                                        <td className="p-4 text-gray-500">
                                            Sub {item.lowStockThreshold} {item.unit}
                                        </td>
                                        <td className="p-4">
                                            {statusLabel}
                                        </td>
                                        <td className="p-4 text-right">
                                            <button
                                                onClick={() => {
                                                    setAdjustingItem(item)
                                                    setAdjustQty('')
                                                    setAdjustReason('')
                                                }}
                                                className="p-2 text-brand-600 hover:bg-brand-50 rounded-lg transition-colors"
                                                title="Ajustează Stoc"
                                            >
                                                <Edit2 size={18} />
                                            </button>
                                        </td>
                                    </tr>
                                )
                            })
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    )
}
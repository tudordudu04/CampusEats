import { useState, useEffect } from 'react'
import { InventoryApi } from '../services/api'
import { Package, Plus, AlertTriangle, Search, RefreshCw, Edit2, CalendarDays } from 'lucide-react'
import type { InventoryItemDto } from '../types'
import { useLanguage } from '../contexts/LanguageContext'

export default function InventoryPage() {
    const [ingredients, setIngredients] = useState<InventoryItemDto[]>([])
    const [loading, setLoading] = useState(true)
    const [searchTerm, setSearchTerm] = useState('')
    const [showAddForm, setShowAddForm] = useState(false)
    const { language } = useLanguage()

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
            <div className=" md: flex justify-between items-center mb-8">
                <div className="flex-col items-center gap-3">
                    <h1 className="text-3xl md:text-3xl font-bold text-gray-900 dark:text-slate-100 flex items-center gap-3">
                        <Package className="size-16 text-brand-600 md:text-brand-600" />
                        {language === 'ro' ? 'Inventar Bucătărie' : 'Kitchen Inventory'}
                    </h1>
                    <p className="py-2 md:text-gray-500 dark:text-slate-400 mt-1">{language === 'ro' ? 'Gestionează stocurile de ingrediente' : 'Manage ingredient stocks'}</p>
                </div>
                <button
                    onClick={() => setShowAddForm(!showAddForm)}
                    className="hidden md:flex bg-brand-600 dark:bg-brand-700 text-white px-6 py-2.5 rounded-lg hover:bg-brand-700 dark:hover:bg-brand-600 items-center gap-2 transition-colors shadow-sm"
                >
                    <Plus size={20} />
                    {language === 'ro' ? 'Adaugă Ingredient' : 'Add Ingredient'}
                </button>
            </div>
            <button
                onClick={() => setShowAddForm(!showAddForm)}
                className="flex md:hidden bg-brand-600 dark:bg-brand-700 text-white px-4 py-2 rounded-lg hover:bg-brand-700 dark:hover:bg-brand-600 w-full mb-3 items-center gap-2 transition-colors shadow-md"
            >
                <Plus size={20} />
                {language === 'ro' ? 'Adaugă Ingredient' : 'Add Ingredient'}
            </button>
            {/* Add Ingredient Form */}
            {showAddForm && (
                <div className="bg-white dark:bg-slate-800 p-6 rounded-xl shadow-lg border border-gray-100 dark:border-slate-700 mb-8 animate-fade-in">
                    <h3 className="text-lg font-semibold mb-4 dark:text-slate-100">{language === 'ro' ? 'Ingredient Nou' : 'New Ingredient'}</h3>
                    <form onSubmit={handleAddIngredient} className="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
                        <div className="md:col-span-2">
                            <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Nume' : 'Name'}</label>
                            <input
                                type="text"
                                required
                                value={newName}
                                onChange={e => setNewName(e.target.value)}
                                className="w-full p-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-transparent bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                                placeholder={language === 'ro' ? 'Ex: Făină' : 'E.g.: Flour'}
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Unitate' : 'Unit'}</label>
                            <select
                                value={newUnit}
                                onChange={e => setNewUnit(e.target.value)}
                                className="w-full p-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                            >
                                <option value="kg">kg</option>
                                <option value="g">g</option>
                                <option value="l">l</option>
                                <option value="ml">ml</option>
                                <option value="buc">buc</option>
                            </select>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Prag alertă stoc' : 'Stock alert threshold'}</label>
                            <input
                                type="number"
                                required
                                value={newThreshold}
                                onChange={e => setNewThreshold(e.target.value)}
                                className="w-full p-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-transparent bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                                placeholder={language === 'ro' ? 'Ex: 5' : 'E.g.: 5'}
                            />
                        </div>
                        <button type="submit" className="bg-green-600 dark:bg-green-700 text-white p-2 rounded-lg hover:bg-green-700 dark:hover:bg-green-600">
                            {language === 'ro' ? 'Salvează' : 'Save'}
                        </button>
                    </form>
                </div>
            )}

            {/* Adjust Stock Modal/Form Overlay */}
            {adjustingItem && (
                <div className="fixed inset-0 bg-black/50 dark:bg-black/70 flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-xl max-w-md w-full p-6 animate-fade-in">
                        <h3 className="text-xl font-bold mb-4 dark:text-slate-100">{language === 'ro' ? `Ajustare Stoc: ${adjustingItem.name}` : `Adjust Stock: ${adjustingItem.name}`}</h3>
                        <form onSubmit={handleAdjustStock} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">{language === 'ro' ? 'Tip Tranzacție' : 'Transaction Type'}</label>
                                <select
                                    value={adjustType}
                                    onChange={e => setAdjustType(Number(e.target.value))}
                                    className="w-full p-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                                >
                                    <option value={0}>{language === 'ro' ? 'Restock (Livrare)' : 'Restock (Delivery)'}</option>
                                    <option value={1}>{language === 'ro' ? 'Usage (Consum)' : 'Usage (Consumption)'}</option>
                                    <option value={2}>{language === 'ro' ? 'Waste (Pierdere)' : 'Waste (Loss)'}</option>
                                    <option value={3}>{language === 'ro' ? 'Adjustment (Corecție)' : 'Adjustment (Correction)'}</option>
                                </select>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Cantitate' : 'Quantity'}</label>
                                <div className="flex items-center gap-2">
                                    <input
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        required
                                        value={adjustQty}
                                        onChange={e => setAdjustQty(e.target.value)}
                                        className="flex-1 p-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-transparent bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                                        placeholder="10"
                                    />
                                    <span className="text-gray-500 dark:text-slate-400 font-medium">{adjustingItem.unit}</span>
                                </div>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Motiv / Notă' : 'Reason / Note'}</label>
                                <input
                                    type="text"
                                    value={adjustReason}
                                    onChange={e => setAdjustReason(e.target.value)}
                                    className="w-full p-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                                    placeholder={language === 'ro' ? 'Ex: Livrare nouă, Expirat, Consumat' : 'E.g.: New delivery, Expired, Consumed'}
                                />
                            </div>
                            <div className="flex justify-end gap-3 mt-6">
                                <button
                                    type="button"
                                    onClick={() => {
                                        setAdjustingItem(null)
                                        setAdjustType(0)
                                    }}
                                    className="px-4 py-2 text-gray-600 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-slate-700 rounded-lg"
                                >
                                    {language === 'ro' ? 'Anulează' : 'Cancel'}
                                </button>
                                <button
                                    type="submit"
                                    className="px-4 py-2 bg-brand-600 dark:bg-brand-700 text-white rounded-lg hover:bg-brand-700 dark:hover:bg-brand-600 shadow-sm"
                                >
                                    {language === 'ro' ? 'Actualizează' : 'Update'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Search and Filter */}
            <div className="bg-white dark:bg-slate-800 rounded-t-xl border-b border-gray-200 dark:border-slate-700 p-4 flex justify-between items-center">
                <div className="relative w-full max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 dark:text-slate-500" size={18} />
                    <input
                        type="text"
                        placeholder={language === 'ro' ? 'Caută ingredient...' : 'Search ingredient...'}
                        value={searchTerm}
                        onChange={e => setSearchTerm(e.target.value)}
                        className="w-full pl-10 pr-4 py-2 border border-gray-200 dark:border-slate-600 rounded-full focus:outline-none focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100 placeholder:text-gray-400 dark:placeholder:text-slate-500"
                    />
                </div>
                <button onClick={fetchInventory} className="p-2 text-gray-500 dark:text-slate-400 hover:bg-gray-100 dark:hover:bg-slate-700 rounded-full" title="Refresh">
                    <RefreshCw size={20} />
                </button>
            </div>

            {/* Mobile: Vertical Inventory Table (ingredients as columns, scroll sideways) */}
            <div className="bg-white dark:bg-slate-800 shadow-sm h-full rounded-b-xl overflow-x-auto border border-gray-200 dark:border-slate-700 md:hidden">
                {loading ? (
                    <div className="p-8 text-center text-gray-500 dark:text-slate-400">
                        {language === 'ro' ? 'Se încarcă stocurile...' : 'Loading stocks...'}
                    </div>
                ) : filteredIngredients.length === 0 ? (
                    <div className="p-8 text-center text-gray-500 dark:text-slate-400">
                        {language === 'ro' ? 'Nu s-au găsit ingrediente.' : 'No ingredients found.'}
                    </div>
                ) : (
                    <table className="min-w-max text-left h-full border-separate border-spacing-3">
                        <thead>
                        <tr className="bg-gray-50 dark:bg-slate-900 text-gray-600 dark:text-slate-400 text-xs uppercase tracking-wider">
                            {/* Left header column label (vertical table header) */}
                            <th className="p-2 font-semibold text-left align-bottom">
                                {language === 'ro' ? 'Detaliu' : 'Details'}
                            </th>
                            {filteredIngredients.map(item => (
                                <th
                                    key={item.id}
                                    className="p-2 font-semibold text-center align-bottom"
                                >
                                    <div className="mx-auto text-gray-900 dark:text-slate-100 font-medium">
                                        {item.name}
                                    </div>
                                </th>
                            ))}
                        </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100 dark:divide-slate-700 text-sm gap-2">
                        {/* Stoc Curent */}
                        <tr>
                            <th className="p-2 font-semibold text-gray-700 dark:text-slate-300 text-left bg-white dark:bg-slate-800">
                                {language === 'ro' ? 'Stoc Curent' : 'Current Stock'}
                            </th>
                            {filteredIngredients.map(item => (
                                <td key={item.id} className="p-2 text-center">
                                        <span className="font-bold text-base dark:text-slate-100">
                                            {item.currentStock}
                                        </span>
                                </td>
                            ))}
                        </tr>

                        {/* Unitate */}
                        <tr>
                            <th className="p-2 font-semibold text-gray-700 dark:text-slate-300 text-left bg-white dark:bg-slate-800">
                                {language === 'ro' ? 'Unitate' : 'Unit'}
                            </th>
                            {filteredIngredients.map(item => (
                                <td key={item.id} className="p-2 text-center text-gray-600 dark:text-slate-400">
                                    {item.unit}
                                </td>
                            ))}
                        </tr>

                        {/* Ultima Actualizare */}
                        <tr>
                            <th className="p-2 font-semibold text-gray-700 dark:text-slate-300 text-left bg-white dark:bg-slate-800">
                                {language === 'ro' ? 'Ultima Actualizare' : 'Last Updated'}
                            </th>
                            {filteredIngredients.map(item => (
                                <td key={item.id} className="p-2 text-center text-gray-500 dark:text-slate-400">
                                    <div className="flex flex-col items-center gap-1">
                                        <CalendarDays size={14} className="text-gray-400 dark:text-slate-500" />
                                        <span className="text-xs">
                                                {new Date(item.updatedAt).toLocaleDateString('ro-RO', {
                                                    day: '2-digit',
                                                    month: '2-digit',
                                                    year: 'numeric',
                                                })}
                                            </span>
                                    </div>
                                </td>
                            ))}
                        </tr>

                        {/* Alertă Stoc */}
                        <tr>
                            <th className="p-2 font-semibold text-gray-700 dark:text-slate-300 text-left bg-white dark:bg-slate-800">
                                {language === 'ro' ? 'Alertă Stoc' : 'Stock Alert'}
                            </th>
                            {filteredIngredients.map(item => (
                                <td key={item.id} className="p-2 text-center text-gray-500 dark:text-slate-400 text-xs">
                                    {language === 'ro' ? `Sub ${item.lowStockThreshold} ${item.unit}` : `Below ${item.lowStockThreshold} ${item.unit}`}
                                </td>
                            ))}
                        </tr>

                        {/* Status */}
                        <tr>
                            <th className="p-2 font-semibold text-gray-700 dark:text-slate-300 text-left bg-white dark:bg-slate-800">
                                Status
                            </th>
                            {filteredIngredients.map(item => {
                                const isLowStock = item.currentStock <= item.lowStockThreshold
                                let statusLabel
                                if (isLowStock) {
                                    statusLabel = (
                                        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-[10px] font-medium bg-red-100 dark:bg-red-950/50 text-red-700 dark:text-red-400">
                                                <AlertTriangle size={10} /> {language === 'ro' ? 'Stoc Critic' : 'Critical Stock'}
                                            </span>
                                    )
                                } else {
                                    statusLabel = (
                                        <span className="inline-flex items-center px-2 py-1 rounded-full text-[10px] font-medium bg-green-100 dark:bg-green-950/50 text-green-700 dark:text-green-400">
                                                {language === 'ro' ? 'În Stoc' : 'In Stock'}
                                            </span>
                                    )
                                }
                                return (
                                    <td key={item.id} className="p-2 text-center">
                                        {statusLabel}
                                    </td>
                                )
                            })}
                        </tr>

                        {/* Acțiuni */}
                        <tr>
                            <th className="p-2 font-semibold text-gray-700 dark:text-slate-300 text-left bg-white dark:bg-slate-800">
                                {language === 'ro' ? 'Acțiuni' : 'Actions'}
                            </th>
                            {filteredIngredients.map(item => (
                                <td key={item.id} className="p-2 text-center">
                                    <button
                                        onClick={() => {
                                            setAdjustingItem(item)
                                            setAdjustQty('')
                                            setAdjustReason('')
                                        }}
                                        className="p-2 text-brand-600 dark:text-brand-400 hover:bg-brand-50 dark:hover:bg-brand-900/30 rounded-lg transition-colors inline-flex items-center justify-center"
                                        title={language === 'ro' ? 'Ajustează Stoc' : 'Adjust Stock'}
                                    >
                                        <Edit2 size={16} />
                                    </button>
                                </td>
                            ))}
                        </tr>
                        </tbody>
                    </table>
                )}
            </div>

            {/* Desktop: original horizontal Inventory Table (unchanged layout) */}
            <div className="bg-white dark:bg-slate-800 shadow-sm rounded-b-xl overflow-hidden border border-gray-200 dark:border-slate-700 hidden md:block">
                <table className="w-full text-left border-collapse">
                    <thead>
                    <tr className="bg-gray-50 dark:bg-slate-900 text-gray-600 dark:text-slate-400 text-sm uppercase tracking-wider">
                        <th className="p-4 font-semibold">{language === 'ro' ? 'Ingredient' : 'Ingredient'}</th>
                        <th className="p-4 font-semibold">{language === 'ro' ? 'Stoc Curent' : 'Current Stock'}</th>
                        <th className="p-4 font-semibold">{language === 'ro' ? 'Unitate' : 'Unit'}</th>
                        <th className="p-4 font-semibold">{language === 'ro' ? 'Ultima Actualizare' : 'Last Updated'}</th>
                        <th className="p-4 font-semibold">{language === 'ro' ? 'Alertă Stoc' : 'Stock Alert'}</th>
                        <th className="p-4 font-semibold">Status</th>
                        <th className="p-4 font-semibold text-right">{language === 'ro' ? 'Acțiuni' : 'Actions'}</th>
                    </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100 dark:divide-slate-700">
                    {loading ? (
                        <tr><td colSpan={7} className="p-8 text-center text-gray-500 dark:text-slate-400">{language === 'ro' ? 'Se încarcă stocurile...' : 'Loading stocks...'}</td></tr>
                    ) : filteredIngredients.length === 0 ? (
                        <tr><td colSpan={7} className="p-8 text-center text-gray-500 dark:text-slate-400">{language === 'ro' ? 'Nu s-au găsit ingrediente.' : 'No ingredients found.'}</td></tr>
                    ) : (
                        filteredIngredients.map(item => {
                            const isLowStock = item.currentStock <= item.lowStockThreshold;
                            let statusLabel;
                            if (isLowStock) {
                                statusLabel = (
                                    <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-red-100 dark:bg-red-950/50 text-red-700 dark:text-red-400">
                                            <AlertTriangle size={12} /> {language === 'ro' ? 'Stoc Critic' : 'Critical Stock'}
                                        </span>
                                );
                            } else {
                                statusLabel = (
                                    <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-green-100 dark:bg-green-950/50 text-green-700 dark:text-green-400">
                                            {language === 'ro' ? 'În Stoc' : 'In Stock'}
                                        </span>
                                );
                            }
                            return (
                                <tr key={item.id} className="hover:bg-gray-50 dark:hover:bg-slate-700/50 transition-colors">
                                    <td className="p-4 font-medium text-gray-900 dark:text-slate-100">{item.name}</td>
                                    <td className="p-4">
                                        <span className="font-bold text-lg dark:text-slate-100">{item.currentStock}</span>
                                    </td>
                                    <td className="p-4 dark:text-slate-300">{item.unit}</td>
                                    <td className="p-4 flex items-center gap-2 text-gray-500 dark:text-slate-400">
                                        <CalendarDays size={16} />
                                        {new Date(item.updatedAt).toLocaleDateString(language === 'ro' ? 'ro-RO' : 'en-US', {
                                            day: '2-digit',
                                            month: '2-digit',
                                            year: 'numeric',}
                                        )}
                                    </td>
                                    <td className="p-4 text-gray-500 dark:text-slate-400">
                                        {language === 'ro' ? `Sub ${item.lowStockThreshold} ${item.unit}` : `Below ${item.lowStockThreshold} ${item.unit}`}
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
                                            className="p-2 text-brand-600 dark:text-brand-400 hover:bg-brand-50 dark:hover:bg-brand-900/30 rounded-lg transition-colors"
                                            title={language === 'ro' ? 'Ajustează Stoc' : 'Adjust Stock'}
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
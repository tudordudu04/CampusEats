import React, { useState, useEffect } from 'react'
import { MenuApi } from '../services/api'
import type { CreateMenuItem, MenuItem } from '../types'

const CATEGORY_OPTIONS = [
    { label: 'Pizza', value: 0 },
    { label: 'Burger', value: 1 },
    { label: 'Salad', value: 2 },
    { label: 'Soup', value: 3 },
    { label: 'Dessert', value: 4 },
    { label: 'Drink', value: 5 },
    { label: 'Other', value: 6 },
]

export default function MenuPage() {
    const [items, setItems] = useState<MenuItem[]>([])
    const [name, setName] = useState('')
    const [price, setPrice] = useState<number | ''>('')
    const [description, setDescription] = useState('')
    const [category, setCategory] = useState<number>(0) // number state
    const [loading, setLoading] = useState(false)

    useEffect(() => {
        load()
    }, [])

    async function load() {
        const data = await MenuApi.list()
        setItems(data)
    }

    async function onCreate(e: React.FormEvent) {
        e.preventDefault()
        setLoading(true)
        try {
            const payload: CreateMenuItem = {
                name,
                price: typeof price === 'number' ? price : parseFloat(String(price)),
                description: description || null,
                category: category, // ensure CreateMenuItem.category is a number
                imageUrl: null,
                allergens: []
            }
            await MenuApi.create(payload)
            setName('')
            setPrice('')
            setDescription('')
            setCategory(0)
            await load()
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="space-y-8">
            {/* make this full width inside the admin card */}
            <div className="w-full bg-white rounded-2xl shadow border border-gray-100 p-6">
                <h3 className="text-xl font-semibold mb-1">Create menu item</h3>
                <p className="text-sm text-gray-500 mb-6">
                    Add a new dish to the menu. You can edit or remove it later.
                </p>

                <form onSubmit={onCreate} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            Name
                        </label>
                        <input
                            value={name}
                            onChange={e => setName(e.target.value)}
                            required
                            className="w-full px-3 py-2.5 rounded-xl border border-gray-300 bg-gray-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm"
                            placeholder="Margherita Pizza"
                        />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Price [RON]
                            </label>
                            <input
                                type="number"
                                step="0.01"
                                value={price}
                                onChange={e =>
                                    setPrice(e.target.value === '' ? '' : Number(e.target.value))
                                }
                                required
                                className="w-full px-3 py-2.5 rounded-xl border border-gray-300 bg-gray-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm"
                                placeholder="24.50"
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                Category
                            </label>
                            <select
                                value={String(category)}
                                onChange={e => setCategory(Number(e.target.value))}
                                className="w-full px-3 py-2.5 rounded-xl border border-gray-300 bg-gray-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm"
                            >
                                {CATEGORY_OPTIONS.map(c => (
                                    <option key={c.value} value={String(c.value)}>
                                        {c.label}
                                    </option>
                                ))}
                            </select>
                        </div>
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            Description [optional]
                        </label>
                        <textarea
                            value={description}
                            onChange={e => setDescription(e.target.value)}
                            rows={3}
                            className="w-full px-3 py-2.5 rounded-xl border border-gray-300 bg-gray-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm resize-none"
                            placeholder="Short description of the dish..."
                        />
                    </div>

                    <div className="pt-2">
                        <button
                            type="submit"
                            disabled={loading}
                            className="inline-flex items-center justify-center px-4 py-2.5 rounded-xl text-sm font-semibold text-white bg-brand-600 hover:bg-brand-700 shadow-md shadow-brand-500/20 disabled:opacity-70 transition"
                        >
                            {loading ? 'Creating…' : 'Create item'}
                        </button>
                    </div>
                </form>
            </div>

            <div>
                <h3 className="text-lg font-semibold mb-3">Current menu items</h3>
                <div className="bg-white rounded-2xl shadow border border-gray-100 overflow-hidden">
                    <table className="min-w-full text-sm">
                        <thead className="bg-gray-50">
                        <tr>
                            <th className="px-4 py-2 text-left font-medium text-gray-600">Name</th>
                            <th className="px-4 py-2 text-left font-medium text-gray-600">Price</th>
                            <th className="px-4 py-2 text-left font-medium text-gray-600">Category</th>
                        </tr>
                        </thead>
                        <tbody>
                        {items.length === 0 && (
                            <tr>
                                <td
                                    colSpan={3}
                                    className="px-4 py-4 text-center text-gray-500"
                                >
                                    No items yet. Create your first menu item above.
                                </td>
                            </tr>
                        )}
                        {items.map(i => (
                            <tr key={i.id} className="border-t border-gray-100">
                                <td className="px-4 py-2">{i.name}</td>
                                <td className="px-4 py-2 text-gray-700">
                                    {i.price.toFixed(2)} RON
                                </td>
                                <td className="px-4 py-2 text-gray-500">{i.category}</td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    )
}

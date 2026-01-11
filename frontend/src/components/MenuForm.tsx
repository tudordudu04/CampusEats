import React, { useState, useEffect } from 'react'
import { MenuApi, FileApi } from '../services/api'
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
    const [imageUrl, setImageUrl] = useState<string | null>(null)
    const [uploading, setUploading] = useState(false)
    const [uploadError, setUploadError] = useState<string | null>(null)

    useEffect(() => {
        load()
    }, [])

    async function load() {
        const data = await MenuApi.list()
        setItems(data)
    }

    async function handleImageChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0]
        if (!file) return
        setUploadError(null)
        setUploading(true)
        try {
            const { url } = await FileApi.uploadMenuImage(file)
            setImageUrl(url)
        } catch (err: any) {
            setUploadError(err.message || 'Failed to upload image')
        } finally {
            setUploading(false)
        }
    }
    
    async function onCreate(e: React.FormEvent) {
        e.preventDefault()
        setLoading(true)
        try {
            const payload: CreateMenuItem = {
                name,
                price: typeof price === 'number' ? price : parseFloat(String(price)),
                description: description || null,
                category: category, 
                imageUrl: imageUrl,
                allergens: []
            }
            await MenuApi.create(payload)
            setName('')
            setPrice('')
            setDescription('')
            setCategory(0)
            setImageUrl(null)
            await load()
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="space-y-8">
            <div className="w-full bg-white dark:bg-slate-800 rounded-2xl shadow border border-gray-100 dark:border-slate-700 p-6">
                <h3 className="text-xl font-semibold mb-1 dark:text-slate-100">Create menu item</h3>
                <p className="text-sm text-gray-500 dark:text-slate-400 mb-6">
                    Add a new dish to the menu. You can edit or remove it later.
                </p>

                <form onSubmit={onCreate} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">
                            Name
                        </label>
                        <input
                            value={name}
                            onChange={e => setName(e.target.value)}
                            required
                            className="w-full px-3 py-2.5 rounded-xl border border-gray-300 dark:border-slate-600 bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm text-gray-900 dark:text-slate-100"
                            placeholder="Margherita Pizza"
                        />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">
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
                                className="w-full px-3 py-2.5 rounded-xl border border-gray-300 dark:border-slate-600 bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm text-gray-900 dark:text-slate-100"
                                placeholder="24.50"
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">
                                Category
                            </label>
                            <select
                                value={String(category)}
                                onChange={e => setCategory(Number(e.target.value))}
                                className="w-full px-3 py-2.5 rounded-xl border border-gray-300 dark:border-slate-600 bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm text-gray-900 dark:text-slate-100"
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
                        <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">
                            Description [optional]
                        </label>
                        <textarea
                            value={description}
                            onChange={e => setDescription(e.target.value)}
                            rows={3}
                            className="w-full px-3 py-2.5 rounded-xl border border-gray-300 dark:border-slate-600 bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500 text-sm resize-none text-gray-900 dark:text-slate-100"
                            placeholder="Short description of the dish..."
                        />
                    </div>
                    
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">
                            Image
                        </label>
                        <div className="flex items-center gap-3">
                            <input
                                type="file"
                                accept="image/*"
                                onChange={handleImageChange}
                                disabled={uploading}
                                className="text-sm"
                            />
                            {uploading && <span className="text-xs text-gray-500 dark:text-slate-400">Uploading…</span>}
                        </div>
                        {uploadError && (
                            <div className="mt-1 text-xs text-red-600 dark:text-red-400">{uploadError}</div>
                        )}
                        {imageUrl && (
                            <div className="mt-3">
                                <p className="text-xs text-gray-500 dark:text-slate-400 mb-1">Preview:</p>
                                <img
                                    src={imageUrl}
                                    alt="Preview"
                                    className="h-24 w-24 object-cover rounded-lg border dark:border-slate-600"
                                />
                            </div>
                        )}
                    </div>
                    
                    <div className="pt-2">
                        <button
                            type="submit"
                            disabled={loading}
                            className="inline-flex items-center justify-center px-4 py-2.5 rounded-xl text-sm font-semibold text-white bg-brand-600 dark:bg-brand-700 hover:bg-brand-700 dark:hover:bg-brand-600 shadow-md shadow-brand-500/20 disabled:opacity-70 transition"
                        >
                            {loading ? 'Creating…' : 'Create item'}
                        </button>
                    </div>
                </form>
            </div>

            <div>
                <h3 className="text-lg font-semibold mb-3 dark:text-slate-100">Current menu items</h3>
                <div className="bg-white dark:bg-slate-800 rounded-2xl shadow border border-gray-100 dark:border-slate-700">
                    {/* Make table horizontally scrollable on small screens */}
                    <div className="overflow-x-auto">
                        <table className="min-w-[480px] w-full text-sm">
                            <thead className="bg-gray-50 dark:bg-slate-900">
                            <tr>
                                <th className="px-4 py-2 text-left font-medium text-gray-600 dark:text-slate-400">Name</th>
                                <th className="px-4 py-2 text-left font-medium text-gray-600 dark:text-slate-400">Price</th>
                                <th className="px-4 py-2 text-left font-medium text-gray-600 dark:text-slate-400">Category</th>
                                <th className="px-4 py-2 text-left font-medium text-gray-600 dark:text-slate-400">Actions</th>
                            </tr>
                            </thead>
                            <tbody>
                            {items.length === 0 && (
                                <tr>
                                    <td
                                        colSpan={4}
                                        className="px-4 py-4 text-center text-gray-500 dark:text-slate-400"
                                    >
                                        No items yet. Create your first menu item above.
                                    </td>
                                </tr>
                            )}
                            {items.map(i => (
                                <tr key={i.id} className="border-t border-gray-100 dark:border-slate-700">
                                    <td className="px-4 py-2 dark:text-slate-200">{i.name}</td>
                                    <td className="px-4 py-2 text-gray-700 dark:text-slate-300">
                                        {i.price.toFixed(2)} RON
                                    </td>
                                    <td className="px-4 py-2 text-gray-500 dark:text-slate-400">{i.category}</td>
                                    <td className="px-4 py-2">
                                        <button
                                            onClick={async () => {
                                                if (confirm(`Remove "${i.name}" from menu?`)) {
                                                    await MenuApi.delete(i.id)
                                                    await load()
                                                }
                                            }}
                                            className="px-3 py-1 text-sm bg-red-600 dark:bg-red-700 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
                                        >
                                            Remove
                                        </button>
                                    </td>
                                </tr>
                            ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    )
}

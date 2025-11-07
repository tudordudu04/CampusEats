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
        <div>
            <form onSubmit={onCreate} style={{ display: 'grid', gap: 8, maxWidth: 420 }}>
                <h3>Create menu item</h3>
                <label>
                    Name
                    <input value={name} onChange={e => setName(e.target.value)} required />
                </label>
                <label>
                    Price
                    <input
                        type="number"
                        step="0.01"
                        value={price}
                        onChange={e => setPrice(e.target.value === '' ? '' : Number(e.target.value))}
                        required
                    />
                </label>
                <label>
                    Description
                    <input value={description} onChange={e => setDescription(e.target.value)} />
                </label>
                <label>
                    Category
                    <select
                        value={String(category)}                      /* pass string to select */
                        onChange={e => setCategory(Number(e.target.value))} /* convert back to number */
                    >
                        {CATEGORY_OPTIONS.map(c => (
                            <option key={c.value} value={String(c.value)}> {/* option values as strings */}
                                {c.label}
                            </option>
                        ))}
                    </select>
                </label>
                <button type="submit" disabled={loading}>
                    {loading ? 'Creating…' : 'Create'}
                </button>
            </form>

            <hr />

            <h3>Items</h3>
            <ul>
                {items.map(i => (
                    <li key={i.id}>
                        {i.name} — {i.price} — {i.category}
                    </li>
                ))}
            </ul>
        </div>
    )
}

import { useEffect, useMemo, useState } from 'react'
import { MenuApi } from '../services/api'
import type { CreateMenuItem, MenuItem, UpdateMenuItem } from '../types'
import MenuForm from '../components/MenuForm'

export default function MenuPage() {
    const [items, setItems] = useState<MenuItem[]>([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [editing, setEditing] = useState<MenuItem | null>(null)
    const [search, setSearch] = useState('')

    const filtered = useMemo(() => {
        const s = search.trim().toLowerCase()
        if (!s) return items
        return items.filter(x =>
            x.name.toLowerCase().includes(s) ||
            (x.description ?? '').toLowerCase().includes(s) ||
            (x.category ?? '').toLowerCase().includes(s)
        )
    }, [items, search])

    const load = async () => {
        setLoading(true)
        setError(null)
        try {
            const data = await MenuApi.list()
            setItems(data)
        } catch (e: any) {
            setError(e.message ?? 'Failed to load menu')
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => { load() }, [])

    const onCreate = async (payload: CreateMenuItem) => {
        await MenuApi.create(payload)
        await load()
    }

    const onUpdate = async (payload: CreateMenuItem) => {
        if (!editing) return
        const body: UpdateMenuItem = { id: editing.id, ...payload }
        await MenuApi.update(editing.id, body)
        setEditing(null)
        await load()
    }

    const onDelete = async (id: string) => {
        if (!confirm('Delete this menu item?')) return
        await MenuApi.delete(id)
        await load()
    }

    return (
        <div style={{ display: 'grid', gap: 24 }}>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input placeholder="Search..." value={search} onChange={e => setSearch(e.target.value)} />
                <button onClick={load} disabled={loading}>Refresh</button>
                {error && <span style={{ color: 'red' }}>{error}</span>}
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
                <div>
                    <h2>{editing ? 'Edit Menu Item' : 'Create Menu Item'}</h2>
                    <MenuForm
                        initial={editing ?? undefined}
                        onSubmit={editing ? onUpdate : onCreate}
                        submitLabel={editing ? 'Update' : 'Create'}
                    />
                    {editing && <button style={{ marginTop: 8 }} onClick={() => setEditing(null)}>Cancel edit</button>}
                </div>

                <div>
                    <h2>Menu Items</h2>
                    {loading ? <p>Loading...</p> : (
                        <table width="100%" cellPadding={8} style={{ borderCollapse: 'collapse' }}>
                            <thead>
                            <tr style={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
                                <th>Name</th>
                                <th>Price</th>
                                <th>Category</th>
                                <th>Allergens</th>
                                <th></th>
                            </tr>
                            </thead>
                            <tbody>
                            {filtered.map(item => (
                                <tr key={item.id} style={{ borderBottom: '1px solid #f0f0f0' }}>
                                    <td>{item.name}</td>
                                    <td>${item.price.toFixed(2)}</td>
                                    <td>{item.category ?? '-'}</td>
                                    <td>{item.allergens.join(', ') || '-'}</td>
                                    <td style={{ display: 'flex', gap: 8 }}>
                                        <button onClick={() => setEditing(item)}>Edit</button>
                                        <button onClick={() => onDelete(item.id)}>Delete</button>
                                    </td>
                                </tr>
                            ))}
                            {filtered.length === 0 && (
                                <tr><td colSpan={5}>No items</td></tr>
                            )}
                            </tbody>
                        </table>
                    )}
                </div>
            </div>
        </div>
    )
}
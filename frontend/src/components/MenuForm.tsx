import { useEffect, useState } from 'react'
import type { CreateMenuItem } from '../types'

type Props = {
    initial?: Partial<CreateMenuItem>
    onSubmit: (data: CreateMenuItem) => Promise<void> | void
    submitLabel?: string
}

export default function MenuForm({ initial, onSubmit, submitLabel = 'Save' }: Props) {
    const [name, setName] = useState(initial?.name ?? '')
    const [price, setPrice] = useState(initial?.price ?? 0)
    const [description, setDescription] = useState(initial?.description ?? '')
    const [category, setCategory] = useState(initial?.category ?? '')
    const [imageUrl, setImageUrl] = useState(initial?.imageUrl ?? '')
    const [allergens, setAllergens] = useState((initial?.allergens ?? []).join(', '))

    useEffect(() => {
        setName(initial?.name ?? '')
        setPrice(initial?.price ?? 0)
        setDescription(initial?.description ?? '')
        setCategory(initial?.category ?? '')
        setImageUrl(initial?.imageUrl ?? '')
        setAllergens((initial?.allergens ?? []).join(', '))
    }, [initial?.name, initial?.price, initial?.description, initial?.category, initial?.imageUrl, initial?.allergens])

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        const payload: CreateMenuItem = {
            name: name.trim(),
            price: Number(price),
            description: description.trim() || undefined,
            category: category.trim() || undefined,
            imageUrl: imageUrl.trim() || undefined,
            allergens: allergens
                .split(',')
                .map(a => a.trim())
                .filter(Boolean),
        }
        await onSubmit(payload)
    }

    return (
        <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 8, maxWidth: 520 }}>
            <label>
                Name
                <input value={name} onChange={e => setName(e.target.value)} required />
            </label>
            <label>
                Price
                <input type="number" step="0.01" value={price} onChange={e => setPrice(parseFloat(e.target.value))} required />
            </label>
            <label>
                Description
                <textarea value={description} onChange={e => setDescription(e.target.value)} rows={3} />
            </label>
            <label>
                Category
                <input value={category} onChange={e => setCategory(e.target.value)} />
            </label>
            <label>
                Image URL
                <input value={imageUrl} onChange={e => setImageUrl(e.target.value)} />
            </label>
            <label>
                Allergens (comma separated)
                <input value={allergens} onChange={e => setAllergens(e.target.value)} placeholder="e.g. nuts, gluten" />
            </label>
            <button type="submit">{submitLabel}</button>
        </form>
    )
}
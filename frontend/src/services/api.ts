import type { CreateMenuItem, MenuItem, UpdateMenuItem } from '../types'

const BASE_URL = 'http://localhost:5103'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
        headers: { 'Content-Type': 'application/json' },
        ...init,
    })
    if (!res.ok) {
        const text = await res.text().catch(() => '')
        throw new Error(text || `Request failed with ${res.status}`)
    }
    if (res.status === 204) {
        // No content
        return undefined as unknown as T
    }
    return res.json()
}

export const MenuApi = {
    list: () => request<MenuItem[]>('/api/menu'),
    get: (id: string) => request<MenuItem>(`/api/menu/${id}`),
    create: (body: CreateMenuItem) =>
        request<{ id: string }>('/api/menu', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: string, body: UpdateMenuItem) =>
        request<void>(`/api/menu/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    delete: (id: string) =>
        request<void>(`/api/menu/${id}`, { method: 'DELETE' }),
}
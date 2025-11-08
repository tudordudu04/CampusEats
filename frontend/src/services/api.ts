// frontend/src/services/api.ts
import type { CreateMenuItem, MenuItem, UpdateMenuItem } from '../types'

const BASE_URL = 'http://localhost:5103'

// In-memory token + localStorage persistence
let accessToken: string | null = localStorage.getItem('access_token')

export function setAccessToken(token: string | null) {
    accessToken = token
    if (token) localStorage.setItem('access_token', token)
    else localStorage.removeItem('access_token')
}

function authHeaders(): HeadersInit {
    return accessToken ? { Authorization: `Bearer ${accessToken}` } : {}
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
        credentials: 'include', // send refresh cookie when needed
        headers: {
            'Content-Type': 'application/json',
            ...authHeaders(),
            ...(init?.headers || {}),
        },
        ...init,
    })
    if (!res.ok) {
        const text = await res.text().catch(() => '')
        throw new Error(text || `Request failed with ${res.status}`)
    }
    if (res.status === 204) {
        return undefined as unknown as T
    }
    return res.json()
}

export type AuthResult = { accessToken: string }
export type RegisterBody = { name: string; email: string; password: string }
export type LoginBody = { email: string; password: string }

export const AuthApi = {
    register: async (body: RegisterBody) => {
        const result = await request<AuthResult>('/auth/register', {
            method: 'POST',
            body: JSON.stringify(body),
        })
        setAccessToken(result.accessToken)
        return result
    },
    login: async (body: LoginBody) => {
        const result = await request<AuthResult>('/auth/login', {
            method: 'POST',
            body: JSON.stringify(body),
        })
        setAccessToken(result.accessToken)
        return result
    },
    refresh: async () => {
        const result = await request<AuthResult>('/auth/refresh', { method: 'POST' })
        setAccessToken(result.accessToken)
        return result
    },
    logout: async () => {
        await request<void>('/auth/logout', { method: 'POST' })
        setAccessToken(null)
    },
    getToken: () => accessToken,
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

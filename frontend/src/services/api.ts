import type { CreateMenuItem, MenuItem, UpdateMenuItem, OrderDto, KitchenTaskDto, LoyaltyAccount, LoyaltyTransactionDto } from '../types'

const BASE_URL = 'http://localhost:5103'

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
        credentials: 'include',
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

export const PaymentApi = {
    createSession: (items: Array<{ menuItemId: string; quantity: number }>, notes?: string) =>
    request<{ sessionId: string; checkoutUrl: string }>('/api/payments/create-session', {
        method: 'POST',
        body: JSON.stringify({ items, notes })
    })
}

export const LoyaltyApi = {
    getAccount: () => request<LoyaltyAccount>('/api/loyalty/account'),
    getTransactions: () => request<LoyaltyTransactionDto[]>('/api/loyalty/transactions'),
}

export const OrderApi = {
    getAll: (all: boolean = false) => request<OrderDto[]>(`/api/orders?all=${all}`),
    getById: (id: string) => request<OrderDto>(`/api/orders/${id}`),
    cancel: (id: string) => request<void>(`/api/orders/${id}/cancel`, { method: 'POST' }),
    // Irelevanta asta de jos
    // create: (items: Array<{ menuItemId: string; quantity: number }>, notes?: string) => 
    //     request<{ orderId: string }>('/api/orders', {
    //         method: 'POST',
    //         body: JSON.stringify({ items, notes })
    //     })
}

export const KitchenApi = {
    getAll: () => request<KitchenTaskDto[]>('/api/kitchen/tasks'),
    getByStatus: (status: string) => request<KitchenTaskDto[]>(`/api/kitchen/tasks/${status}`),
    updateStatus: (id: string, status: string) => 
        request(`/api/kitchen/tasks/${id}`, {
            method: 'PUT',
            body: JSON.stringify({ id, status })
        }),
    delete: (id: string) => request(`/api/kitchen/tasks/${id}`, { method: 'DELETE' })
}
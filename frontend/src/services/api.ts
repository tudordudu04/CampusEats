import type { CreateMenuItem, MenuItem, UpdateMenuItem, OrderDto, KitchenTaskDto, LoyaltyAccount, LoyaltyTransactionDto, InventoryItemDto } from '../types'

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

export type AuthResult = { accessToken: string; }
export type RegisterBody = { name: string; email: string; password: string; role: number;}
export type LoginBody = { email: string; password: string }
export type UserDto = {
    id: string
    name: string
    email: string
    role: string
    createdAtUtc: string
    updatedAtUtc: string
}

export const FileApi = {
    uploadMenuImage: async (file: File): Promise<{ url: string }> => {
        const formData = new FormData()
        formData.append('file', file)

        const res = await fetch(`${BASE_URL}/api/menu/images`, {
            method: 'POST',
            body: formData,
            credentials: 'include',
            headers: {
                ...authHeaders(),
            },
        })

        if (!res.ok) {
            const text = await res.text().catch(() => '')
            throw new Error(text || `Upload failed with ${res.status}`)
        }

        const result = await res.json()
        return result as { url: string }
    },
}
export const AuthApi = {
    register: async (body: RegisterBody) => {
        const result = await request<AuthResult>('/auth/register', {
            method: 'POST',
            body: JSON.stringify(body),
        })
        setAccessToken(result.accessToken)
        return result
    },
    adminRegister: async (body: RegisterBody) => {
        const result = await request<AuthResult>('/auth/register', {
            method: 'POST',
            body: JSON.stringify(body),
        })
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
    getAllUsers: () => request<UserDto[]>('/auth/users'),
    deleteUser: async (id: string) => {
        const body = { userId: id }
        const result = await request<void>('/auth/delete', {
            method: 'DELETE',
            body: JSON.stringify(body),
        })
        return result
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
    createSession: (items: Array<{ menuItemId: string; quantity: number }>, notes?: string, userCouponId?: string | null) =>
    request<{ sessionId: string; checkoutUrl: string }>('/api/payments/create-session', {
        method: 'POST',
        body: JSON.stringify({ items, notes, userCouponId })
    })
}

export const LoyaltyApi = {
    getAccount: () => request<LoyaltyAccount>('/api/loyalty/account'),
    getTransactions: () => request<LoyaltyTransactionDto[]>('/api/loyalty/transactions'),
}

export const CouponApi = {
    getAvailable: () => request<import('../types').CouponDto[]>('/api/coupons/available'),
    getMyCoupons: () => request<import('../types').UserCouponDto[]>('/api/coupons/my-coupons'),
    purchase: (couponId: string) => 
        request<{ success: boolean; message: string; userCouponId?: string; remainingPoints?: number }>('/api/coupons/purchase', {
            method: 'POST',
            body: JSON.stringify({ couponId })
        }),
    create: (body: import('../types').CreateCouponRequest) =>
        request<{ success: boolean; message: string; couponId?: string }>('/api/coupons', {
            method: 'POST',
            body: JSON.stringify(body)
        })
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

export const InventoryApi = {
    getAll: () => request<InventoryItemDto[]>('/api/inventory'),
    getByName: (name: string) => request<InventoryItemDto>(`/api/inventory/${name}`),
    create: (name: string, unit: string, lowStockThreshold: number) => 
        request(`/api/inventory/ingredients`, {
            method: 'POST',
            body: JSON.stringify({ name, unit, lowStockThreshold })
        }),
    adjustStock: (ingredientId: string, quantity: number, type: number, note: string) =>
        request(`/api/inventory/adjust`, {
            method: 'PUT',
            body: JSON.stringify({ ingredientId, quantity, type, note })
        })
}
export enum UserRole {
    STUDENT = 'STUDENT',
    WORKER = 'WORKER',
    MANAGER = 'MANAGER'
}

export enum OrderStatus {
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Completed = 3,
    Cancelled = 4
}

export enum KitchenTaskStatus {
    NotStarted = 0,
    Preparing = 1,
    Ready = 2,
    Completed = 3
}

export type MenuItem = {
    id: string
    name: string
    price: number
    description: string | null
    category: number
    imageUrl: string | null
    allergens: string[]
}

export type CreateMenuItem = {
    name: string
    price: number
    description: string | null
    category: number
    imageUrl: string | null
    allergens: string[]
}

export type UpdateMenuItem = Partial<CreateMenuItem>

export type OrderItemDto = {
    id: string
    menuItemId: string
    menuItemName: string | null
    quantity: number
    unitPrice: number
}

export type OrderDto = {
    id: string
    userId: string
    status: OrderStatus
    total: number
    createdAtUtc: string
    updatedAtUtc: string
    items: OrderItemDto[]
    notes: string | null
}

export type KitchenTaskDto = {
    id: string
    orderId: string
    assignedTo: string
    status: string // Backend-ul trimite .ToString()
    notes: string | null
    updatedAt: string
    orderStatus: string 
}

export type InventoryItemDto = {
    id: string
    name: string
    currentStock: number
    unit: string
    lowStockThreshold: number
    updatedAt: string
}

export type LoyaltyAccount = {
    id: string
    userId: string
    points: number
    updatedAtUtc: string
}

// --- Adăugiri pentru Loyalty ---

export enum LoyaltyTransactionType {
    Earned = 0,
    Redeemed = 1,
    Expired = 2,
    Adjusted = 3
}

export type LoyaltyTransactionDto = {
    id: string
    pointsChange: number
    type: LoyaltyTransactionType
    description: string
    relatedOrderId: string | null
    createdAtUtc: string
}
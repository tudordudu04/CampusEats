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
    averageRating: number | null
    reviewCount: number
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

// --- Coupon Types ---

export enum CouponType {
    PercentageDiscount = 0,
    FixedAmountDiscount = 1,
    FreeItem = 2
}

export type CouponDto = {
    id: string
    name: string
    description: string
    type: CouponType
    discountValue: number
    pointsCost: number
    specificMenuItemId: string | null
    specificMenuItemName: string | null
    minimumOrderAmount: number | null
    isActive: boolean
    expiresAtUtc: string | null
}

export type UserCouponDto = {
    id: string
    couponId: string
    couponName: string
    couponDescription: string
    couponType: CouponType
    discountValue: number
    minimumOrderAmount: number | null
    specificMenuItemId: string | null
    specificMenuItemName: string | null
    acquiredAtUtc: string
    expiresAtUtc: string | null
    isUsed: boolean
    usedAtUtc: string | null
}

export type CreateCouponRequest = {
    name: string
    description: string
    type: CouponType
    discountValue: number
    pointsCost: number
    specificMenuItemId: string | null
    minimumOrderAmount: number | null
    expiresAtUtc: string | null
}

export type user = {
    id: string
    name: string
    email: string
    role: UserRole | string
    profilePictureUrl?: string | null
    addressCity?: string | null
    addressStreet?: string | null
    addressNumber?: string | null
    addressDetails?: string | null

}

// --- Review Types ---


export type ReviewDto = {
    id: string
    menuItemId: string
    userId: string
    userName: string
    rating: number
    comment: string | null
    createdAtUtc: string
    updatedAtUtc: string
}

export type MenuItemRatingDto = {
    menuItemId: string
    averageRating: number
    totalReviews: number
}

export type AddReviewRequest = {
    menuItemId: string
    rating: number
    comment: string | null
}

export type UpdateReviewRequest = {
    rating: number
    comment: string | null
}


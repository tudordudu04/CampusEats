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
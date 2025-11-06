export type MenuItem = {
    id: string
    name: string
    price: number
    description?: string | null
    category?: string | null
    imageUrl?: string | null
    allergens: string[]
}

export type CreateMenuItem = Omit<MenuItem, 'id'>
export type UpdateMenuItem = { id: string } & CreateMenuItem
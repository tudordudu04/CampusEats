import { useEffect, useState, useMemo } from 'react'
import { MenuApi } from '../services/api'
import type { MenuItem } from '../types'
import { Plus, Star, MessageSquare, Filter, ArrowUpDown, ChevronDown, ChevronUp } from 'lucide-react'
import { StarRating } from '../components/StarRating'
import { useLanguage } from '../contexts/LanguageContext'

type Props = {
    onAddToCart: (item: MenuItem) => void
    isLoggedIn?: boolean
    onViewReviews?: (item: MenuItem) => void
    refreshTrigger?: number
}

// Enum pentru categorii (sincronizat cu backend)
enum MenuCategory {
    PIZZA = 0,
    BURGER = 1,
    SALAD = 2,
    SOUP = 3,
    DESSERT = 4,
    DRINK = 5,
    OTHER = 6
}

const categoryLabels: Record<MenuCategory, string> = {
    [MenuCategory.PIZZA]: '🍕 Pizza',
    [MenuCategory.BURGER]: '🍔 Burger',
    [MenuCategory.SALAD]: '🥗 Salată',
    [MenuCategory.SOUP]: '🍲 Supă',
    [MenuCategory.DESSERT]: '🍰 Desert',
    [MenuCategory.DRINK]: '🥤 Băutură',
    [MenuCategory.OTHER]: '🍽️ Altele'
}

type SortOption = 'default' | 'price-asc' | 'price-desc' | 'rating' | 'name'

export default function MenuPage({ onAddToCart, isLoggedIn, onViewReviews, refreshTrigger }: Props) {
    const [items, setItems] = useState<MenuItem[]>([])
    const [loading, setLoading] = useState(true)
    const [selectedCategory, setSelectedCategory] = useState<MenuCategory | 'all'>('all')
    const [sortBy, setSortBy] = useState<SortOption>('default')
    const [isFilterOpen, setIsFilterOpen] = useState(false)
    const { language } = useLanguage()

    // Category labels with translations
    const categoryLabels: Record<MenuCategory, string> = {
        [MenuCategory.PIZZA]: `🍕 ${language === 'ro' ? 'Pizza' : 'Pizza'}`,
        [MenuCategory.BURGER]: `🍔 ${language === 'ro' ? 'Burger' : 'Burger'}`,
        [MenuCategory.SALAD]: `🥗 ${language === 'ro' ? 'Salată' : 'Salad'}`,
        [MenuCategory.SOUP]: `🍲 ${language === 'ro' ? 'Supă' : 'Soup'}`,
        [MenuCategory.DESSERT]: `🍰 ${language === 'ro' ? 'Desert' : 'Dessert'}`,
        [MenuCategory.DRINK]: `🥤 ${language === 'ro' ? 'Băutură' : 'Drink'}`,
        [MenuCategory.OTHER]: `🍽️ ${language === 'ro' ? 'Altele' : 'Other'}`
    }

    useEffect(() => {
        const load = async () => {
            setLoading(true)
            try {
                const data = await MenuApi.list()
                setItems(data)
            } finally {
                setLoading(false)
            }
        }
        load()
    }, [refreshTrigger])

    // Filtrare și sortare
    const filteredAndSortedItems = useMemo(() => {
        // Filtrare
        let result = selectedCategory === 'all' 
            ? items 
            : items.filter(item => item.category === selectedCategory)
        
        // Sortare
        switch (sortBy) {
            case 'price-asc':
                result = [...result].sort((a, b) => a.price - b.price)
                break
            case 'price-desc':
                result = [...result].sort((a, b) => b.price - a.price)
                break
            case 'rating':
                result = [...result].sort((a, b) => {
                    const ratingA = a.averageRating ?? 0
                    const ratingB = b.averageRating ?? 0
                    return ratingB - ratingA // descrescător (cel mai bun rating mai întâi)
                })
                break
            case 'name':
                result = [...result].sort((a, b) => a.name.localeCompare(b.name, 'ro'))
                break
            default:
                // default - ordine originală
                break
        }
        
        return result
    }, [items, selectedCategory, sortBy])

    if (loading) return (
        <div className="flex justify-center items-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-brand-600"></div>
        </div>
    )

    return (
        <div className="space-y-6">
            {/* Secțiune Filtrare și Sortare */}
            <div className="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-gray-100 dark:border-slate-700 overflow-hidden">
                {/* Header cu buton toggle */}
                <button
                    onClick={() => setIsFilterOpen(!isFilterOpen)}
                    className="w-full px-6 py-4 flex items-center justify-between hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors"
                >
                    <div className="flex items-center gap-3">
                        <div className="bg-brand-100 dark:bg-brand-900/40 p-2 rounded-lg">
                            <Filter size={20} className="text-brand-600 dark:text-brand-400" />
                        </div>
                        <div className="text-left">
                            <h3 className="font-bold text-gray-900 dark:text-slate-100 text-lg">{language === 'ro' ? 'Filtrează și Sortează' : 'Filter and Sort'}</h3>
                            <p className="text-sm text-gray-500 dark:text-slate-400">
                                {selectedCategory !== 'all' && `${language === 'ro' ? 'Categorie' : 'Category'}: ${categoryLabels[selectedCategory]}`}
                                {selectedCategory !== 'all' && sortBy !== 'default' && ' • '}
                                {sortBy !== 'default' && (language === 'ro' ? 'Sortare activă' : 'Sorting active')}
                                {selectedCategory === 'all' && sortBy === 'default' && (language === 'ro' ? 'Personalizează afișarea' : 'Customize display')}
                            </p>
                        </div>
                    </div>
                    <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-gray-600 dark:text-slate-400 hidden sm:inline">
                            {isFilterOpen ? (language === 'ro' ? 'Ascunde' : 'Hide') : (language === 'ro' ? 'Arată' : 'Show')}
                        </span>
                        {isFilterOpen ? (
                            <ChevronUp size={20} className="text-gray-600 dark:text-slate-400" />
                        ) : (
                            <ChevronDown size={20} className="text-gray-600 dark:text-slate-400" />
                        )}
                    </div>
                </button>

                {/* Conținut collapsible */}
                {isFilterOpen && (
                    <div className="px-6 pb-6 border-t border-gray-100 dark:border-slate-700">
                        {/* Filtrare pe Categorii */}
                        <div className="mb-6 mt-6">
                            <div className="flex items-center gap-2 mb-3">
                                <Filter size={18} className="text-gray-600 dark:text-slate-400" />
                                <h3 className="font-bold text-gray-900 dark:text-slate-100">{language === 'ro' ? 'Filtrează după Categorie' : 'Filter by Category'}</h3>
                            </div>
                            <div className="flex flex-wrap gap-2">
                                <button
                                    onClick={() => setSelectedCategory('all')}
                                    className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                        selectedCategory === 'all'
                                            ? 'bg-brand-600 text-white shadow-lg shadow-brand-500/30'
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                    }`}
                                >
                                    {language === 'ro' ? 'Toate' : 'All'} ({items.length})
                                </button>
                                {Object.entries(categoryLabels).map(([cat, label]) => {
                                    const categoryValue = Number(cat) as MenuCategory
                                    const count = items.filter(item => item.category === categoryValue).length
                                    return (
                                        <button
                                            key={cat}
                                            onClick={() => setSelectedCategory(categoryValue)}
                                            className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                                selectedCategory === categoryValue
                                                    ? 'bg-brand-600 text-white shadow-lg shadow-brand-500/30'
                                                    : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                            }`}
                                        >
                                            {label} ({count})
                                        </button>
                                    )
                                })}
                            </div>
                        </div>

                        {/* Sortare */}
                        <div>
                            <div className="flex items-center gap-2 mb-3">
                                <ArrowUpDown size={18} className="text-gray-600 dark:text-slate-400" />
                                <h3 className="font-bold text-gray-900 dark:text-slate-100">{language === 'ro' ? 'Sortează' : 'Sort'}</h3>
                            </div>
                            <div className="flex flex-wrap gap-2">
                                <button
                                    onClick={() => setSortBy('default')}
                                    className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                        sortBy === 'default'
                                            ? 'bg-gray-900 dark:bg-slate-600 text-white shadow-lg'
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                    }`}
                                >
                                    {language === 'ro' ? 'Implicit' : 'Default'}
                                </button>
                                <button
                                    onClick={() => setSortBy('rating')}
                                    className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                        sortBy === 'rating'
                                            ? 'bg-gray-900 dark:bg-slate-600 text-white shadow-lg'
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                    }`}
                                >
                                    ⭐ {language === 'ro' ? 'Rating' : 'Rating'}
                                </button>
                                <button
                                    onClick={() => setSortBy('price-asc')}
                                    className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                        sortBy === 'price-asc'
                                            ? 'bg-gray-900 dark:bg-slate-600 text-white shadow-lg'
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                    }`}
                                >
                                    💰 {language === 'ro' ? 'Preț Crescator' : 'Price: Low to High'}
                                </button>
                                <button
                                    onClick={() => setSortBy('price-desc')}
                                    className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                        sortBy === 'price-desc'
                                            ? 'bg-gray-900 dark:bg-slate-600 text-white shadow-lg'
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                    }`}
                                >
                                    💰 {language === 'ro' ? 'Preț Descăzător' : 'Price: High to Low'}
                                </button>
                                <button
                                    onClick={() => setSortBy('name')}
                                    className={`px-4 py-2 rounded-xl font-medium text-sm transition-all ${
                                        sortBy === 'name'
                                            ? 'bg-gray-900 dark:bg-slate-600 text-white shadow-lg'
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                    }`}
                                >
                                    🔤 {language === 'ro' ? 'Nume' : 'Name'}
                                </button>
                            </div>
                        </div>

                        {/* Rezultate */}
                        <div className="mt-4 pt-4 border-t border-gray-200 dark:border-slate-700">
                            <p className="text-sm text-gray-600 dark:text-slate-400">
                                {language === 'ro' ? 'Se arată' : 'Showing'} <span className="font-bold text-brand-600 dark:text-brand-400">{filteredAndSortedItems.length}</span> {language === 'ro' ? 'produse' : 'products'}
                                {selectedCategory !== 'all' && (
                                    <span> {language === 'ro' ? 'din categoria' : 'from category'} <span className="font-bold">{categoryLabels[selectedCategory]}</span></span>
                                )}
                            </p>
                        </div>
                    </div>
                )}
            </div>

            {/* Grid cu Produse */}
            {filteredAndSortedItems.length === 0 ? (
                <div className="text-center py-12 bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-gray-100 dark:border-slate-700">
                    <p className="text-gray-500 dark:text-slate-400 text-lg">{language === 'ro' ? 'Nu există produse în această categorie' : 'No products in this category'}</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {filteredAndSortedItems.map(item => (
                <div key={item.id} className="group bg-white dark:bg-slate-800 rounded-2xl shadow-sm hover:shadow-xl transition-all duration-300 border border-gray-100 dark:border-slate-700 overflow-hidden flex flex-col hover:-translate-y-1">
                    {/* Imagine / Placeholder */}
                    <div className="relative h-48 bg-gray-100 dark:bg-slate-700 overflow-hidden flex items-center justify-center">
                        {item.imageUrl ? (
                            <img src={item.imageUrl} alt={item.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
                        ) : (
                            <span className="text-6xl transform group-hover:scale-110 transition-transform">🍽️</span>
                        )}
                        <div className="absolute top-3 right-3 bg-white/95 dark:bg-slate-800/95 backdrop-blur-sm px-3 py-1 rounded-lg text-sm font-bold text-gray-900 dark:text-slate-100 shadow-sm border border-gray-100 dark:border-slate-600">
                            {item.price.toFixed(2)} RON
                        </div>
                        
                        {/* Rating Badge */}
                        {item.averageRating !== null && item.averageRating > 0 && (
                            <div className="absolute bottom-3 right-3 bg-white/95 dark:bg-slate-800/95 backdrop-blur-sm px-2 py-1 rounded-lg shadow-sm border border-gray-100 dark:border-slate-600 flex items-center gap-1">
                                <Star size={14} className="text-yellow-400 fill-yellow-400" />
                                <span className="text-sm font-bold text-gray-900 dark:text-slate-100">
                                    {item.averageRating.toFixed(1)}
                                </span>
                                <span className="text-xs text-gray-500 dark:text-slate-400">
                                    ({item.reviewCount})
                                </span>
                            </div>
                        )}
                    </div>
                    
                    <div className="p-5 flex flex-col flex-1">
                        <h3 className="text-lg font-bold text-gray-900 dark:text-slate-100 mb-1">{item.name}</h3>
                        <p className="text-gray-500 dark:text-slate-400 text-sm line-clamp-2 mb-4 flex-1 min-h-[2.5em]">
                            {item.description || 'Un preparat delicios pregătit proaspăt.'}
                        </p>
                        
                        {item.allergens && item.allergens.length > 0 && (
                            <div className="flex gap-1 flex-wrap mb-4">
                                {item.allergens.map(a => (
                                    <span key={a} className="text-[10px] uppercase font-bold tracking-wider bg-rose-50 dark:bg-rose-900/30 text-rose-600 dark:text-rose-400 px-2 py-1 rounded border border-rose-100 dark:border-rose-800">
                                        {a}
                                    </span>
                                ))}
                            </div>
                        )}

                        {isLoggedIn && (
                            <div className="flex gap-2">
                                <button
                                    onClick={() => onAddToCart(item)}
                                    className="flex-1 flex items-center justify-center gap-2 bg-gray-900 dark:bg-brand-600 hover:bg-brand-600 dark:hover:bg-brand-700 active:bg-brand-700 dark:active:bg-brand-800 text-white py-3 rounded-xl font-bold text-sm transition-all shadow-lg shadow-gray-200 dark:shadow-slate-900/50 hover:shadow-brand-500/30 dark:hover:shadow-brand-500/20"
                                >
                                    <Plus size={18} />
                                    {language === 'ro' ? 'Adaugă în Coș' : 'Add to Cart'}
                                </button>
                                
                                {onViewReviews && (
                                    <button
                                        onClick={() => onViewReviews(item)}
                                        className="flex items-center justify-center gap-1 bg-white dark:bg-slate-700 hover:bg-gray-50 dark:hover:bg-slate-600 border border-gray-200 dark:border-slate-600 text-gray-700 dark:text-slate-200 px-3 py-3 rounded-xl font-medium text-sm transition-all"
                                        title={language === 'ro' ? 'Vezi Recenzii' : 'View Reviews'}
                                    >
                                        <MessageSquare size={18} />
                                    </button>
                                )}
                            </div>
                        )}
                    </div>
                </div>
            ))}
                </div>
            )}
        </div>
    )
}
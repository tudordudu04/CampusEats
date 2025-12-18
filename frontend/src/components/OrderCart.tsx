import { useState, useEffect, useMemo } from 'react'
import type { MenuItem, UserCouponDto } from '../types'
import { PaymentApi, CouponApi } from '../services/api' 
import { ShoppingCart, X, Trash2, Minus, Plus, CreditCard, Tag } from 'lucide-react'

type CartItem = { item: MenuItem; quantity: number }

type Props = {
    cart: CartItem[]
    onClear: () => void
    onUpdateQuantity: (itemId: string, qty: number) => void
}

export default function OrderCart({ cart, onClear, onUpdateQuantity }: Props) {
    const [isOpen, setIsOpen] = useState(false)
    const [loading, setLoading] = useState(false)
    const [myCoupons, setMyCoupons] = useState<UserCouponDto[]>([])
    const [selectedCouponId, setSelectedCouponId] = useState<string | null>(null)
    const [showCoupons, setShowCoupons] = useState(false)

    const subtotal = cart.reduce((sum, c) => sum + c.item.price * c.quantity, 0)
    const itemCount = cart.reduce((acc, i) => acc + i.quantity, 0)
    
    const selectedCoupon = useMemo(() => {
        if (!selectedCouponId) return null
        return myCoupons.find(c => c.id === selectedCouponId)
    }, [selectedCouponId, myCoupons])

    const discountAmount = useMemo(() => {
        if (!selectedCoupon) return 0
        
        // Check minimum order amount
        if (selectedCoupon.minimumOrderAmount && subtotal < selectedCoupon.minimumOrderAmount) {
            return 0
        }

        switch (selectedCoupon.couponType) {
            case 0: // PercentageDiscount
                return subtotal * (selectedCoupon.discountValue / 100)
            case 1: // FixedAmountDiscount
                return Math.min(selectedCoupon.discountValue, subtotal)
            case 2: // FreeItem
                if (selectedCoupon.specificMenuItemId) {
                    const item = cart.find(c => c.item.id === selectedCoupon.specificMenuItemId)
                    return item ? item.item.price : 0
                }
                return cart.length > 0 ? cart[0].item.price : 0
            default:
                return 0
        }
    }, [selectedCoupon, subtotal, cart])

    const total = Math.max(0, subtotal - discountAmount)

    useEffect(() => {
        if (isOpen) {
            loadCoupons()
        }
    }, [isOpen])

    const loadCoupons = async () => {
        try {
            const userCoupons = await CouponApi.getMyCoupons()
            // Filter out used ones (backend should already do this, but double-check)
            const availableCoupons = userCoupons.filter(uc => !uc.isUsed)
            setMyCoupons(availableCoupons)
        } catch (err) {
            console.error('Failed to load coupons', err)
        }
    }

    const handlePlaceOrderDirect = async () => {
        if (cart.length === 0) return
        setLoading(true)
        try {
            const items = cart.map(c => ({ menuItemId: c.item.id, quantity: c.quantity }))
            const { checkoutUrl } = await PaymentApi.createSession(items, "Plata la livrare (Cash)", selectedCouponId)
            onClear()
            setIsOpen(false)
            setSelectedCouponId(null)
            await loadCoupons() // Reload coupons to remove used one
            window.location.href = checkoutUrl
        } catch (err: any) {
            alert('Eroare la plasarea comenzii: ' + err.message)
        } finally {
            setLoading(false)
        }
    }

    if (!isOpen) {
        return (
            <button
                onClick={() => setIsOpen(true)}
                className="fixed bottom-6 right-6 bg-gray-900 hover:bg-brand-600 text-white p-4 rounded-full shadow-2xl hover:shadow-brand-500/40 hover:scale-105 transition-all z-50 flex items-center gap-3 group animate-bounce-in"
            >
                <div className="relative">
                    <ShoppingCart size={24} />
                    {itemCount > 0 && (
                        <span className="absolute -top-2 -right-2 bg-red-500 text-white text-[10px] font-bold w-5 h-5 flex items-center justify-center rounded-full border-2 border-gray-900">
                            {itemCount}
                        </span>
                    )}
                </div>
                <span className="font-bold pr-2 hidden group-hover:inline transition-all">Vezi Coș</span>
            </button>
        )
    }

    return (
        <>
            {/* Overlay fundal */}
            <div className="fixed inset-0 bg-black/30 backdrop-blur-sm z-40 transition-opacity" onClick={() => setIsOpen(false)} />
            
            {/* Cart Panel */}
            <div className="fixed bottom-0 left-0 right-0 w-full max-w-full sm:bottom-6 sm:right-6 sm:left-auto sm:w-full sm:max-w-md bg-white rounded-2xl shadow-2xl border border-gray-100 z-50 flex flex-col max-h-[85vh] animate-slide-up transform transition-all">
                {/* Header */}
                <div className="p-5 border-b border-gray-100 flex justify-between items-center bg-gray-50/80 backdrop-blur rounded-t-2xl">
                    <div className="flex items-center gap-2">
                        <div className="bg-brand-100 p-1.5 rounded text-brand-600">
                            <ShoppingCart size={18} />
                        </div>
                        <h3 className="text-lg font-bold text-gray-900">Comanda Ta</h3>
                    </div>
                    <button onClick={() => setIsOpen(false)} className="text-gray-400 hover:text-gray-600 hover:bg-gray-200 p-1 rounded-full transition-colors">
                        <X size={20} />
                    </button>
                </div>

                {/* Lista Produse */}
                <div className="flex-1 overflow-y-auto p-5 space-y-4">
                    {cart.length === 0 ? (
                        <div className="text-center py-12 text-gray-400 flex flex-col items-center">
                            <div className="bg-gray-50 p-4 rounded-full mb-3">
                                <ShoppingCart size={32} className="opacity-20" />
                            </div>
                            <p className="font-medium">Coșul este gol</p>
                            <p className="text-sm">Adaugă ceva delicios din meniu!</p>
                        </div>
                    ) : (
                        cart.map(c => (
                            <div key={c.item.id} className="flex items-center gap-4 bg-white p-3 border border-gray-100 rounded-xl hover:border-brand-200 transition-colors shadow-sm">
                                <div className="flex-1">
                                    <div className="font-bold text-gray-900 text-sm mb-0.5">{c.item.name}</div>
                                    <div className="text-sm text-brand-600 font-semibold">{c.item.price.toFixed(2)} RON</div>
                                </div>
                                
                                <div className="flex items-center gap-2 bg-gray-50 rounded-lg p-1 border border-gray-200">
                                    <button onClick={() => onUpdateQuantity(c.item.id, c.quantity - 1)} disabled={c.quantity <= 1} className="w-7 h-7 flex items-center justify-center text-gray-600 hover:bg-white hover:shadow rounded-md disabled:opacity-30 transition-all">
                                        <Minus size={14} />
                                    </button>
                                    <span className="font-bold text-gray-800 w-6 text-center text-sm">{c.quantity}</span>
                                    <button onClick={() => onUpdateQuantity(c.item.id, c.quantity + 1)} className="w-7 h-7 flex items-center justify-center text-gray-600 hover:bg-white hover:shadow rounded-md transition-all">
                                        <Plus size={14} />
                                    </button>
                                </div>
                                
                                <button onClick={() => onUpdateQuantity(c.item.id, 0)} className="text-gray-400 hover:text-red-500 hover:bg-red-50 p-2 rounded-lg transition-all">
                                    <Trash2 size={18} />
                                </button>
                            </div>
                        ))
                    )}
                </div>

                {/* Footer / Total */}
                <div className="p-6 border-t border-gray-100 bg-gray-50 rounded-b-2xl">
                    {/* Coupon Selection */}
                    {myCoupons.length > 0 && (
                        <div className="mb-4">
                            <button
                                onClick={() => setShowCoupons(!showCoupons)}
                                className="w-full flex items-center justify-between p-3 bg-white border border-gray-200 rounded-lg hover:border-green-400 transition-colors"
                            >
                                <div className="flex items-center gap-2">
                                    <Tag size={18} className="text-green-600" />
                                    <span className="font-medium text-gray-700">
                                        {selectedCouponId ? 'Cupon aplicat' : 'Aplică un cupon'}
                                    </span>
                                </div>
                                <span className="text-xs text-gray-500">{myCoupons.length} disponibile</span>
                            </button>

                            {showCoupons && (
                                <div className="mt-2 space-y-2 max-h-40 overflow-y-auto">
                                    <button
                                        onClick={() => setSelectedCouponId(null)}
                                        className={`w-full text-left p-2 rounded border ${!selectedCouponId ? 'border-green-500 bg-green-50' : 'border-gray-200 hover:border-gray-300'}`}
                                    >
                                        <span className="text-sm text-gray-600">Fără cupon</span>
                                    </button>
                                    {myCoupons.map(coupon => (
                                        <button
                                            key={coupon.id}
                                            onClick={() => {
                                                setSelectedCouponId(coupon.id)
                                                setShowCoupons(false)
                                            }}
                                            className={`w-full text-left p-2 rounded border ${selectedCouponId === coupon.id ? 'border-green-500 bg-green-50' : 'border-gray-200 hover:border-gray-300'}`}
                                        >
                                            <div className="font-medium text-sm text-gray-900">{coupon.couponName}</div>
                                            <div className="text-xs text-gray-500">{coupon.couponDescription}</div>
                                        </button>
                                    ))}
                                </div>
                            )}
                        </div>
                    )}

                    {/* Subtotal și Discount */}
                    <div className="space-y-2 mb-4">
                        <div className="flex justify-between items-center text-sm">
                            <span className="text-gray-600">Subtotal</span>
                            <span className="font-medium text-gray-900">{subtotal.toFixed(2)} RON</span>
                        </div>
                        
                        {discountAmount > 0 && (
                            <div className="flex justify-between items-center text-sm">
                                <span className="text-green-600 font-medium">Discount ({selectedCoupon?.couponName})</span>
                                <span className="font-medium text-green-600">-{discountAmount.toFixed(2)} RON</span>
                            </div>
                        )}
                        
                        {selectedCoupon?.minimumOrderAmount && subtotal < selectedCoupon.minimumOrderAmount && (
                            <div className="text-xs text-red-500 bg-red-50 p-2 rounded">
                                Comandă minimă: {selectedCoupon.minimumOrderAmount.toFixed(2)} RON
                            </div>
                        )}
                    </div>

                    <div className="flex justify-between items-center mb-5 pt-3 border-t border-gray-200">
                        <span className="text-gray-500 font-medium">Total de plată</span>
                        <span className="text-3xl font-extrabold text-gray-900 tracking-tight">{total.toFixed(2)} <span className="text-base font-normal text-gray-500">RON</span></span>
                    </div>
                    
                    <button
                        onClick={handlePlaceOrderDirect}
                        disabled={cart.length === 0 || loading}
                        className="w-full py-4 bg-gray-900 hover:bg-brand-600 text-white rounded-xl font-bold shadow-xl shadow-brand-500/20 disabled:opacity-50 disabled:shadow-none transition-all flex justify-center items-center gap-2 transform active:scale-[0.98]"
                    >
                        {loading ? 'Se procesează...' : (
                            <>
                                <CreditCard size={20} />
                                Plasează Comanda (Cash)
                            </>
                        )}
                    </button>
                </div>
            </div>
        </>
    )
}
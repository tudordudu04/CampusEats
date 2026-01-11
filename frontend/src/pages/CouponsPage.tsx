import { useState, useEffect } from 'react'
import { CouponApi } from '../services/api'
import { CouponDto, UserCouponDto, CouponType } from '../types'
import { useLoyaltyPoints } from '../hooks/useLoyaltyPoints'
import { useLanguage } from '../contexts/LanguageContext'

export function CouponsPage() {
    const [availableCoupons, setAvailableCoupons] = useState<CouponDto[]>([])
    const [myCoupons, setMyCoupons] = useState<UserCouponDto[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)
    const [purchaseMessage, setPurchaseMessage] = useState<string | null>(null)
    const { points: loyaltyPoints, refresh: refetchPoints } = useLoyaltyPoints()
    const points = loyaltyPoints ?? 0
    const { language } = useLanguage()

    useEffect(() => {
        loadData()
    }, [])

    const loadData = async () => {
        try {
            setLoading(true)
            setError(null)
            const [available, my] = await Promise.all([
                CouponApi.getAvailable(),
                CouponApi.getMyCoupons()
            ])
            setAvailableCoupons(available)
            setMyCoupons(my)
        } catch (err: any) {
            setError(err.message || 'Failed to load coupons')
        } finally {
            setLoading(false)
        }
    }

    const handlePurchase = async (couponId: string, pointsCost: number) => {
        if (points < pointsCost) {
            setPurchaseMessage(language === 'ro' ? 'Puncte insuficiente!' : 'Insufficient points!')
            setTimeout(() => setPurchaseMessage(null), 3000)
            return
        }

        try {
            const result = await CouponApi.purchase(couponId)
            if (result.success) {
                // Reload both coupons and points
                await Promise.all([
                    loadData(),
                    refetchPoints()
                ])
                // Emit event to update points in navbar
                window.dispatchEvent(new Event('loyalty:refresh'))
            } else {
                setPurchaseMessage(result.message)
                setTimeout(() => setPurchaseMessage(null), 3000)
            }
        } catch (err: any) {
            setPurchaseMessage(err.message || (language === 'ro' ? 'Eroare la achiziționarea cuponului' : 'Error purchasing coupon'))
            setTimeout(() => setPurchaseMessage(null), 3000)
        }
    }

    const getCouponTypeLabel = (type: CouponType) => {
        switch (type) {
            case CouponType.PercentageDiscount: return language === 'ro' ? 'Discount Procentual' : 'Percentage Discount'
            case CouponType.FixedAmountDiscount: return language === 'ro' ? 'Discount Fix' : 'Fixed Discount'
            case CouponType.FreeItem: return language === 'ro' ? 'Produs Gratuit' : 'Free Item'
            default: return 'Unknown'
        }
    }

    const formatDiscount = (coupon: CouponDto) => {
        switch (coupon.type) {
            case CouponType.PercentageDiscount:
                return `${coupon.discountValue}% ${language === 'ro' ? 'discount' : 'discount'}`
            case CouponType.FixedAmountDiscount:
                return `${coupon.discountValue} ${language === 'ro' ? 'lei discount' : 'RON discount'}`
            case CouponType.FreeItem:
                return coupon.specificMenuItemName ? `${coupon.specificMenuItemName} ${language === 'ro' ? 'gratuit' : 'free'}` : (language === 'ro' ? 'Produs gratuit' : 'Free product')
            default:
                return ''
        }
    }

    if (loading) return <div className="p-8 text-center dark:text-slate-300">{language === 'ro' ? 'Se încarcă...' : 'Loading...'}</div>
    if (error) return <div className="p-8 text-center text-red-500 dark:text-red-400">{error}</div>

    return (
        <div className="min-h-screen p-8">
            <div className="max-w-7xl mx-auto">
                <div className="mb-8">
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-slate-100">{language === 'ro' ? 'Cupoane' : 'Coupons'}</h1>
                    <p className="text-gray-600 dark:text-slate-300 mt-2">
                        {language === 'ro' ? 'Puncte disponibile' : 'Available Points'}: <span className="font-bold text-brand-600 dark:text-brand-400">{points}</span>
                    </p>
                </div>

                {purchaseMessage && (
                    <div className={`mb-6 p-4 rounded-lg ${purchaseMessage.includes('succes') ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300' : 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300'}`}>
                        {purchaseMessage}
                    </div>
                )}

                {/* My Coupons Section */}
                <div className="mb-12">
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-slate-100 mb-4">{language === 'ro' ? 'Cupoanele Mele' : 'My Coupons'}</h2>
                    {myCoupons.length === 0 ? (
                        <p className="text-gray-500 dark:text-slate-400 italic">{language === 'ro' ? 'Nu ai cupoane active.' : 'You have no active coupons.'}</p>
                    ) : (
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            {myCoupons.map(uc => (
                                <div key={uc.id} className="bg-gradient-to-br from-gray-900 to-gray-800 dark:from-slate-800 dark:to-slate-900 text-white rounded-xl shadow-xl border border-gray-700 dark:border-slate-600 p-6 hover:shadow-2xl transition-all">
                                    <div className="flex items-start justify-between mb-2">
                                        <h3 className="text-xl font-bold">{uc.couponName}</h3>
                                        <span className="bg-brand-500 text-white text-xs font-semibold px-3 py-1 rounded-full">ACTIV</span>
                                    </div>
                                    <p className="text-gray-300 text-sm mb-4">{uc.couponDescription}</p>
                                    <div className="text-xs text-gray-400 space-y-1">
                                        <p>Cumpărat: {new Date(uc.acquiredAtUtc).toLocaleDateString('ro-RO')}</p>
                                        {uc.expiresAtUtc && (
                                            <p>Expiră: {new Date(uc.expiresAtUtc).toLocaleDateString('ro-RO')}</p>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                {/* Available Coupons Section */}
                <div>
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-slate-100 mb-4">{language === 'ro' ? 'Cupoane Disponibile' : 'Available Coupons'}</h2>
                    {availableCoupons.length === 0 ? (
                        <p className="text-gray-500 dark:text-slate-400 italic">{language === 'ro' ? 'Nu sunt cupoane disponibile momentan.' : 'No coupons available at the moment.'}</p>
                    ) : (
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            {availableCoupons.map(coupon => (
                                <div key={coupon.id} className="bg-white dark:bg-slate-800 rounded-xl shadow-lg hover:shadow-2xl transition-all p-6 border-2 border-gray-200 dark:border-slate-700 hover:border-brand-400 dark:hover:border-brand-500">
                                    <div className="mb-4">
                                        <div className="flex items-center justify-between mb-2">
                                            <h3 className="text-xl font-bold text-gray-900 dark:text-slate-100">{coupon.name}</h3>
                                            <span className="bg-brand-100 text-brand-700 text-xs font-semibold px-3 py-1 rounded-full">
                                                {getCouponTypeLabel(coupon.type)}
                                            </span>
                                        </div>
                                        <p className="text-gray-600 dark:text-slate-300 text-sm mb-3">{coupon.description}</p>
                                        <div className="bg-gradient-to-r from-brand-50 to-brand-100 dark:from-brand-900/30 dark:to-brand-800/30 border-l-4 border-brand-500 dark:border-brand-400 p-3 mb-3 rounded-r-lg">
                                            <p className="text-brand-800 dark:text-brand-300 font-bold text-sm">{formatDiscount(coupon)}</p>
                                        </div>
                                        {coupon.minimumOrderAmount && (
                                            <p className="text-xs text-gray-500 dark:text-slate-400">📦 Comandă minimă: {coupon.minimumOrderAmount} lei</p>
                                        )}
                                        {coupon.expiresAtUtc && (
                                            <p className="text-xs text-gray-500 dark:text-slate-400 mt-1">
                                                ⏰ Valabil până: {new Date(coupon.expiresAtUtc).toLocaleDateString('ro-RO')}
                                            </p>
                                        )}
                                    </div>
                                    <div className="flex items-center justify-between pt-4 border-t border-gray-200 dark:border-slate-700">
                                        <div className="flex items-center gap-1">
                                            <span className="text-2xl font-bold text-brand-600 dark:text-brand-400">{coupon.pointsCost}</span>
                                            <span className="text-sm text-gray-500 dark:text-slate-400">puncte</span>
                                        </div>
                                        <button
                                            onClick={() => handlePurchase(coupon.id, coupon.pointsCost)}
                                            disabled={points < coupon.pointsCost}
                                            className={`px-5 py-2.5 rounded-xl font-bold transition-all shadow-md ${
                                                points < coupon.pointsCost
                                                    ? 'bg-gray-300 dark:bg-slate-700 text-gray-500 dark:text-slate-500 cursor-not-allowed'
                                                    : 'bg-gray-900 dark:bg-brand-600 text-white hover:bg-brand-600 dark:hover:bg-brand-700 shadow-brand-500/20 hover:scale-105 active:scale-95'
                                            }`}
                                        >
                                            {points < coupon.pointsCost ? (language === 'ro' ? 'Insuficient' : 'Insufficient') : (language === 'ro' ? 'Cumpără' : 'Buy')}
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}

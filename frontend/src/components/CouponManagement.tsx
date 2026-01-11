import { useState, useEffect } from 'react'
import { CouponApi, MenuApi } from '../services/api'
import { CreateCouponRequest, CouponType, MenuItem, CouponDto } from '../types'
import { useLanguage } from '../contexts/LanguageContext'

export function CouponManagement() {
    const [menuItems, setMenuItems] = useState<MenuItem[]>([])
    const [existingCoupons, setExistingCoupons] = useState<CouponDto[]>([])
    const [formData, setFormData] = useState<CreateCouponRequest>({
        name: '',
        description: '',
        type: CouponType.PercentageDiscount,
        discountValue: 0,
        pointsCost: 0,
        specificMenuItemId: null,
        minimumOrderAmount: null,
        expiresAtUtc: null
    })
    const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null)
    const [loading, setLoading] = useState(false)
    const { language } = useLanguage()

    useEffect(() => {
        loadMenuItems()
        loadCoupons()
    }, [])

    const loadMenuItems = async () => {
        try {
            const items = await MenuApi.list()
            setMenuItems(items)
        } catch (err) {
            console.error('Failed to load menu items', err)
        }
    }

    const loadCoupons = async () => {
        try {
            const coupons = await CouponApi.getAvailable()
            setExistingCoupons(coupons)
        } catch (err) {
            console.error('Failed to load coupons', err)
        }
    }

    const handleDelete = async (couponId: string) => {
        if (!confirm(language === 'ro' ? 'Ești sigur că vrei să ștergi acest cupon?' : 'Are you sure you want to delete this coupon?')) {
            return
        }

        try {
            const result = await CouponApi.delete(couponId)
            if (result.success) {
                loadCoupons() // Reload the list
            } else {
                setMessage({ type: 'error', text: result.message })
                setTimeout(() => setMessage(null), 5000)
            }
        } catch (err: any) {
            setMessage({ type: 'error', text: err.message || (language === 'ro' ? 'Eroare la ștergerea cuponului' : 'Error deleting coupon') })
            setTimeout(() => setMessage(null), 5000)
        }
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setLoading(true)
        setMessage(null)

        try {
            const result = await CouponApi.create(formData)
            if (result.success) {
                setMessage({ type: 'success', text: language === 'ro' ? 'Cupon creat cu succes!' : 'Coupon created successfully!' })
                setFormData({
                    name: '',
                    description: '',
                    type: CouponType.PercentageDiscount,
                    discountValue: 0,
                    pointsCost: 0,
                    specificMenuItemId: null,
                    minimumOrderAmount: null,
                    expiresAtUtc: null
                })
                loadCoupons() // Reload the list
            } else {
                setMessage({ type: 'error', text: result.message })
            }
        } catch (err: any) {
            setMessage({ type: 'error', text: err.message || (language === 'ro' ? 'Eroare la crearea cuponului' : 'Error creating coupon') })
        } finally {
            setLoading(false)
            setTimeout(() => setMessage(null), 5000)
        }
    }

    const getCouponTypeLabel = (type: CouponType) => {
        if (language === 'ro') {
            switch (type) {
                case CouponType.PercentageDiscount:
                    return 'Reducere Procentuală'
                case CouponType.FixedAmountDiscount:
                    return 'Reducere Fixă'
                case CouponType.FreeItem:
                    return 'Produs Gratuit'
                default:
                    return 'Unknown'
            }
        } else {
            switch (type) {
                case CouponType.PercentageDiscount:
                    return 'Percentage Discount'
                case CouponType.FixedAmountDiscount:
                    return 'Fixed Discount'
                case CouponType.FreeItem:
                    return 'Free Item'
                default:
                    return 'Unknown'
            }
        }
    }

    return (
        <div className="space-y-6">
            {/* Form creare cupon nou */}
            <div className="bg-white dark:bg-slate-800 rounded-lg shadow-md p-6 border border-gray-100 dark:border-slate-700">
                <h2 className="text-2xl font-bold mb-6 dark:text-slate-100">{language === 'ro' ? 'Creare Cupon Nou' : 'Create New Coupon'}</h2>

                {message && (
                    <div className={`mb-4 p-4 rounded-lg ${
                        message.type === 'success' 
                            ? 'bg-green-100 dark:bg-green-950/50 text-green-800 dark:text-green-400' 
                            : 'bg-red-100 dark:bg-red-950/50 text-red-800 dark:text-red-400'
                    }`}>
                        {message.text}
                    </div>
                )}

            <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Nume Cupon' : 'Coupon Name'}</label>
                    <input
                        type="text"
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                        placeholder={language === 'ro' ? 'ex: Weekend Special 20%' : 'e.g.: Weekend Special 20%'}
                        required
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Descriere' : 'Description'}</label>
                    <textarea
                        value={formData.description}
                        onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                        rows={3}
                        placeholder={language === 'ro' ? 'ex: Primești 20% reducere la orice comandă în acest weekend!' : 'e.g.: Get 20% off any order this weekend!'}
                        required
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Tip Cupon' : 'Coupon Type'}</label>
                    <select
                        value={formData.type}
                        onChange={(e) => {
                            const newType = Number(e.target.value) as CouponType
                            setFormData({ 
                                ...formData, 
                                type: newType,
                                discountValue: newType === CouponType.FreeItem ? 0 : formData.discountValue
                            })
                        }}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                    >
                        <option value={CouponType.PercentageDiscount}>{language === 'ro' ? 'Reducere Procentuală (%) - se aplică la toată comanda' : 'Percentage Discount (%) - applies to entire order'}</option>
                        <option value={CouponType.FixedAmountDiscount}>{language === 'ro' ? 'Reducere Fixă (lei) - se scade din totalul comenzii' : 'Fixed Discount - deducted from order total'}</option>
                        <option value={CouponType.FreeItem}>{language === 'ro' ? 'Produs Gratuit - un produs devine gratuit' : 'Free Item - one product becomes free'}</option>
                    </select>
                </div>

                {formData.type !== CouponType.FreeItem && (
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">
                            {formData.type === CouponType.PercentageDiscount 
                                ? (language === 'ro' ? 'Procent Reducere (%)' : 'Discount Percentage (%)')
                                : (language === 'ro' ? 'Valoare Reducere (lei)' : 'Discount Amount')
                            }
                        </label>
                        <input
                            type="number"
                            value={formData.discountValue || ''}
                            onChange={(e) => setFormData({ ...formData, discountValue: e.target.value ? Number(e.target.value) : 0 })}
                            className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                            min="0.01"
                            step="0.01"
                            placeholder={formData.type === CouponType.PercentageDiscount ? (language === 'ro' ? 'ex: 20' : 'e.g.: 20') : (language === 'ro' ? 'ex: 10.00' : 'e.g.: 10.00')}
                            required
                        />
                    </div>
                )}

                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Cost în Puncte' : 'Points Cost'}</label>
                    <input
                        type="number"
                        value={formData.pointsCost || ''}
                        onChange={(e) => setFormData({ ...formData, pointsCost: e.target.value ? Number(e.target.value) : 0 })}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                        min="1"
                        placeholder={language === 'ro' ? 'ex: 100' : 'e.g.: 100'}
                        required
                    />
                </div>

                {formData.type === CouponType.FreeItem && (
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Produs Specific (Opțional)' : 'Specific Product (Optional)'}</label>
                        <select
                            value={formData.specificMenuItemId || ''}
                            onChange={(e) => setFormData({ ...formData, specificMenuItemId: e.target.value || null })}
                            className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                        >
                            <option value="">{language === 'ro' ? '-- Selectează Produs --' : '-- Select Product --'}</option>
                            {menuItems.map(item => (
                                <option key={item.id} value={item.id}>{item.name} - {item.price} lei</option>
                            ))}
                        </select>
                    </div>
                )}

                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Valoare Minimă Comandă (lei) - Opțional' : 'Minimum Order Amount - Optional'}</label>
                    <input
                        type="number"
                        value={formData.minimumOrderAmount || ''}
                        onChange={(e) => setFormData({ ...formData, minimumOrderAmount: e.target.value ? Number(e.target.value) : null })}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                        min="0"
                        step="0.01"
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">{language === 'ro' ? 'Data Expirare (Opțional)' : 'Expiration Date (Optional)'}</label>
                    <input
                        type="datetime-local"
                        value={formData.expiresAtUtc ? new Date(formData.expiresAtUtc).toISOString().slice(0, 16) : ''}
                        onChange={(e) => setFormData({ ...formData, expiresAtUtc: e.target.value ? new Date(e.target.value).toISOString() : null })}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                    />
                </div>

                <button
                    type="submit"
                    disabled={loading}
                    className="w-full bg-brand-600 dark:bg-brand-700 text-white py-3 rounded-lg font-semibold hover:bg-brand-700 dark:hover:bg-brand-600 disabled:bg-gray-400 dark:disabled:bg-slate-600 disabled:cursor-not-allowed transition-colors"
                >
                    {loading ? (language === 'ro' ? 'Se creează...' : 'Creating...') : (language === 'ro' ? 'Creare Cupon' : 'Create Coupon')}
                </button>
            </form>
        </div>

            {/* Lista cupoane existente */}
            <div className="bg-white dark:bg-slate-800 rounded-lg shadow-md p-6 border border-gray-100 dark:border-slate-700">
                <h2 className="text-2xl font-bold mb-4 dark:text-slate-100">{language === 'ro' ? 'Cupoane Existente' : 'Existing Coupons'}</h2>
                {existingCoupons.length === 0 ? (
                    <p className="text-gray-500 dark:text-slate-400">{language === 'ro' ? 'Nu există cupoane create încă.' : 'No coupons created yet.'}</p>
                ) : (
                    <div className="space-y-3">
                        {existingCoupons.map(coupon => (
                            <div key={coupon.id} className="border border-gray-200 dark:border-slate-700 rounded-lg p-4 flex justify-between items-start bg-white dark:bg-slate-900/50">
                                <div className="flex-1">
                                    <h3 className="font-semibold text-lg dark:text-slate-100">{coupon.name}</h3>
                                    <p className="text-gray-600 dark:text-slate-400 text-sm mt-1">{coupon.description}</p>
                                    <div className="flex gap-4 mt-2 text-sm">
                                        <span className="text-gray-700 dark:text-slate-300">
                                            <strong>{language === 'ro' ? 'Tip:' : 'Type:'}</strong> {getCouponTypeLabel(coupon.type)}
                                        </span>
                                        {coupon.discountValue > 0 && (
                                            <span className="text-gray-700 dark:text-slate-300">
                                                <strong>{language === 'ro' ? 'Valoare:' : 'Value:'}</strong> {coupon.type === CouponType.PercentageDiscount ? `${coupon.discountValue}%` : `${coupon.discountValue} lei`}
                                            </span>
                                        )}
                                        <span className="text-gray-700 dark:text-slate-300">
                                            <strong>{language === 'ro' ? 'Cost:' : 'Cost:'}</strong> {coupon.pointsCost} {language === 'ro' ? 'puncte' : 'points'}
                                        </span>
                                        {coupon.minimumOrderAmount && (
                                            <span className="text-gray-700 dark:text-slate-300">
                                                <strong>{language === 'ro' ? 'Comandă minimă:' : 'Minimum order:'}</strong> {coupon.minimumOrderAmount} lei
                                            </span>
                                        )}
                                    </div>
                                    {coupon.expiresAtUtc && (
                                        <p className="text-sm text-gray-500 dark:text-slate-400 mt-1">
                                            {language === 'ro' ? 'Expiră:' : 'Expires:'} {new Date(coupon.expiresAtUtc).toLocaleDateString(language === 'ro' ? 'ro-RO' : 'en-US')}
                                        </p>
                                    )}
                                    <span className={`inline-block mt-2 px-2 py-1 rounded text-xs ${
                                        coupon.isActive 
                                            ? 'bg-green-100 dark:bg-green-950/50 text-green-800 dark:text-green-400' 
                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-800 dark:text-slate-300'
                                    }`}>
                                        {coupon.isActive ? (language === 'ro' ? 'Activ' : 'Active') : (language === 'ro' ? 'Inactiv' : 'Inactive')}
                                    </span>
                                </div>
                                <button
                                    onClick={() => handleDelete(coupon.id)}
                                    className="ml-4 px-4 py-2 bg-red-600 dark:bg-red-700 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
                                >
                                    {language === 'ro' ? 'Șterge' : 'Delete'}
                                </button>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    )
}

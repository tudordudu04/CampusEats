import { useEffect, useState } from 'react'
import { OrderApi } from '../services/api'
import { OrderDto, OrderStatus } from '../types'
import { Clock, CheckCircle2, XCircle, Package, Utensils } from 'lucide-react'
import { useLanguage } from '../contexts/LanguageContext'

export default function OrdersPage() {
    const [orders, setOrders] = useState<OrderDto[]>([])
    const [loading, setLoading] = useState(true)
    const { language } = useLanguage()

    useEffect(() => {
        loadOrders()
    }, [])

    const loadOrders = async () => {
        try {
            const data = await OrderApi.getAll()
            setOrders(data)
        } finally {
            setLoading(false)
        }
    }

    const handleCancel = async (id: string) => {
        if (!confirm(language === 'ro' ? 'Sigur dorești să anulezi această comandă?' : 'Are you sure you want to cancel this order?')) return
        try {
            await OrderApi.cancel(id)
            window.dispatchEvent(new CustomEvent('loyalty:refresh'))
            await loadOrders()
        } catch (err) {
            alert(language === 'ro' ? 'Eroare la anularea comenzii' : 'Error canceling order')
        }
    }

    const getStatusStyle = (status: OrderStatus) => {
        switch(status) {
            case OrderStatus.Pending: return { bg: 'bg-amber-100', text: 'text-amber-700', icon: Clock, label: language === 'ro' ? 'În Așteptare' : 'Pending' }
            case OrderStatus.Preparing: return { bg: 'bg-blue-100', text: 'text-blue-700', icon: Package, label: language === 'ro' ? 'În Pregătire' : 'Preparing' }
            case OrderStatus.Completed: return { bg: 'bg-green-100', text: 'text-green-700', icon: CheckCircle2, label: language === 'ro' ? 'Finalizată' : 'Completed' }
            case OrderStatus.Cancelled: return { bg: 'bg-red-100', text: 'text-red-700', icon: XCircle, label: language === 'ro' ? 'Anulată' : 'Cancelled' }
            default: return { bg: 'bg-gray-100', text: 'text-gray-700', icon: Clock, label: 'Unknown' }
        }
    }

    if (loading) return <div className="text-center py-20 text-gray-500 dark:text-slate-400">{language === 'ro' ? 'Se încarcă...' : 'Loading...'}</div>

    return (
        <div className="max-w-4xl mx-auto">
            <h2 className="text-2xl font-bold mb-6 text-gray-900 dark:text-slate-100 flex items-center gap-2">
                <Utensils className="text-brand-600 dark:text-brand-400" /> {language === 'ro' ? 'Comenzile Mele' : 'My Orders'}
            </h2>
            {orders.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-800 rounded-2xl border border-dashed border-gray-300 dark:border-slate-600">
                    <Package size={48} className="mx-auto text-gray-300 dark:text-slate-600 mb-3" />
                    <p className="text-gray-500 dark:text-slate-400 text-lg">{language === 'ro' ? 'Nu ai comenzi încă' : 'No orders yet'}</p>
                </div>
            ) : (
                <div className="space-y-5">
                    {orders.map(order => {
                        const statusStyle = getStatusStyle(order.status)
                        const StatusIcon = statusStyle.icon
                        
                        return (
                            <div key={order.id} className="bg-white dark:bg-slate-800 border border-gray-100 dark:border-slate-700 rounded-2xl p-6 shadow-sm hover:shadow-lg transition-all duration-300">
                                <div className="flex flex-wrap justify-between items-start mb-5 gap-4">
                                    <div>
                                        <div className="flex items-center gap-3 mb-2">
                                            <span className="text-lg font-extrabold text-gray-900 dark:text-slate-100 font-mono">#{order.id.slice(0, 8)}</span>
                                            <span className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wide flex items-center gap-1.5 ${statusStyle.bg} ${statusStyle.text}`}>
                                                <StatusIcon size={14} />
                                                {statusStyle.label}
                                            </span>
                                        </div>
                                        <p className="text-sm text-gray-500 dark:text-slate-400 flex items-center gap-1">
                                            <Clock size={14} />
                                            {new Date(order.createdAtUtc).toLocaleString('ro-RO')}
                                        </p>
                                    </div>
                                    <div className="text-right">
                                        <span className="block text-2xl font-extrabold text-brand-600 dark:text-brand-400">{order.total.toFixed(2)} <span className="text-sm font-medium text-gray-500 dark:text-slate-400">RON</span></span>
                                    </div>
                                </div>

                                <div className="bg-gray-50 dark:bg-slate-700/50 rounded-xl p-4 mb-4 border border-gray-100 dark:border-slate-600">
                                    <ul className="space-y-3">
                                        {order.items.map(item => (
                                            <li key={item.id} className="flex justify-between text-sm items-center">
                                                <span className="text-gray-700 dark:text-slate-200 font-medium">
                                                    <span className="bg-white dark:bg-slate-600 border border-gray-200 dark:border-slate-500 px-2 py-0.5 rounded text-xs font-bold text-gray-900 dark:text-slate-100 mr-2 shadow-sm">
                                                        {item.quantity}x
                                                    </span> 
                                                    {item.menuItemName || 'Produs'}
                                                </span>
                                                <span className="text-gray-500 dark:text-slate-400 font-mono">{item.unitPrice} RON</span>
                                            </li>
                                        ))}
                                    </ul>
                                </div>

                                {order.status === OrderStatus.Pending && (
                                    <div className="flex justify-end pt-3 border-t border-gray-100 dark:border-slate-700">
                                        <button 
                                            onClick={() => handleCancel(order.id)} 
                                            className="text-sm text-red-600 dark:text-red-400 hover:text-white hover:bg-red-600 dark:hover:bg-red-700 px-4 py-2 rounded-lg font-medium transition-all border border-red-100 dark:border-red-800 hover:border-red-600 dark:hover:border-red-700"
                                        >
                                            {language === 'ro' ? 'Anulează Comanda' : 'Cancel Order'}
                                        </button>
                                    </div>
                                )}
                            </div>
                        )
                    })}
                </div>
            )}
        </div>
    )
}
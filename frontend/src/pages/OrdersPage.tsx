import { useEffect, useState } from 'react'
import { OrderApi } from '../services/api'
import { OrderDto, OrderStatus } from '../types'
import { Clock, CheckCircle2, XCircle, Package, Utensils } from 'lucide-react'

export default function OrdersPage() {
    const [orders, setOrders] = useState<OrderDto[]>([])
    const [loading, setLoading] = useState(true)

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
        if (!confirm('Sigur dorești să anulezi comanda?')) return
        try {
            await OrderApi.cancel(id)
            window.dispatchEvent(new CustomEvent('loyalty:refresh'))
            await loadOrders()
        } catch (err) {
            alert('Nu s-a putut anula comanda.')
        }
    }

    const getStatusStyle = (status: OrderStatus) => {
        switch(status) {
            case OrderStatus.Pending: return { bg: 'bg-amber-100', text: 'text-amber-700', icon: Clock, label: 'În așteptare' }
            case OrderStatus.Preparing: return { bg: 'bg-blue-100', text: 'text-blue-700', icon: Package, label: 'Se prepară' }
            case OrderStatus.Completed: return { bg: 'bg-green-100', text: 'text-green-700', icon: CheckCircle2, label: 'Finalizată' }
            case OrderStatus.Cancelled: return { bg: 'bg-red-100', text: 'text-red-700', icon: XCircle, label: 'Anulată' }
            default: return { bg: 'bg-gray-100', text: 'text-gray-700', icon: Clock, label: 'Unknown' }
        }
    }

    if (loading) return <div className="text-center py-20 text-gray-500">Se încarcă comenzile...</div>

    return (
        <div className="max-w-4xl mx-auto">
            <h2 className="text-2xl font-bold mb-6 text-gray-900 flex items-center gap-2">
                <Utensils className="text-brand-600" /> Comenzile Mele
            </h2>
            {orders.length === 0 ? (
                <div className="text-center py-16 bg-white rounded-2xl border border-dashed border-gray-300">
                    <Package size={48} className="mx-auto text-gray-300 mb-3" />
                    <p className="text-gray-500 text-lg">Nu ai plasat nicio comandă încă.</p>
                </div>
            ) : (
                <div className="space-y-5">
                    {orders.map(order => {
                        const statusStyle = getStatusStyle(order.status)
                        const StatusIcon = statusStyle.icon
                        
                        return (
                            <div key={order.id} className="bg-white border border-gray-100 rounded-2xl p-6 shadow-sm hover:shadow-lg transition-all duration-300">
                                <div className="flex flex-wrap justify-between items-start mb-5 gap-4">
                                    <div>
                                        <div className="flex items-center gap-3 mb-2">
                                            <span className="text-lg font-extrabold text-gray-900 font-mono">#{order.id.slice(0, 8)}</span>
                                            <span className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wide flex items-center gap-1.5 ${statusStyle.bg} ${statusStyle.text}`}>
                                                <StatusIcon size={14} />
                                                {statusStyle.label}
                                            </span>
                                        </div>
                                        <p className="text-sm text-gray-500 flex items-center gap-1">
                                            <Clock size={14} />
                                            {new Date(order.createdAtUtc).toLocaleString('ro-RO')}
                                        </p>
                                    </div>
                                    <div className="text-right">
                                        <span className="block text-2xl font-extrabold text-brand-600">{order.total.toFixed(2)} <span className="text-sm font-medium text-gray-500">RON</span></span>
                                    </div>
                                </div>

                                <div className="bg-gray-50 rounded-xl p-4 mb-4 border border-gray-100">
                                    <ul className="space-y-3">
                                        {order.items.map(item => (
                                            <li key={item.id} className="flex justify-between text-sm items-center">
                                                <span className="text-gray-700 font-medium">
                                                    <span className="bg-white border border-gray-200 px-2 py-0.5 rounded text-xs font-bold text-gray-900 mr-2 shadow-sm">
                                                        {item.quantity}x
                                                    </span> 
                                                    {item.menuItemName || 'Produs'}
                                                </span>
                                                <span className="text-gray-500 font-mono">{item.unitPrice} RON</span>
                                            </li>
                                        ))}
                                    </ul>
                                </div>

                                {order.status === OrderStatus.Pending && (
                                    <div className="flex justify-end pt-3 border-t border-gray-100">
                                        <button 
                                            onClick={() => handleCancel(order.id)} 
                                            className="text-sm text-red-600 hover:text-white hover:bg-red-600 px-4 py-2 rounded-lg font-medium transition-all border border-red-100 hover:border-red-600"
                                        >
                                            Anulează Comanda
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
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { OrderApi } from '../services/api'
import { OrderDto } from '../types'
import { ArrowLeft, User, Clock, Receipt, Utensils } from 'lucide-react'

export default function KitchenOrderDetails() {
    const { id } = useParams<{ id: string }>()
    const navigate = useNavigate()
    const [order, setOrder] = useState<OrderDto | null>(null)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        if (!id) return;
        
        OrderApi.getById(id)
            .then(data => setOrder(data))
            .catch(err => alert("Nu s-a putut încărca comanda: " + err.message))
            .finally(() => setLoading(false))
    }, [id])

    if (loading) return <div className="p-10 text-center">Se încarcă detaliile...</div>
    if (!order) return <div className="p-10 text-center text-red-500">Comanda nu a fost găsită.</div>

    return (
        <div className="max-w-3xl mx-auto py-8 px-4">
            <button 
                onClick={() => navigate('/kitchen')} 
                className="flex items-center gap-2 text-gray-500 hover:text-gray-900 mb-6 transition-colors"
            >
                <ArrowLeft size={20} /> Înanpoi la Bucătărie
            </button>

            <div className="bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
                {/* Header */}
                <div className="bg-gray-50 p-6 border-b border-gray-100 flex justify-between items-start">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
                            <Receipt className="text-brand-600" />
                            Comanda #{order.id.slice(0, 8)}
                        </h1>
                        <p className="text-gray-500 mt-1 flex items-center gap-2">
                            <Clock size={16} /> 
                            {new Date(order.createdAtUtc).toLocaleString('ro-RO')}
                        </p>
                    </div>
                    <div className="bg-white px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
                        <span className="text-xs text-gray-500 uppercase font-bold block">Status</span>
                        <span className="font-medium text-brand-600">
                            {/* Aici poți afișa statusul text sau un label mapat */}
                            {order.status} 
                        </span>
                    </div>
                </div>

                {/* Detalii Client (Vezi nota de la Backend de mai jos pentru nume!) */}
                <div className="p-6 border-b border-gray-100 bg-blue-50/30">
                    <h3 className="text-sm font-bold text-gray-900 uppercase tracking-wide mb-3 flex items-center gap-2">
                        <User size={16} /> Client
                    </h3>
                    <div className="text-gray-700">
                        {/* Momentan avem doar ID-ul, numele trebuie adus din backend */}
                        <p><span className="font-medium">User ID:</span> {order.userId}</p>
                        {order.notes && (
                            <div className="mt-3 p-3 bg-yellow-50 border border-yellow-100 rounded-lg text-yellow-800 text-sm">
                                <strong>Notițe client:</strong> {order.notes}
                            </div>
                        )}
                    </div>
                </div>

                {/* Lista Produse */}
                <div className="p-6">
                    <h3 className="text-sm font-bold text-gray-900 uppercase tracking-wide mb-4 flex items-center gap-2">
                        <Utensils size={16} /> Produse Comandate
                    </h3>
                    <div className="space-y-3">
                        {order.items.map((item) => (
                            <div key={item.id} className="flex justify-between items-center p-3 hover:bg-gray-50 rounded-xl border border-transparent hover:border-gray-100 transition-all">
                                <div className="flex items-center gap-3">
                                    <div className="bg-brand-100 text-brand-700 font-bold w-8 h-8 flex items-center justify-center rounded-lg text-sm">
                                        {item.quantity}x
                                    </div>
                                    <span className="font-medium text-gray-900">{item.menuItemName}</span>
                                </div>
                                <span className="text-gray-500 font-mono">{item.unitPrice.toFixed(2)} RON</span>
                            </div>
                        ))}
                    </div>
                    
                    <div className="mt-6 pt-6 border-t border-gray-100 flex justify-end">
                        <div className="text-right">
                            <span className="text-gray-500 text-sm mr-4">Total</span>
                            <span className="text-3xl font-extrabold text-gray-900">{order.total.toFixed(2)} <span className="text-base font-normal text-gray-500">RON</span></span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )

}
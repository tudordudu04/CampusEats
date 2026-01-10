import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { OrderApi } from '../services/api'
import { OrderDto } from '../types'
import { ArrowLeft, User, Clock, Receipt, Utensils } from 'lucide-react'
import { useLanguage } from '../contexts/LanguageContext'

export default function KitchenOrderDetails() {
    const { id } = useParams<{ id: string }>()
    const navigate = useNavigate()
    const [order, setOrder] = useState<OrderDto | null>(null)
    const [loading, setLoading] = useState(true)
    const { language } = useLanguage()

    useEffect(() => {
        if (!id) return;
        
        OrderApi.getById(id)
            .then(data => setOrder(data))
            .catch(err => alert((language === 'ro' ? 'Nu s-a putut încărca comanda: ' : 'Could not load order: ') + err.message))
            .finally(() => setLoading(false))
    }, [id, language])

    if (loading) return <div className="p-10 text-center dark:text-slate-300">{language === 'ro' ? 'Se încarcă detaliile...' : 'Loading details...'}</div>
    if (!order) return <div className="p-10 text-center text-red-500 dark:text-red-400">{language === 'ro' ? 'Comanda nu a fost găsită.' : 'Order not found.'}</div>

    return (
        <div className="max-w-3xl mx-auto py-8 px-4">
            <button 
                onClick={() => navigate('/kitchen')} 
                className="flex items-center gap-2 text-gray-500 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-100 mb-6 transition-colors"
            >
                <ArrowLeft size={20} /> {language === 'ro' ? 'Înapoi la Bucătărie' : 'Back to Kitchen'}
            </button>

            <div className="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-gray-200 dark:border-slate-700 overflow-hidden">
                {/* Header */}
                <div className="bg-gray-50 dark:bg-slate-900 p-6 border-b border-gray-100 dark:border-slate-700 flex justify-between items-start">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900 dark:text-slate-100 flex items-center gap-2">
                            <Receipt className="text-brand-600 dark:text-brand-400" />
                            {language === 'ro' ? 'Comanda' : 'Order'} #{order.id.slice(0, 8)}
                        </h1>
                        <p className="text-gray-500 dark:text-slate-400 mt-1 flex items-center gap-2">
                            <Clock size={16} /> 
                            {new Date(order.createdAtUtc).toLocaleString(language === 'ro' ? 'ro-RO' : 'en-US')}
                        </p>
                    </div>
                    <div className="bg-white dark:bg-slate-800 px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-700 shadow-sm">
                        <span className="text-xs text-gray-500 dark:text-slate-400 uppercase font-bold block">Status</span>
                        <span className="font-medium text-brand-600 dark:text-brand-400">
                            {/* Aici poți afișa statusul text sau un label mapat */}
                            {order.status} 
                        </span>
                    </div>
                </div>

                {/* Detalii Client (Vezi nota de la Backend de mai jos pentru nume!) */}
                <div className="p-6 border-b border-gray-100 dark:border-slate-700 bg-blue-50/30 dark:bg-blue-950/20">
                    <h3 className="text-sm font-bold text-gray-900 dark:text-slate-100 uppercase tracking-wide mb-3 flex items-center gap-2">
                        <User size={16} /> {language === 'ro' ? 'Client' : 'Customer'}
                    </h3>
                    <div className="text-gray-700 dark:text-slate-300">
                        {/* Momentan avem doar ID-ul, numele trebuie adus din backend */}
                        <p><span className="font-medium">User ID:</span> {order.userId}</p>
                        {order.notes && (
                            <div className="mt-3 p-3 bg-yellow-50 dark:bg-yellow-950/30 border border-yellow-100 dark:border-yellow-900 rounded-lg text-yellow-800 dark:text-yellow-300 text-sm">
                                <strong>{language === 'ro' ? 'Notițe client:' : 'Customer notes:'}</strong> {order.notes}
                            </div>
                        )}
                    </div>
                </div>

                {/* Lista Produse */}
                <div className="p-6">
                    <h3 className="text-sm font-bold text-gray-900 dark:text-slate-100 uppercase tracking-wide mb-4 flex items-center gap-2">
                        <Utensils size={16} /> {language === 'ro' ? 'Produse Comandate' : 'Ordered Items'}
                    </h3>
                    <div className="space-y-3">
                        {order.items.map((item) => (
                            <div key={item.id} className="flex justify-between items-center p-3 hover:bg-gray-50 dark:hover:bg-slate-700/50 rounded-xl border border-transparent hover:border-gray-100 dark:hover:border-slate-600 transition-all">
                                <div className="flex items-center gap-3">
                                    <div className="bg-brand-100 dark:bg-brand-900/50 text-brand-700 dark:text-brand-300 font-bold w-8 h-8 flex items-center justify-center rounded-lg text-sm">
                                        {item.quantity}x
                                    </div>
                                    <span className="font-medium text-gray-900 dark:text-slate-100">{item.menuItemName}</span>
                                </div>
                                <span className="text-gray-500 dark:text-slate-400 font-mono">{item.unitPrice.toFixed(2)} RON</span>
                            </div>
                        ))}
                    </div>
                    
                    <div className="mt-6 pt-6 border-t border-gray-100 dark:border-slate-700 flex justify-end">
                        <div className="text-right">
                            <span className="text-gray-500 dark:text-slate-400 text-sm mr-4">{language === 'ro' ? 'Total' : 'Total'}</span>
                            <span className="text-3xl font-extrabold text-gray-900 dark:text-slate-100">{order.total.toFixed(2)} <span className="text-base font-normal text-gray-500 dark:text-slate-400">RON</span></span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}
import { useEffect, useState } from 'react'
import { LoyaltyApi } from '../services/api'
import { LoyaltyTransactionDto } from '../types'
import { Gift, TrendingUp, TrendingDown, Calendar, History } from 'lucide-react'
import { useLanguage } from '../contexts/LanguageContext'

export default function LoyaltyPage() {
    const [points, setPoints] = useState<number>(0)
    const [transactions, setTransactions] = useState<LoyaltyTransactionDto[]>([])
    const [loading, setLoading] = useState(true)
    const { language } = useLanguage()

    useEffect(() => {
        const loadData = async () => {
            try {
                const [accountData, transactionsData] = await Promise.all([
                    LoyaltyApi.getAccount(),
                    LoyaltyApi.getTransactions()
                ])
                setPoints(accountData.points)
                setTransactions(transactionsData)
            } catch (err) {
                console.error(err)
            } finally {
                setLoading(false)
            }
        }
        loadData()
    }, [])

    if (loading) return <div className="text-center py-20 text-gray-500 dark:text-slate-400">{language === 'ro' ? 'Se încarcă...' : 'Loading...'}</div>

    return (
        <div className="max-w-4xl mx-auto">
            <h2 className="text-2xl font-bold mb-6 text-gray-900 dark:text-slate-100 flex items-center gap-2">
                <Gift className="text-brand-600 dark:text-brand-400" /> {language === 'ro' ? 'Puncte de Loialitate' : 'Loyalty Points'}
            </h2>

            {/* Card Principal Puncte */}
            <div className="bg-gradient-to-r from-brand-600 to-brand-500 rounded-2xl p-8 text-white shadow-xl mb-10 flex flex-col md:flex-row items-center justify-between relative overflow-hidden">
                <div className="relative z-10">
                    <p className="text-brand-100 text-center font-medium mb-1 text-lg">{language === 'ro' ? 'Sold Curent' : 'Current Balance'}</p>
                    <h3 className="text-5xl text-center font-extrabold">{points} <span className="text-2xl font-normal opacity-80">{language === 'ro' ? 'puncte' : 'points'}</span></h3>
                    <p className="mt-4 text-sm bg-white/20 backdrop-blur-sm inline-block px-3 py-1 rounded-full">
                        {language === 'ro' ? '10% din valoarea comenzii = puncte' : '10% of order value = points'}
                    </p>
                </div>
                <div className="relative z-10 mt-6 md:mt-0 bg-white/10 backdrop-blur-md p-4 rounded-xl border border-white/20">
                    <Gift size={64} className="text-white opacity-90" />
                </div>
                
                {/* Elemente decorative */}
                <div className="absolute top-0 right-0 -mr-10 -mt-10 w-40 h-40 bg-white opacity-10 rounded-full blur-2xl"></div>
                <div className="absolute bottom-0 left-0 -ml-10 -mb-10 w-40 h-40 bg-black opacity-10 rounded-full blur-2xl"></div>
            </div>

            {/* Istoric Tranzacții */}
            <div className="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-gray-100 dark:border-slate-700 overflow-hidden">
                <div className="p-6 border-b border-gray-100 dark:border-slate-700 bg-gray-50/50 dark:bg-slate-700/50 flex items-center gap-2">
                    <History className="text-gray-500 dark:text-slate-400" size={20}/>
                    <h3 className="font-bold text-gray-800 dark:text-slate-200">{language === 'ro' ? 'Istoric Tranzacții' : 'Transaction History'}</h3>
                </div>

                <div className="divide-y divide-gray-100 dark:divide-slate-700">
                    {transactions.length === 0 ? (
                        <div className="p-10 text-center text-gray-500 dark:text-slate-400 italic">
                            {language === 'ro' ? 'Nu există tranzacții încă.' : 'No transactions yet.'}
                        </div>
                    ) : (
                        transactions.map((t) => {
                            const isPositive = t.pointsChange > 0;
                            return (
                                <div key={t.id} className="p-5 hover:bg-gray-50 dark:hover:bg-slate-700/50 transition-colors flex items-center justify-between">
                                    <div className="flex items-start gap-4">
                                        <div className={`p-2.5 rounded-full ${isPositive ? 'bg-green-100 dark:bg-green-900/40 text-green-600 dark:text-green-400' : 'bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400'}`}>
                                            {isPositive ? <TrendingUp size={20} /> : <TrendingDown size={20} />}
                                        </div>
                                        <div>
                                            <p className="font-bold text-gray-900 dark:text-slate-100">{t.description}</p>
                                            <p className="text-xs text-gray-500 dark:text-slate-400 flex items-center gap-1 mt-1">
                                                <Calendar size={12} />
                                                {new Date(t.createdAtUtc).toLocaleString('ro-RO')}
                                            </p>
                                        </div>
                                    </div>
                                    <div className={`text-lg font-bold ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
                                        {isPositive ? '+' : ''}{t.pointsChange} pct
                                    </div>
                                </div>
                            )
                        })
                    )}
                </div>
            </div>
        </div>
    )
}
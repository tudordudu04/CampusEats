import { useState } from 'react'
import { AuthApi } from '../services/api'
import { Mail, Lock, ArrowRight, Loader2 } from 'lucide-react'

type Props = { onLoggedIn: () => void }

export default function LoginPage({ onLoggedIn }: Props) {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const onSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setError(null)
        setLoading(true)
        try {
            await AuthApi.login({ email, password })
            onLoggedIn()
        } catch (err: any) {
            let uiMessage = 'Logarea a eșuat';

            const data = JSON.parse(err.message);
            if (data) {
                if (typeof data === 'string') {
                    uiMessage = data.replace('"', '');
                } else if (Array.isArray(data.errors) && data.errors.length > 0) {
                    uiMessage = data.errors.join('\n');
                } else if (typeof data.message === 'string') {
                    uiMessage = data.message;
                }
            }
            setError(uiMessage)
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="flex justify-center items-center min-h-[60vh]">
            <div className="w-full max-w-md bg-white rounded-2xl shadow-xl border border-gray-100 p-8 md:p-10 animate-fade-in">
                <div className="text-center mb-8">
                    <h2 className="text-3xl font-bold text-gray-900">Bine ai venit! 👋</h2>
                    <p className="text-gray-500 mt-2">Te rugăm să te autentifici pentru a continua.</p>
                </div>

                <form onSubmit={onSubmit} className="space-y-5">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
                        <div className="relative group">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 group-focus-within:text-brand-500 transition-colors">
                                <Mail size={18} />
                            </div>
                            <input
                                type="email"
                                value={email}
                                onChange={e => setEmail(e.target.value)}
                                className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 rounded-xl focus:ring-2 focus:ring-brand-500 focus:border-brand-500 transition-all outline-none bg-gray-50 focus:bg-white"
                                placeholder="student@test.com"
                                required
                            />
                        </div>
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1.5">Parolă</label>
                        <div className="relative group">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 group-focus-within:text-brand-500 transition-colors">
                                <Lock size={18} />
                            </div>
                            <input
                                type="password"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 rounded-xl focus:ring-2 focus:ring-brand-500 focus:border-brand-500 transition-all outline-none bg-gray-50 focus:bg-white"
                                placeholder="••••••••"
                                required
                            />
                        </div>
                    </div>
                    
                    {error && (
                        <div className="p-3 bg-red-50 text-red-700 text-sm rounded-lg border border-red-100 flex items-center gap-2 animate-shake">
                            <span className="font-bold">!</span> {error}
                        </div>
                    )}

                    <button 
                        type="submit" 
                        disabled={loading}
                        className="w-full flex items-center justify-center gap-2 bg-brand-600 hover:bg-brand-700 text-white py-3 rounded-xl font-bold shadow-lg shadow-brand-500/20 transition-all disabled:opacity-70 hover:-translate-y-0.5 active:translate-y-0"
                    >
                        {loading ? <Loader2 className="animate-spin" /> : (
                            <>Sign in <ArrowRight size={18} /></>
                        )}
                    </button>
                </form>
            </div>
        </div>
    )
}
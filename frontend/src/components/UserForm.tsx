import React, { useState } from 'react'
import { Mail, Lock, User as UserIcon, ArrowRight, Loader2 } from 'lucide-react'
import { useLanguage } from '../contexts/LanguageContext'

export type UserRoleValue = 0 | 1 | 2

export type UserFormValues = {
    name: string
    email: string
    password: string
    confirmPassword: string
    role: UserRoleValue
}

type Props = {
    showRoleSelector?: boolean
    initialRole?: UserRoleValue
    loading?: boolean
    error?: string | null
    /** Called when the form is submitted and passwords match */
    onSubmit: (values: UserFormValues) => void | Promise<void>
}

export default function UserForm({
     showRoleSelector = false,
     initialRole = 0,
     loading = false,
     error = null,
     onSubmit,
 }: Props) 
{
    const [name, setName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [role, setRole] = useState<UserRoleValue>(initialRole)
    const [localError, setLocalError] = useState<string | null>(null)
    const { language } = useLanguage()

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setLocalError(null)

        if (password !== confirmPassword) {
            setLocalError(language === 'ro' ? 'Parolele nu se potrivesc' : 'Passwords do not match')
            return
        }

        await onSubmit({ name, email, password, confirmPassword, role })
    }

    const effectiveError = error ?? localError

    return (
        <form onSubmit={handleSubmit} className="space-y-5">
            <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1.5">{language === 'ro' ? 'Nume Complet' : 'Full Name'}</label>
                <div className="relative group">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 dark:text-slate-500 group-focus-within:text-brand-500 dark:group-focus-within:text-brand-400">
                        <UserIcon size={18} />
                    </div>
                    <input
                        type="text"
                        value={name}
                        onChange={e => setName(e.target.value)}
                        className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 dark:border-slate-600 rounded-xl focus:ring-2 focus:ring-brand-500 dark:focus:ring-brand-400 focus:border-brand-500 dark:focus:border-brand-400 outline-none bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 transition-all dark:text-slate-100"
                        placeholder={language === 'ro' ? 'Ion Popescu' : 'John Doe'}
                        required
                    />
                </div>
            </div>

            <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1.5">Email</label>
                <div className="relative group">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 dark:text-slate-500 group-focus-within:text-brand-500 dark:group-focus-within:text-brand-400">
                        <Mail size={18} />
                    </div>
                    <input
                        type="email"
                        value={email}
                        onChange={e => setEmail(e.target.value)}
                        className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 dark:border-slate-600 rounded-xl focus:ring-2 focus:ring-brand-500 dark:focus:ring-brand-400 focus:border-brand-500 dark:focus:border-brand-400 outline-none bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 transition-all dark:text-slate-100"
                        placeholder="student@test.com"
                        required
                    />
                </div>
            </div>

            <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1.5">{language === 'ro' ? 'Parolă' : 'Password'}</label>
                <div className="relative group">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 dark:text-slate-500 group-focus-within:text-brand-500 dark:group-focus-within:text-brand-400">
                        <Lock size={18} />
                    </div>
                    <input
                        type="password"
                        value={password}
                        onChange={e => setPassword(e.target.value)}
                        className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 dark:border-slate-600 rounded-xl focus:ring-2 focus:ring-brand-500 dark:focus:ring-brand-400 focus:border-brand-500 dark:focus:border-brand-400 outline-none bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 transition-all dark:text-slate-100"
                        placeholder="••••••••"
                        required
                    />
                </div>
            </div>

            <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1.5">{language === 'ro' ? 'Confirmă Parola' : 'Confirm Password'}</label>
                <div className="relative group">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 dark:text-slate-500 group-focus-within:text-brand-500 dark:group-focus-within:text-brand-400 transition-colors">
                        <Lock size={18} />
                    </div>
                    <input
                        type="password"
                        value={confirmPassword}
                        onChange={e => setConfirmPassword(e.target.value)}
                        className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 dark:border-slate-600 rounded-xl focus:ring-2 focus:ring-brand-500 dark:focus:ring-brand-400 focus:border-brand-500 dark:focus:border-brand-400 transition-all outline-none bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 dark:text-slate-100"
                        placeholder="••••••••"
                        required
                    />
                </div>
            </div>

            {showRoleSelector && (
                <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1.5">
                        {language === 'ro' ? 'Rol utilizator' : 'User role'}
                    </label>
                    <select
                        value={role}
                        onChange={e => setRole(Number(e.target.value) as UserRoleValue)}
                        className="block w-full px-3 py-2.5 border border-gray-300 dark:border-slate-600 rounded-xl focus:ring-2 focus:ring-brand-500 dark:focus:ring-brand-400 focus:border-brand-500 dark:focus:border-brand-400 outline-none bg-gray-50 dark:bg-slate-700 focus:bg-white dark:focus:bg-slate-600 transition-all dark:text-slate-100"
                    >
                        <option value={0}>Student</option>
                        <option value={1}>Worker</option>
                        <option value={2}>Manager</option>
                    </select>
                </div>
            )}

            {effectiveError && (
                <div className="p-3 bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-300 text-sm rounded-lg border border-red-100 dark:border-red-800 flex items-center gap-2">
                    <span className="font-bold">!</span> {effectiveError}
                </div>
            )}

            <button
                type="submit"
                disabled={loading}
                className="w-full flex items-center justify-center gap-2 bg-brand-600 hover:bg-brand-700 text-white py-3 rounded-xl font-bold shadow-lg shadow-brand-500/20 transition-all disabled:opacity-70 hover:-translate-y-0.5"
            >
                {loading ? <Loader2 className="animate-spin" /> : (
                    <>{language === 'ro' ? 'Crează Cont' : 'Create Account'} <ArrowRight size={18} /></>
                )}
            </button>
        </form>
    )
}

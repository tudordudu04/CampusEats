import { useState } from 'react'
import MenuForm from '../components/MenuForm'
import UserManagement from '../components/UserManagement'
import UserForm, { type UserFormValues } from '../components/UserForm'
import { CouponManagement } from '../components/CouponManagement'
import { AuthApi } from '../services/api'
import { useLanguage } from '../contexts/LanguageContext'

type AdminAction = 'menu' | 'register' | 'delete' | 'coupons'

export default function AdminPage() {
    const [selectedAction, setSelectedAction] = useState<AdminAction>('menu')
    const [registerLoading, setRegisterLoading] = useState(false)
    const [registerError, setRegisterError] = useState<string | null>(null)
    const [registerSuccess, setRegisterSuccess] = useState<string | null>(null)
    const { language } = useLanguage()

    async function handleAdminRegisterSubmit(values: UserFormValues) {
        const { name, email, password, role } = values
        setRegisterError(null)
        setRegisterSuccess(null)
        setRegisterLoading(true)
        try {
            await AuthApi.adminRegister({ name, email, password, role })
            setRegisterSuccess(language === 'ro' ? 'Utilizator înregistrat cu succes.' : 'User registered successfully.')
        } catch (err: any) {
            setRegisterError(err.message || (language === 'ro' ? 'Înregistrarea a eșuat' : 'Registration failed'))
        } finally {
            setRegisterLoading(false)
        }
    }

    return (
        <div className="max-w-5xl mx-auto py-8">
            <h1 className="text-3xl text-center md:text-3xl font-bold mb-6">{language === 'ro' ? 'Panou Admin' : 'Admin Panel'}</h1>

            <div className="mb-6 overflow-x-auto">
                <div className="flex gap-3 min-w-max">
                    <button
                        onClick={() => setSelectedAction('menu')}
                        className={`shrink-0 px-4 py-2 rounded-lg border ${
                            selectedAction === 'menu'
                                ? 'bg-brand-600 dark:bg-brand-700 text-white border-brand-600 dark:border-brand-700'
                                : 'bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 border-gray-300 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700'
                        }`}
                    >
                        {language === 'ro' ? 'Gestionează Meniu' : 'Manage Menu'}
                    </button>

                    <button
                        onClick={() => setSelectedAction('coupons')}
                        className={`shrink-0 px-4 py-2 rounded-lg border ${
                            selectedAction === 'coupons'
                                ? 'bg-brand-600 dark:bg-brand-700 text-white border-brand-600 dark:border-brand-700'
                                : 'bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 border-gray-300 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700'
                        }`}
                    >
                        Manage Coupons
                    </button>

                    <button
                        onClick={() => setSelectedAction('register')}
                        className={`shrink-0 px-4 py-2 rounded-lg border ${
                            selectedAction === 'register'
                                ? 'bg-brand-600 dark:bg-brand-700 text-white border-brand-600 dark:border-brand-700'
                                : 'bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 border-gray-300 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700'
                        }`}
                    >
                        {language === 'ro' ? 'Înregistrează utilizator' : 'Register user'}
                    </button>

                    <button
                        onClick={() => setSelectedAction('delete')}
                        className={`shrink-0 px-4 py-2 rounded-lg border ${
                            selectedAction === 'delete'
                                ? 'bg-brand-600 dark:bg-brand-700 text-white border-brand-600 dark:border-brand-700'
                                : 'bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 border-gray-300 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700'
                        }`}
                    >
                        {language === 'ro' ? 'Șterge utilizator' : 'Delete user'}
                    </button>
                </div>
            </div>

            <div className="bg-white dark:bg-slate-800 rounded-2xl shadow p-6 border border-gray-100 dark:border-slate-700">
                {selectedAction === 'menu' && (
                    <div>
                        <h2 className="text-xl font-semibold mb-4 dark:text-slate-100">{language === 'ro' ? 'Adaugă produs în meniu' : 'Add menu item'}</h2>
                        <div className="overflow-x-auto">
                            <MenuForm />
                        </div>
                    </div>
                )}

                {selectedAction === 'coupons' && (
                    <div>
                        <CouponManagement />
                    </div>
                )}

                {selectedAction === 'register' && (
                    <div>
                        <h2 className="text-xl font-semibold mb-4 dark:text-slate-100">{language === 'ro' ? 'Înregistrează utilizator' : 'Register user'}</h2>
                        <UserForm
                            showRoleSelector={true}
                            initialRole={1}
                            loading={registerLoading}
                            error={registerError}
                            onSubmit={handleAdminRegisterSubmit}
                        />
                        {registerSuccess && (
                            <div className="mt-4 p-3 bg-green-50 dark:bg-green-950/50 text-green-700 dark:text-green-400 text-sm rounded-lg border border-green-100 dark:border-green-800">
                                {registerSuccess}
                            </div>
                        )}
                    </div>
                )}

                {selectedAction === 'delete' && (
                    <UserManagement />
                )}
            </div>
        </div>
    )
}
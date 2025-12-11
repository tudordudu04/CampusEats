import { useState } from 'react'
import MenuForm from '../components/MenuForm'
import UserManagement from '../components/UserManagement'
import UserForm, { type UserFormValues } from '../components/UserForm'
import { CouponManagement } from '../components/CouponManagement'
import { AuthApi } from '../services/api'

type AdminAction = 'menu' | 'register' | 'delete' | 'coupons'

export default function AdminPage() {
    const [selectedAction, setSelectedAction] = useState<AdminAction>('menu')
    const [registerLoading, setRegisterLoading] = useState(false)
    const [registerError, setRegisterError] = useState<string | null>(null)
    const [registerSuccess, setRegisterSuccess] = useState<string | null>(null)

    async function handleAdminRegisterSubmit(values: UserFormValues) {
        const { name, email, password, role } = values
        setRegisterError(null)
        setRegisterSuccess(null)
        setRegisterLoading(true)
        try {
            await AuthApi.adminRegister({ name, email, password, role })
            setRegisterSuccess('User registered succesfuly.')
        } catch (err: any) {
            setRegisterError(err.message || 'Înregistrarea a eșuat')
        } finally {
            setRegisterLoading(false)
        }
    }

    return (
        <div className="max-w-5xl mx-auto py-8">
            <h1 className="text-3xl font-bold mb-6">Admin Panel</h1>

            <div className="flex gap-3 mb-6">
                <button
                    onClick={() => setSelectedAction('menu')}
                    className={`px-4 py-2 rounded-lg border ${
                        selectedAction === 'menu'
                            ? 'bg-brand-600 text-white border-brand-600'
                            : 'bg-white text-gray-700 border-gray-300'
                    }`}
                >
                    Add menu item
                </button>

                <button
                    onClick={() => setSelectedAction('coupons')}
                    className={`px-4 py-2 rounded-lg border ${
                        selectedAction === 'coupons'
                            ? 'bg-brand-600 text-white border-brand-600'
                            : 'bg-white text-gray-700 border-gray-300'
                    }`}
                >
                    Manage Coupons
                </button>

                <button
                    onClick={() => setSelectedAction('register')}
                    className={`px-4 py-2 rounded-lg border ${
                        selectedAction === 'register'
                            ? 'bg-brand-600 text-white border-brand-600'
                            : 'bg-white text-gray-700 border-gray-300'
                    }`}
                >
                    Register user
                </button>

                <button
                    onClick={() => setSelectedAction('delete')}
                    className={`px-4 py-2 rounded-lg border ${
                        selectedAction === 'delete'
                            ? 'bg-brand-600 text-white border-brand-600'
                            : 'bg-white text-gray-700 border-gray-300'
                    }`}
                >
                    Delete user
                </button>
            </div>

            <div className="bg-white rounded-2xl shadow p-6">
                {selectedAction === 'menu' && (
                    <div>
                        <h2 className="text-xl font-semibold mb-4">Add menu item</h2>
                        <MenuForm />
                    </div>
                )}

                {selectedAction === 'coupons' && (
                    <div>
                        <CouponManagement />
                    </div>
                )}

                {selectedAction === 'register' && (
                    <div>
                        <h2 className="text-xl font-semibold mb-4">Register user</h2>
                        <UserForm
                            showRoleSelector={true}
                            initialRole={1}
                            loading={registerLoading}
                            error={registerError}
                            onSubmit={handleAdminRegisterSubmit}
                        />
                        {registerSuccess && (
                            <div className="mt-4 p-3 bg-green-50 text-green-700 text-sm rounded-lg border border-green-100">
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

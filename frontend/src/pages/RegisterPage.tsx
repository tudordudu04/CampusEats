import { useState } from 'react'
import { AuthApi } from '../services/api'
import UserForm, { UserFormValues, UserRoleValue } from '../components/UserForm'

type Props = {
    onRegistered: () => void
    initialRole?: UserRoleValue
    showRoleSelector?: boolean
}

export default function RegisterPage({ onRegistered, initialRole = 0, showRoleSelector = false }: Props) {
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const handleSubmit = async ({ name, email, password, role }: UserFormValues) => {
        setError(null)
        setLoading(true)
        try {
            await AuthApi.register({ name, email, password, role })
            onRegistered()
        } catch (err: any) {
            let uiMessage = 'Înregistrarea a eșuat';

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
            setError(uiMessage);
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="flex justify-center items-center min-h-[60vh]">
            <div className="w-full max-w-md bg-white rounded-2xl shadow-xl border border-gray-100 p-8 md:p-10 animate-fade-in">
                <div className="text-center mb-8">
                    <h2 className="text-3xl font-bold text-gray-900">Cont Nou 🚀</h2>
                    <p className="text-gray-500 mt-2">Completează datele pentru a te înregistra.</p>
                </div>

                <UserForm
                    showRoleSelector={showRoleSelector}
                    initialRole={initialRole}
                    loading={loading}
                    error={error}
                    onSubmit={handleSubmit}
                />
            </div>
        </div>
    )
}

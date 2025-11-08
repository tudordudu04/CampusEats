// frontend/src/pages/RegisterPage.tsx
import { useState } from 'react'
import { AuthApi } from '../services/api'

type Props = { onRegistered: () => void }

export default function RegisterPage({ onRegistered }: Props) {
    const [name, setName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const onSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setError(null)
        setLoading(true)
        try {
            // Assumes /auth/register returns { accessToken } like login
            await AuthApi.register({ name, email, password })
            onRegistered()
        } catch (err: any) {
            setError(err.message || 'Registration failed')
        } finally {
            setLoading(false)
        }
    }

    return (
        <form onSubmit={onSubmit} style={{ display: 'grid', gap: 12, maxWidth: 360 }}>
            <h2>Register</h2>
            <label>
                Name
                <input
                    type="text"
                    value={name}
                    onChange={e => setName(e.target.value)}
                    required
                />
            </label>
            <label>
                Email
                <input
                    type="email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    required
                />
            </label>
            <label>
                Password
                <input
                    type="password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    required
                />
            </label>
            {error && <div style={{ color: 'crimson' }}>{error}</div>}
            <button type="submit" disabled={loading}>
                {loading ? 'Creating account…' : 'Create account'}
            </button>
        </form>
    )
}

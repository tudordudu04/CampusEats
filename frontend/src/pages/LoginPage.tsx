// frontend/src/pages/LoginPage.tsx
import { useState } from 'react'
import { AuthApi } from '../services/api'

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
            setError(err.message || 'Login failed')
        } finally {
            setLoading(false)
        }
    }

    return (
        <form onSubmit={onSubmit} style={{ display: 'grid', gap: 12, maxWidth: 360 }}>
            <h2>Login</h2>
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
                {loading ? 'Signing in…' : 'Sign in'}
            </button>
        </form>
    )
}

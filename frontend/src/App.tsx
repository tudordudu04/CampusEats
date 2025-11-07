// frontend/src/App.tsx
import { useState } from 'react'
import MenuPage from './pages/MenuPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import { AuthApi } from './services/api'

export default function App() {
    const [view, setView] = useState<'login' | 'register' | 'app'>(
        AuthApi.getToken() ? 'app' : 'login'
    )

    const onAuthDone = () => setView('app')

    return (
        <div style={{ padding: 24, fontFamily: 'system-ui, sans-serif' }}>
            <header style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
                <h1 style={{ marginRight: 'auto' }}>CampusEats</h1>
                {view === 'app' ? (
                    <button onClick={async () => { await AuthApi.logout(); setView('login') }}>
                        Logout
                    </button>
                ) : (
                    <>
                        <button
                            onClick={() => setView('login')}
                            disabled={view === 'login'}
                        >
                            Login
                        </button>
                        <button
                            onClick={() => setView('register')}
                            disabled={view === 'register'}
                        >
                            Register
                        </button>
                    </>
                )}
            </header>

            <main style={{ marginTop: 24 }}>
                {view === 'app' && (
                    <>
                        <h2>Menu Management</h2>
                        <MenuPage />
                    </>
                )}
                {view === 'login' && <LoginPage onLoggedIn={onAuthDone} />}
                {view === 'register' && <RegisterPage onRegistered={onAuthDone} />}
            </main>
        </div>
    )
}

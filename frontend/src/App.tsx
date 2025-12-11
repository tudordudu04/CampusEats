import { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Link, Navigate, useLocation } from 'react-router-dom'
import { jwtDecode } from 'jwt-decode'
import { AuthApi } from './services/api'
import { LogOut, Pizza, ShoppingBag, ClipboardList, ChefHat, Settings, Gift, Warehouse } from 'lucide-react'
import type { MenuItem } from './types'

// Pagini
import MenuPage from './pages/MenuPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import OrdersPage from './pages/OrdersPage'
import KitchenDashboard from './pages/KitchenDashboard'
import KitchenOrderDetails from './pages/KitchenOrderDetails'
import MenuForm from './components/MenuForm'
import OrderCart from './components/OrderCart'
import PaymentResult from './components/PaymentResult'
import LoyaltyPage from './pages/LoyaltyPage' // Import pagina nouă
import AdminPage from './pages/AdminPage'
import { useLoyaltyPoints } from './hooks/useLoyaltyPoints'
import InventoryPage from "./pages/InventoryPage";

type CartItem = { item: MenuItem; quantity: number }

// Componenta ajutătoare pentru link-uri de navigare
function NavLink({ to, icon: Icon, children, active }: any) {
    return (
        <Link 
            to={to} 
            className={`flex items-center gap-2 px-4 py-2 rounded-full transition-all font-medium text-sm
            ${active ? 'bg-brand-100 text-brand-700 shadow-sm' : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'}`}
        >
            <Icon size={18} />
            {children}
        </Link>
    )
}

function Layout({ children, role, onLogout }: any) {
    const location = useLocation()
    const { points: loyaltyPoints } = useLoyaltyPoints(role === 'STUDENT')
    const points = loyaltyPoints ?? 0
    return (
        <div className="min-h-screen flex flex-col bg-gray-50 font-sans">
            <header className="bg-white/90 backdrop-blur-md border-b border-gray-200 sticky top-0 z-40 shadow-sm">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between h-16 items-center">
                        {/* Logo */}
                        <div className="flex items-center gap-2">
                            <div className="bg-brand-500 p-2 rounded-lg text-white">
                                <Pizza size={24} />
                            </div>
                            <Link to="/" className="text-xl font-bold bg-gradient-to-r from-brand-600 to-brand-500 bg-clip-text text-transparent hover:opacity-80 transition-opacity">
                                CampusEats
                            </Link>
                        </div>
                        
                        {/* Navigare Desktop */}
                        <nav className="hidden md:flex gap-2">
                            <NavLink to="/" icon={ShoppingBag} active={location.pathname === '/'}>Meniu</NavLink>
                            
                            {role && (
                                <NavLink to="/orders" icon={ClipboardList} active={location.pathname === '/orders'}>Comenzi</NavLink>
                            )}
                            
                            {(role === 'WORKER' || role === 'MANAGER') && (
                                <NavLink to="/kitchen" icon={ChefHat} active={location.pathname === '/kitchen'}>Bucătărie</NavLink>
                            )}

                            {(role === 'WORKER' || role === 'MANAGER') && (
                                <NavLink to="/inventory" icon={Warehouse} active={location.pathname === '/inventory'}>Inventar</NavLink>
                            )}
                            
                            {(role === 'MANAGER') && (
                                <NavLink to="/admin" icon={Settings} active={location.pathname === '/admin'}>Admin</NavLink>
                            )}
                        </nav>

                        {/* Zona Utilizator / Login */}
                        <div className="flex items-center gap-4">
                            {role ? (
                                <div className="flex items-center gap-3">
                                    {/* Link Puncte Loialitate - doar pentru STUDENT */}
                                    {role === 'STUDENT' && (
                                        <>
                                            <Link to="/loyalty" className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-amber-50 border border-amber-200 hover:bg-amber-100 rounded-full text-amber-700 text-sm font-semibold shadow-sm transition-colors cursor-pointer" title="Vezi Detalii Puncte">
                                                <Gift size={14} />
                                                <span>{points}</span>
                                            </Link>
                                            <div className="h-6 w-px bg-gray-300 mx-1"></div>
                                        </>
                                    )}

                                    <button
                                        onClick={onLogout}
                                        className="flex items-center gap-2 text-gray-500 hover:text-red-600 transition-colors text-sm font-medium hover:bg-red-50 px-3 py-1.5 rounded-lg"
                                        title="Deconectare"
                                    >
                                        <LogOut size={18} />
                                    </button>
                                </div>
                            ) : (
                                <div className="flex gap-3">
                                    <Link to="/login" className="text-gray-600 hover:text-brand-600 font-medium text-sm px-3 py-2">Login</Link>
                                    <Link to="/register" className="bg-brand-600 hover:bg-brand-700 text-white px-5 py-2 rounded-full text-sm font-medium shadow-md shadow-brand-500/30 transition-all hover:scale-105">
                                        Sign Up
                                    </Link>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </header>
            
            <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8 animate-fade-in">
                {children}
            </main>
        </div>
    )
}

export default function App() {
    const [token, setToken] = useState<string | null>(() => AuthApi.getToken())
    const [role, setRole] = useState<string | null>(null)
    const [cart, setCart] = useState<CartItem[]>([])
    const [isRefreshing, setIsRefreshing] = useState<boolean>(() => !!AuthApi.getToken())

    useEffect(() => {
        let interval: number | undefined

        const performRefresh = async () => {
            try {
                await AuthApi.refresh()
                setToken(AuthApi.getToken())
            } catch (error) {
                console.error('Token refresh failed:', error)
                await AuthApi.logout()
                setToken(null)
                localStorage.setItem('has_logged_out', '1')
            } finally {
                setIsRefreshing(false)
            }
        }

        const hasLoggedOut = localStorage.getItem('has_logged_out') === '1'

        if (token) {
            performRefresh()
            interval = setInterval(performRefresh, 14 * 60 * 1000)
        } else {
            if (!hasLoggedOut) {
                performRefresh()
            } else {
                setIsRefreshing(false)
            }
        }

        return () => {
            if (interval) clearInterval(interval)
        }
    }, [token])

    useEffect(() => {
        if (token) {
            try {
                const decoded: any = jwtDecode(token)
                const userRole =
                    decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                    decoded.role
                setRole(userRole)
            } catch {
                setRole(null)
            }
        } else {
            setRole(null)
        }
    }, [token])

    const handleLogout = async () => {
        localStorage.setItem('has_logged_out', '1')
        await AuthApi.logout()
        setToken(null)
        setCart([])
        window.location.href = '/'
    }

    if (isRefreshing) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-brand-600"></div>
            </div>
        )
    }

    const addToCart = (item: MenuItem) => {
        setCart(prev => {
            const existing = prev.find(c => c.item.id === item.id)
            if (existing) {
                return prev.map(c =>
                    c.item.id === item.id ? { ...c, quantity: c.quantity + 1 } : c
                )
            }
            return [...prev, { item, quantity: 1 }]
        })
    }

    const updateQuantity = (itemId: string, qty: number) => {
        if (qty <= 0) {
            setCart(prev => prev.filter(c => c.item.id !== itemId))
        } else {
            setCart(prev => prev.map(c =>
                c.item.id === itemId ? { ...c, quantity: qty } : c
            ))
        }
    }

    const onPaymentSuccess = () => {
        setCart([])
        alert("Plată realizată cu succes!")
        window.location.href = '/orders'
    }

    return (
        <BrowserRouter>
            <Layout role={role} onLogout={handleLogout}>
                <Routes>
                    <Route path="/" element={
                        <>
                            <div className="mb-8 text-center md:text-left">
                                <h1 className="text-3xl md:text-4xl font-extrabold text-gray-900 tracking-tight">Meniu Delicios 🍔</h1>
                                <p className="text-gray-500 mt-2 text-lg">Comandă mâncarea preferată direct din campus.</p>
                            </div>
                            <MenuPage onAddToCart={addToCart} isLoggedIn={!!token} />
                            {token && (
                                <OrderCart
                                    cart={cart}
                                    onClear={() => setCart([])}
                                    onUpdateQuantity={updateQuantity}
                                />
                            )}
                        </>
                    } />
                    
                    <Route path="/login" element={!token ? <LoginPage onLoggedIn={() => setToken(AuthApi.getToken())} /> : <Navigate to="/" />} />
                    <Route path="/register" element={!token ? 
                        <RegisterPage
                        initialRole={0} 
                        showRoleSelector={false}
                        onRegistered={() => setToken(AuthApi.getToken())}
                        /> : <Navigate to="/" />} />
                    
                    {/* Rute Protejate Utilizator */}
                    <Route path="/loyalty" element={token ? <LoyaltyPage /> : <Navigate to="/login" />} />
                    <Route path="/orders" element={token ? <OrdersPage /> : <Navigate to="/login" />} />
                    
                    {/* Rute Protejate Staff */}
                    <Route path="/kitchen" element={(role === 'WORKER' || role === 'MANAGER') ? <KitchenDashboard /> : <Navigate to="/" />} />

                    <Route path="/admin" element={ role === 'MANAGER' ? <AdminPage /> : <Navigate to="/login" />}/>
                    <Route path="*" element={<Navigate to="/" />} />
                    <Route path="/kitchen/order/:id" element={(role === 'WORKER' || role === 'MANAGER') ? <KitchenOrderDetails /> : <Navigate to="/" />} />

                    <Route path="/inventory" element={(role === 'WORKER' || role === 'MANAGER') ? <InventoryPage /> : <Navigate to="/" />} />
                    <Route path="/admin/menu" element={(role === 'MANAGER') ? <MenuForm /> : <Navigate to="/" />} />

                </Routes>
                <PaymentResult onSuccess={onPaymentSuccess} />
            </Layout>
        </BrowserRouter>
    )
}
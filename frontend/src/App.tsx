import { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Link, Navigate, useLocation } from 'react-router-dom'
import { jwtDecode } from 'jwt-decode'
import { AuthApi } from './services/api'
import { LogOut, Pizza, ShoppingBag, ClipboardList, ChefHat, Settings, Gift, Warehouse, Ticket, User, Moon, Sun, Languages } from 'lucide-react'
import type { MenuItem } from './types'
import { useTheme } from './contexts/ThemeContext'
import { useLanguage } from './contexts/LanguageContext'


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
import InventoryPage from "./pages/InventoryPage"
import { CouponsPage } from './pages/CouponsPage';
import ProfilePage from "./pages/ProfilePage"
import { ReviewsModal } from './components/ReviewsModal'
type CartItem = { item: MenuItem; quantity: number }

// Componenta ajutătoare pentru link-uri de navigare
function NavLink({ to, icon: Icon, children, active }: any) {
    return (
        <Link 
            to={to} 
            className={`flex items-center gap-2 px-4 py-2 rounded-full transition-all font-medium text-sm
            ${active
                ? 'bg-brand-100 dark:bg-brand-900/40 text-brand-700 dark:text-brand-400 border-brand-300 dark:border-brand-700 shadow'
                : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300 border-gray-200 dark:border-slate-600 hover:bg-brand-50 dark:hover:bg-slate-600 hover:text-brand-700 dark:hover:text-brand-400 hover:border-brand-200 dark:hover:border-brand-700'}`}
        >
            <Icon size={18} />
            {children}
        </Link>
    )
}

function MobileMenu({ isOpen, onClose, role, activePath }: any) {
    return (
        <div
            className={`
                fixed inset-0 z-50 md:hidden
                bg-black/40 backdrop-blur-sm
                transition-opacity duration-200
                ${isOpen ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none'}
            `}
            onClick={onClose}
        >
            <div
                className={`
                    absolute left-0 top-0 h-full w-64 bg-white shadow-xl p-4
                    transform transition-transform duration-200
                    ${isOpen ? 'translate-x-0' : '-translate-x-full'}
                `}
                onClick={e => e.stopPropagation()}
            >
                <div className="flex items-center justify-between mb-4">
                    <span className="text-lg font-semibold">Navigation</span>
                    <button
                        onClick={onClose}
                        aria-label="Close menu"
                        className="text-gray-500 hover:text-gray-800 text-2xl leading-none"
                    >
                        ×
                    </button>
                </div>

                <div className="flex flex-col gap-2">
                    <div onClick={onClose}>
                        <NavLink to="/" icon={ShoppingBag} active={activePath === '/'}>Meniu</NavLink>
                    </div>

                    {role && (
                        <div onClick={onClose}>
                            <NavLink to="/orders" icon={ClipboardList} active={activePath === '/orders'}>Comenzi</NavLink>
                        </div>
                    )}

                    {role === 'STUDENT' && (
                        <div onClick={onClose}>
                            <NavLink to="/coupons" icon={Ticket} active={activePath === '/coupons'}>Cupoane</NavLink>
                        </div>
                    )}

                    {(role === 'WORKER' || role === 'MANAGER') && (
                        <div onClick={onClose}>
                            <NavLink to="/kitchen" icon={ChefHat} active={activePath === '/kitchen'}>Bucătărie</NavLink>
                        </div>
                    )}

                    {(role === 'WORKER' || role === 'MANAGER') && (
                        <div onClick={onClose}>
                            <NavLink to="/inventory" icon={Warehouse} active={activePath === '/inventory'}>Inventar</NavLink>
                        </div>
                    )}

                    {role === 'MANAGER' && (
                        <div onClick={onClose}>
                            <NavLink to="/admin" icon={Settings} active={activePath === '/admin'}>Admin</NavLink>
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}


function Layout({ children, role, onLogout,user }: any) {
    const location = useLocation()
    const { points: loyaltyPoints } = useLoyaltyPoints(role === 'STUDENT')
    const points = loyaltyPoints ?? 0
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const { theme, toggleTheme } = useTheme()
    const { language, toggleLanguage } = useLanguage()
    
    return (
        <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-slate-900 font-sans overflow-hidden transition-colors duration-200">
            <header className={`
            sticky top-0 z-40 border-b dark:border-slate-700 shadow-sm
            ${isMenuOpen ? 'bg-white dark:bg-slate-800' : 'bg-white/90 dark:bg-slate-800/90 backdrop-blur-md'}
          `}>
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
                        
                        <button
                            className="flex md:hidden items-center gap-2 px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 text-gray-700 dark:text-slate-200 shadow-sm dark:bg-slate-700"
                            onClick={() => setIsMenuOpen(true)}
                            aria-label="Open menu"
                        >
                            <span className="text-sm font-semibold">Menu</span>
                        </button>
                        
                        {/* Navigare Desktop */}
                        <nav className="hidden md:flex flex-wrap gap-2 md:flex-nowrap">
                            <NavLink to="/" icon={ShoppingBag} active={location.pathname === '/'}>{language === 'ro' ? 'Meniu' : 'Menu'}</NavLink>
                            
                            {role && (
                                <NavLink to="/orders" icon={ClipboardList} active={location.pathname === '/orders'}>{language === 'ro' ? 'Comenzi' : 'Orders'}</NavLink>
                            )}

                            {role === 'STUDENT' && (
                                <NavLink to="/coupons" icon={Ticket} active={location.pathname === '/coupons'}>{language === 'ro' ? 'Cupoane' : 'Coupons'}</NavLink>
                            )}
                            
                            {(role === 'WORKER' || role === 'MANAGER') && (
                                <NavLink to="/kitchen" icon={ChefHat} active={location.pathname === '/kitchen'}>{language === 'ro' ? 'Bucătărie' : 'Kitchen'}</NavLink>
                            )}

                            {(role === 'WORKER' || role === 'MANAGER') && (
                                <NavLink to="/inventory" icon={Warehouse} active={location.pathname === '/inventory'}>{language === 'ro' ? 'Inventar' : 'Inventory'}</NavLink>
                            )}
                            
                            {(role === 'MANAGER') && (
                                <NavLink to="/admin" icon={Settings} active={location.pathname === '/admin'}>Admin</NavLink>
                            )}
                        </nav>

                        {/* Navigare Mobile */}
                        <div className={`
                            fixed right-0 top-0 z-50 md:hidden
                            bg-white dark:bg-slate-800 rounded-l-lg p-2 shadow-sm w-64 h-screen
                            transform transition-transform duration-200 ease-in-out
                            ${isMenuOpen ? 'translate-x-0 pointer-events-auto' : 'translate-x-full pointer-events-none'}
                          `}
                        >
                            <div className="flex flex-col gap-2">
                                <div className="flex items-center justify-between px-3 py-2 text-sm font-semibold border-b border-gray-200 dark:border-slate-700 dark:text-slate-200">
                                    {language === 'ro' ? 'Navigare' : 'Navigation'}
                                    <button className="bg-gray-100 dark:bg-slate-700 hover:bg-brand-50 dark:hover:bg-slate-600 text-black dark:text-slate-200 border dark:border-slate-600 font-bold px-2 rounded hover:border-brand-200" onClick={() => setIsMenuOpen(false)}>
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="size-6">
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
                                        </svg>
                                    </button>
                                </div>
                                <nav className="flex flex-col gap-2">
                                    <NavLink to="/" icon={ShoppingBag} active={location.pathname === '/'}>{language === 'ro' ? 'Meniu' : 'Menu'}</NavLink>

                                    {role && (
                                        <NavLink to="/orders" icon={ClipboardList} active={location.pathname === '/orders'}>{language === 'ro' ? 'Comenzi' : 'Orders'}</NavLink>
                                    )}

                                    {role === 'STUDENT' && (
                                        <NavLink to="/coupons" icon={Ticket} active={location.pathname === '/coupons'}>{language === 'ro' ? 'Cupoane' : 'Coupons'}</NavLink>
                                    )}

                                    {(role === 'WORKER' || role === 'MANAGER') && (
                                        <NavLink to="/kitchen" icon={ChefHat} active={location.pathname === '/kitchen'}>{language === 'ro' ? 'Bucătărie' : 'Kitchen'}</NavLink>
                                    )}

                                    {(role === 'WORKER' || role === 'MANAGER') && (
                                        <NavLink to="/inventory" icon={Warehouse} active={location.pathname === '/inventory'}>{language === 'ro' ? 'Inventar' : 'Inventory'}</NavLink>
                                    )}

                                    {(role === 'MANAGER') && (
                                        <NavLink to="/admin" icon={Settings} active={location.pathname === '/admin'}>Admin</NavLink>
                                    )}
                                    {role ?(
                                            <div className="mt-2 border-t border-gray-200 dark:border-slate-700 pt-3 flex flex-col gap-2">
                                                {/* Link Puncte Loialitate - doar pentru STUDENT */}
                                                {role === 'STUDENT' && (
                                                    <>
                                                        <Link to="/loyalty" className="flex items-center gap-2 px-3 py-1.5 bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700 hover:bg-amber-100 dark:hover:bg-amber-900/50 rounded-full text-amber-700 dark:text-amber-400 text-sm font-semibold shadow-sm transition-colors cursor-pointer" title="Vezi Detalii Puncte">
                                                            <Gift size={14} />
                                                            <span>{points}</span>
                                                        </Link>
                                                    </>
                                                )}
                                                
                                                {/* Buton Dark Mode Toggle - Mobile */}
                                                <button
                                                    onClick={toggleTheme}
                                                    className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors text-sm font-medium hover:bg-brand-50 dark:hover:bg-slate-700 px-3 py-1.5 rounded-lg"
                                                    title={theme === 'light' ? (language === 'ro' ? 'Mod întunecat' : 'Dark mode') : (language === 'ro' ? 'Mod luminos' : 'Light mode')}
                                                >
                                                    {theme === 'light' ? (
                                                        <>
                                                            <Moon size={18} />
                                                            <span>{language === 'ro' ? 'Mod întunecat' : 'Dark mode'}</span>
                                                        </>
                                                    ) : (
                                                        <>
                                                            <Sun size={18} />
                                                            <span>{language === 'ro' ? 'Mod luminos' : 'Light mode'}</span>
                                                        </>
                                                    )}
                                                </button>
                                                
                                                {/* Buton Schimbare Limbă - Mobile */}
                                                <button
                                                    onClick={toggleLanguage}
                                                    className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors text-sm font-medium hover:bg-brand-50 dark:hover:bg-slate-700 px-3 py-1.5 rounded-lg"
                                                    title={language === 'ro' ? 'Switch to English' : 'Schimbă în Română'}
                                                >
                                                    <Languages size={18} />
                                                    <span className="uppercase">{language === 'ro' ? 'RO → EN' : 'EN → RO'}</span>
                                                </button>
                                                
                                                <Link 
                                                    to="/profile"
                                                    className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors text-sm font-medium hover:bg-blue-50 dark:hover:bg-slate-700 px-3 py-1.5 rounded-lg"
                                                    title={language === 'ro' ? 'Profilul Meu' : 'My Profile'}
                                                    >
                                                        {user?.profilePictureUrl ? (
                                                            <img
                                                                src={user.profilePictureUrl}
                                                                alt={language === 'ro' ? 'Profil' : 'Profile'}
                                                                className="w-6 h-6 rounded-full object-cover border border-gray-200 dark:border-slate-600"
                                                            />
                                                        ) : (
                                                            <User size={18} />
                                                        )}
                                                        <span className="hidden sm:inline">{language === 'ro' ? 'Profil' : 'Profile'}</span>
                                                    </Link>
                                                <button
                                                    onClick={onLogout}
                                                    className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-red-600 dark:hover:text-red-400 transition-colors text-sm font-medium hover:bg-red-50 dark:hover:bg-red-900/30 px-3 py-1.5 rounded-lg"
                                                    title={language === 'ro' ? 'Deconectare' : 'Logout'}
                                                >
                                                    <LogOut size={18} />
                                                    {language === 'ro' ? 'Deconectare' : 'Logout'}
                                                </button>
                                            </div>
                                        )
                                        : (
                                        <div className="mt-2 border-t border-gray-200 dark:border-slate-700 pt-3 flex flex-col gap-2">
                                            {/* Buton Dark Mode Toggle - Mobile pentru utilizatori neautentificați */}
                                            <button
                                                onClick={toggleTheme}
                                                className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors text-sm font-medium hover:bg-brand-50 dark:hover:bg-slate-700 px-3 py-1.5 rounded-lg"
                                                title={theme === 'light' ? (language === 'ro' ? 'Mod întunecat' : 'Dark mode') : (language === 'ro' ? 'Mod luminos' : 'Light mode')}
                                            >
                                                {theme === 'light' ? (
                                                    <>
                                                        <Moon size={18} />
                                                        <span>{language === 'ro' ? 'Mod întunecat' : 'Dark mode'}</span>
                                                    </>
                                                ) : (
                                                    <>
                                                        <Sun size={18} />
                                                        <span>{language === 'ro' ? 'Mod luminos' : 'Light mode'}</span>
                                                    </>
                                                )}
                                            </button>
                                            
                                            {/* Buton Schimbare Limbă - Mobile */}
                                            <button
                                                onClick={toggleLanguage}
                                                className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors text-sm font-medium hover:bg-brand-50 dark:hover:bg-slate-700 px-3 py-1.5 rounded-lg"
                                                title={language === 'ro' ? 'Switch to English' : 'Schimbă în Română'}
                                            >
                                                <Languages size={18} />
                                                <span className="uppercase">{language === 'ro' ? 'RO → EN' : 'EN → RO'}</span>
                                            </button>
                                            
                                            <Link
                                                to="/login"
                                                onClick={() => setIsMenuOpen(false)}
                                                className="px-3 py-2 rounded-lg text-sm font-semibold bg-brand-600 text-white hover:bg-brand-700"
                                            >
                                                Login
                                            </Link>
                                            <Link
                                                to="/register"
                                                onClick={() => setIsMenuOpen(false)}
                                                className="px-3 py-2 rounded-lg text-sm font-semibold bg-brand-600 text-white hover:bg-brand-700"
                                            >
                                                Sign Up
                                            </Link>
                                        </div>
                                    )}
                                </nav>
                            </div>
                        </div>
                        {/* Zona Utilizator / Login */}
                        <div className="hidden md:flex items-center gap-4">
                            {role ? (
                                <div className="flex items-center gap-3">
                                    {/* Link Puncte Loialitate - doar pentru STUDENT */}
                                    {role === 'STUDENT' && (
                                        <>
                                            <Link to="/loyalty" className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700 hover:bg-amber-100 dark:hover:bg-amber-900/50 rounded-full text-amber-700 dark:text-amber-400 text-sm font-semibold shadow-sm transition-colors cursor-pointer" title="Vezi Detalii Puncte">
                                                <Gift size={14} />
                                                <span>{points}</span>
                                            </Link>
                                            <div className="h-6 w-px bg-gray-300 dark:bg-slate-600 mx-1"></div>
                                        </>
                                    )}
                                    
                                    {/* Buton Dark Mode Toggle */}
                                    <button
                                        onClick={toggleTheme}
                                        className="flex items-center justify-center p-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors hover:bg-brand-50 dark:hover:bg-slate-700 rounded-lg"
                                        title={theme === 'light' ? (language === 'ro' ? 'Mod întunecat' : 'Dark mode') : (language === 'ro' ? 'Mod luminos' : 'Light mode')}
                                        aria-label="Toggle theme"
                                    >
                                        {theme === 'light' ? (
                                            <Moon size={20} />
                                        ) : (
                                            <Sun size={20} />
                                        )}
                                    </button>
                                    
                                    {/* Buton Schimbare Limbă */}
                                    <button
                                        onClick={toggleLanguage}
                                        className="flex items-center justify-center gap-1.5 px-3 py-1.5 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors hover:bg-brand-50 dark:hover:bg-slate-700 rounded-lg font-medium text-sm"
                                        title={language === 'ro' ? 'Switch to English' : 'Schimbă în Română'}
                                        aria-label="Toggle language"
                                    >
                                        <Languages size={18} />
                                        <span className="uppercase">{language}</span>
                                    </button>
                                    
                                    <Link
                                        to="/profile"
                                        className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors text-sm font-medium hover:bg-brand-50 dark:hover:bg-slate-700 px-3 py-1.5 rounded-lg"
                                        title={language === 'ro' ? 'Profilul Meu' : 'My Profile'}
                                        >
                                        {user?.profilePictureUrl ? (
                                            <img
                                                src={user.profilePictureUrl}
                                                alt={language === 'ro' ? 'Profil' : 'Profile'}
                                                className="w-6 h-6 rounded-full object-cover border border-gray-200 dark:border-slate-600"
                                            />
                                        ) : (
                                            <User size={18} />
                                        )}
                                            <span className="hidden sm:inline">{language === 'ro' ? 'Profil' : 'Profile'}</span>
                                        </Link>

                                    <button
                                        onClick={onLogout}
                                        className="flex items-center gap-2 text-gray-500 dark:text-slate-300 hover:text-red-600 dark:hover:text-red-400 transition-colors text-sm font-medium hover:bg-red-50 dark:hover:bg-red-900/30 px-3 py-1.5 rounded-lg"
                                        title={language === 'ro' ? 'Deconectare' : 'Logout'}
                                    >
                                        <LogOut size={18} />
                                    </button>
                                </div>
                            ) : (
                                <div className="flex gap-3 items-center">
                                    {/* Buton Dark Mode Toggle pentru utilizatori neautentificați */}
                                    <button
                                        onClick={toggleTheme}
                                        className="flex items-center justify-center p-2 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors hover:bg-brand-50 dark:hover:bg-slate-700 rounded-lg"
                                        title={theme === 'light' ? (language === 'ro' ? 'Mod întunecat' : 'Dark mode') : (language === 'ro' ? 'Mod luminos' : 'Light mode')}
                                        aria-label="Toggle theme"
                                    >
                                        {theme === 'light' ? (
                                            <Moon size={20} />
                                        ) : (
                                            <Sun size={20} />
                                        )}
                                    </button>
                                    
                                    {/* Buton Schimbare Limbă */}
                                    <button
                                        onClick={toggleLanguage}
                                        className="flex items-center justify-center gap-1.5 px-3 py-1.5 text-gray-500 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 transition-colors hover:bg-brand-50 dark:hover:bg-slate-700 rounded-lg font-medium text-sm"
                                        title={language === 'ro' ? 'Switch to English' : 'Schimbă în Română'}
                                        aria-label="Toggle language"
                                    >
                                        <Languages size={18} />
                                        <span className="uppercase">{language}</span>
                                    </button>
                                    
                                    <Link to="/login" className="text-gray-600 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 font-medium text-sm px-3 py-2">Login</Link>
                                    <Link to="/register" className="bg-brand-600 hover:bg-brand-700 text-white px-5 py-2 rounded-full text-sm font-medium shadow-md shadow-brand-500/30 transition-all hover:scale-105">
                                        Sign Up
                                    </Link>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </header>
            
            <main className="flex-1 w-full mx-auto px-2 sm:px-4 md:px-6 lg:px-8 md:max-w-3xl lg:max-w-5xl xl:max-w-7xl py-8 animate-fade-in dark:text-slate-100">
                {children}
            </main>
        </div>
    )
}

export default function App() {
    const [token, setToken] = useState<string | null>(() => AuthApi.getToken())
    const [role, setRole] = useState<string | null>(null)
    const [user, setUser] = useState<any>(null)
    const [cart, setCart] = useState<CartItem[]>([])
    const [isRefreshing, setIsRefreshing] = useState<boolean>(() => !!AuthApi.getToken())
    const [reviewsModalItem, setReviewsModalItem] = useState<MenuItem | null>(null)
    const [menuRefreshTrigger, setMenuRefreshTrigger] = useState(0)
    const { language } = useLanguage()
    

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
            AuthApi.getMe().then(data => setUser(data)).catch(err => console.error("Eroare la preluarea datelor:", err))
        } else {
            setRole(null)
            setUser(null)
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
        alert(language === 'ro' ? "Plată realizată cu succes!" : "Payment successful!")
        window.location.href = '/orders'
    }

    return (
        <BrowserRouter>
            <Layout role={role} user={user} onLogout={handleLogout}>
                <Routes>
                    <Route path="/" element={
                        <>
                            <div className="mb-8 text-center md:text-left">
                                <h1 className="text-3xl md:text-4xl font-extrabold text-gray-900 dark:text-slate-100 tracking-tight">
                                    {language === 'ro' ? 'Meniu Delicios 🍔' : 'Delicious Menu 🍔'}
                                </h1>
                                <p className="text-gray-500 dark:text-slate-400 mt-2 text-lg">
                                    {language === 'ro' ? 'Comandă mâncarea preferată direct din campus.' : 'Order your favorite food directly from campus.'}
                                </p>
                            </div>
                            <MenuPage 
                                onAddToCart={addToCart} 
                                isLoggedIn={!!token}
                                onViewReviews={setReviewsModalItem}
                                refreshTrigger={menuRefreshTrigger}
                            />
                            {token && (
                                <OrderCart
                                    cart={cart}
                                    onClear={() => setCart([])}
                                    onUpdateQuantity={updateQuantity}
                                />
                            )}
                            <ReviewsModal
                                menuItem={reviewsModalItem as MenuItem}
                                isOpen={!!reviewsModalItem}
                                onClose={() => setReviewsModalItem(null)}
                                currentUserId={user?.id}
                                onReviewChange={() => setMenuRefreshTrigger(prev => prev + 1)}
                            />
                        </>
                    } />
                    
                    <Route path="/login" element={!token ? <LoginPage onLoggedIn={() => {
                        setToken(AuthApi.getToken());
                        localStorage.setItem('has_logged_out', '0');
                    }} /> : <Navigate to="/" />} />
                    <Route path="/register" element={!token ? 
                        <RegisterPage
                        initialRole={0} 
                        showRoleSelector={false}
                        onRegistered={() => {
                            setToken(AuthApi.getToken())
                            localStorage.setItem('has_logged_out', '0')
                        }}
                        /> : <Navigate to="/" />} />
                    
                    {/* Rute Protejate Utilizator */}
                    <Route path="/loyalty" element={token ? <LoyaltyPage /> : <Navigate to="/login" />} />
                    <Route path="/coupons" element={token ? <CouponsPage /> : <Navigate to="/login" />} />
                    <Route path="/orders" element={token ? <OrdersPage /> : <Navigate to="/login" />} />
                    
                    {/* Rute Protejate Staff */}
                    <Route path="/kitchen" element={(role === 'WORKER' || role === 'MANAGER') ? <KitchenDashboard /> : <Navigate to="/" />} />

                    <Route path="/admin" element={ role === 'MANAGER' ? <AdminPage /> : <Navigate to="/login" />}/>
                    <Route path="*" element={<Navigate to="/" />} />
                    <Route path="/kitchen/order/:id" element={(role === 'WORKER' || role === 'MANAGER') ? <KitchenOrderDetails /> : <Navigate to="/" />} />


                    <Route path="/inventory" element={(role === 'WORKER' || role === 'MANAGER') ? <InventoryPage /> : <Navigate to="/" />} />
                    <Route path="/admin/menu" element={(role === 'MANAGER') ? <MenuForm /> : <Navigate to="/" />} />
                    <Route path="/profile" element={token ? <ProfilePage /> : <Navigate to="/login" />} />
                </Routes>
                <PaymentResult onSuccess={onPaymentSuccess} />
            </Layout>
        </BrowserRouter>
    )
}
import { createContext, useContext, useEffect, useState, ReactNode } from 'react'

type Language = 'ro' | 'en'

interface LanguageContextType {
    language: Language
    toggleLanguage: () => void
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined)

export function LanguageProvider({ children }: { children: ReactNode }) {
    const [language, setLanguage] = useState<Language>(() => {
        const saved = localStorage.getItem('language')
        return (saved === 'en' || saved === 'ro') ? saved : 'ro'
    })

    useEffect(() => {
        localStorage.setItem('language', language)
    }, [language])

    const toggleLanguage = () => {
        setLanguage(prev => prev === 'ro' ? 'en' : 'ro')
    }

    return (
        <LanguageContext.Provider value={{ language, toggleLanguage }}>
            {children}
        </LanguageContext.Provider>
    )
}

export function useLanguage() {
    const context = useContext(LanguageContext)
    if (!context) {
        throw new Error('useLanguage must be used within LanguageProvider')
    }
    return context
}

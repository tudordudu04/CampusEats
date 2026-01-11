import React, { useState, useEffect } from 'react'
import { StarRating } from './StarRating'
import { ReviewsApi } from '../services/api'
import type { ReviewDto } from '../types'
import { useLanguage } from '../contexts/LanguageContext'

interface ReviewFormProps {
    menuItemId: string
    existingReview?: ReviewDto | null
    onSuccess?: () => void
    onCancel?: () => void
}

export const ReviewForm: React.FC<ReviewFormProps> = ({
    menuItemId,
    existingReview,
    onSuccess,
    onCancel
}) => {
    const [rating, setRating] = useState<number>(existingReview?.rating || 5)
    const [comment, setComment] = useState<string>(existingReview?.comment || '')
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const { language } = useLanguage()

    useEffect(() => {
        if (existingReview) {
            setRating(existingReview.rating)
            setComment(existingReview.comment || '')
        }
    }, [existingReview])

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setIsSubmitting(true)
        setError(null)

        try {
            if (existingReview) {
                await ReviewsApi.updateReview(existingReview.id, {
                    rating,
                    comment: comment.trim() || null
                })
            } else {
                await ReviewsApi.addReview({
                    menuItemId,
                    rating,
                    comment: comment.trim() || null
                })
            }
            onSuccess?.()
        } catch (err) {
            setError(err instanceof Error ? err.message : (language === 'ro' ? 'A apărut o eroare' : 'An error occurred'))
        } finally {
            setIsSubmitting(false)
        }
    }

    const handleDelete = async () => {
        if (!existingReview) return
        
        if (!confirm(language === 'ro' ? 'Sigur vrei să ștergi acest review?' : 'Are you sure you want to delete this review?')) return

        setIsSubmitting(true)
        setError(null)

        try {
            await ReviewsApi.deleteReview(existingReview.id)
            onSuccess?.()
        } catch (err) {
            setError(err instanceof Error ? err.message : (language === 'ro' ? 'A apărut o eroare' : 'An error occurred'))
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-lg border border-gray-100 dark:border-slate-700 p-6">
            <h3 className="text-lg font-bold text-gray-900 dark:text-slate-100 mb-4">
                {language === 'ro' 
                    ? (existingReview ? 'Editează Review-ul' : 'Adaugă Review')
                    : (existingReview ? 'Edit Review' : 'Add Review')
                }
            </h3>

            {error && (
                <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 rounded-lg">
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">
                        Rating
                    </label>
                    <StarRating
                        rating={rating}
                        editable
                        size="lg"
                        onChange={setRating}
                        showNumber
                    />
                </div>

                <div className="mb-4">
                    <label htmlFor="comment" className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">
                        {language === 'ro' ? 'Comentariu (opțional)' : 'Comment (optional)'}
                    </label>
                    <textarea
                        id="comment"
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        rows={4}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100"
                        placeholder={language === 'ro' ? 'Spune-ne ce părere ai despre acest produs...' : 'Tell us what you think about this product...'}
                    />
                </div>

                <div className="flex gap-2">
                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="flex-1 bg-gray-900 dark:bg-brand-700 hover:bg-brand-600 dark:hover:bg-brand-600 active:bg-brand-700 dark:active:bg-brand-800 text-white px-4 py-3 rounded-xl font-bold text-sm transition-all shadow-lg shadow-gray-200 dark:shadow-none hover:shadow-brand-500/30 dark:hover:shadow-none disabled:bg-gray-400 disabled:cursor-not-allowed disabled:shadow-none"
                    >
                        {isSubmitting 
                            ? (language === 'ro' ? 'Se salvează...' : 'Saving...') 
                            : (existingReview 
                                ? (language === 'ro' ? 'Actualizează' : 'Update') 
                                : (language === 'ro' ? 'Adaugă Review' : 'Add Review')
                            )
                        }
                    </button>

                    {onCancel && (
                        <button
                            type="button"
                            onClick={onCancel}
                            className="px-4 py-3 border border-gray-200 dark:border-slate-600 rounded-xl hover:bg-gray-50 dark:hover:bg-slate-700 transition font-medium text-sm text-gray-700 dark:text-slate-300"
                        >
                            {language === 'ro' ? 'Anulează' : 'Cancel'}
                        </button>
                    )}

                    {existingReview && (
                        <button
                            type="button"
                            onClick={handleDelete}
                            disabled={isSubmitting}
                            className="px-4 py-3 bg-red-600 dark:bg-red-700 text-white rounded-xl hover:bg-red-700 dark:hover:bg-red-600 disabled:bg-gray-400 disabled:cursor-not-allowed transition-all font-bold text-sm shadow-lg hover:shadow-red-500/30"
                        >
                            {language === 'ro' ? 'Șterge' : 'Delete'}
                        </button>
                    )}
                </div>
            </form>
        </div>
    )
}

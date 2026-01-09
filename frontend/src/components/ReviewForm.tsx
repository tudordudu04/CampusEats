import React, { useState, useEffect } from 'react'
import { StarRating } from './StarRating'
import { ReviewsApi } from '../services/api'
import type { ReviewDto } from '../types'

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
            setError(err instanceof Error ? err.message : 'A apărut o eroare')
        } finally {
            setIsSubmitting(false)
        }
    }

    const handleDelete = async () => {
        if (!existingReview) return
        
        if (!confirm('Sigur vrei să ștergi acest review?')) return

        setIsSubmitting(true)
        setError(null)

        try {
            await ReviewsApi.deleteReview(existingReview.id)
            onSuccess?.()
        } catch (err) {
            setError(err instanceof Error ? err.message : 'A apărut o eroare')
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <div className="bg-white rounded-xl shadow-lg border border-gray-100 p-6">
            <h3 className="text-lg font-bold text-gray-900 mb-4">
                {existingReview ? 'Editează Review-ul' : 'Adaugă Review'}
            </h3>

            {error && (
                <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg">
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-2">
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
                    <label htmlFor="comment" className="block text-sm font-medium text-gray-700 mb-2">
                        Comentariu (opțional)
                    </label>
                    <textarea
                        id="comment"
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        rows={4}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                        placeholder="Spune-ne ce părere ai despre acest produs..."
                    />
                </div>

                <div className="flex gap-2">
                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="flex-1 bg-gray-900 hover:bg-brand-600 active:bg-brand-700 text-white px-4 py-3 rounded-xl font-bold text-sm transition-all shadow-lg shadow-gray-200 hover:shadow-brand-500/30 disabled:bg-gray-400 disabled:cursor-not-allowed disabled:shadow-none"
                    >
                        {isSubmitting ? 'Se salvează...' : existingReview ? 'Actualizează' : 'Adaugă Review'}
                    </button>

                    {onCancel && (
                        <button
                            type="button"
                            onClick={onCancel}
                            className="px-4 py-3 border border-gray-200 rounded-xl hover:bg-gray-50 transition font-medium text-sm text-gray-700"
                        >
                            Anulează
                        </button>
                    )}

                    {existingReview && (
                        <button
                            type="button"
                            onClick={handleDelete}
                            disabled={isSubmitting}
                            className="px-4 py-3 bg-red-600 text-white rounded-xl hover:bg-red-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-all font-bold text-sm shadow-lg hover:shadow-red-500/30"
                        >
                            Șterge
                        </button>
                    )}
                </div>
            </form>
        </div>
    )
}

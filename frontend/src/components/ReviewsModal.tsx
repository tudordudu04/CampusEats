import React, { useEffect, useState } from 'react'
import { StarRating } from './StarRating'
import { ReviewForm } from './ReviewForm'
import { ReviewsApi } from '../services/api'
import type { MenuItem, ReviewDto } from '../types'
import { X, Edit2 } from 'lucide-react'

interface ReviewsModalProps {
    menuItem: MenuItem
    isOpen: boolean
    onClose: () => void
    currentUserId?: string
    onReviewChange?: () => void
}

export const ReviewsModal: React.FC<ReviewsModalProps> = ({
    menuItem,
    isOpen,
    onClose,
    currentUserId,
    onReviewChange
}) => {
    const [reviews, setReviews] = useState<ReviewDto[]>([])
    const [myReview, setMyReview] = useState<ReviewDto | null>(null)
    const [loading, setLoading] = useState(false)
    const [showForm, setShowForm] = useState(false)

    useEffect(() => {
        if (isOpen && menuItem) {
            loadReviews()
        }
    }, [isOpen, menuItem?.id])

    const loadReviews = async () => {
        setLoading(true)
        try {
            const [allReviews, userReview] = await Promise.all([
                ReviewsApi.getMenuItemReviews(menuItem.id),
                currentUserId ? ReviewsApi.getMyReview(menuItem.id).catch(() => null) : Promise.resolve(null)
            ])
            setReviews(allReviews)
            setMyReview(userReview)
        } catch (error) {
            console.error('Error loading reviews:', error)
        } finally {
            setLoading(false)
        }
    }

    const handleReviewSuccess = () => {
        setShowForm(false)
        loadReviews()
        onReviewChange?.()
    }

    if (!isOpen || !menuItem) return null

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-xl shadow-2xl max-w-3xl w-full max-h-[90vh] overflow-hidden flex flex-col">
                {/* Header */}
                <div className="flex items-center justify-between p-6 border-b">
                    <div>
                        <h2 className="text-2xl font-bold text-gray-900">{menuItem.name}</h2>
                        <div className="flex items-center gap-2 mt-1">
                            {menuItem.averageRating !== null && menuItem.averageRating > 0 ? (
                                <>
                                    <StarRating rating={menuItem.averageRating} showNumber />
                                    <span className="text-sm text-gray-500">
                                        ({menuItem.reviewCount} {menuItem.reviewCount === 1 ? 'review' : 'reviews'})
                                    </span>
                                </>
                            ) : (
                                <span className="text-sm text-gray-500">Niciun review încă</span>
                            )}
                        </div>
                    </div>
                    <button
                        onClick={onClose}
                        className="p-2 hover:bg-gray-100 rounded-lg transition"
                    >
                        <X size={24} />
                    </button>
                </div>

                {/* Content */}
                <div className="flex-1 overflow-y-auto p-6">
                    {/* My Review Section */}
                    {currentUserId && (
                        <div className="mb-6">
                            {showForm || myReview ? (
                                <ReviewForm
                                    menuItemId={menuItem.id}
                                    existingReview={myReview}
                                    onSuccess={handleReviewSuccess}
                                    onCancel={() => setShowForm(false)}
                                />
                            ) : (
                                <button
                                    onClick={() => setShowForm(true)}
                                    className="w-full py-3 bg-gray-900 hover:bg-brand-600 active:bg-brand-700 text-white rounded-xl font-bold text-sm transition-all shadow-lg shadow-gray-200 hover:shadow-brand-500/30"
                                >
                                    Adaugă Review
                                </button>
                            )}
                        </div>
                    )}

                    {/* Reviews List */}
                    <div className="space-y-4">
                        <h3 className="text-lg font-semibold text-gray-900">
                            Toate Review-urile ({reviews.length})
                        </h3>

                        {loading ? (
                            <div className="flex justify-center py-8">
                                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                            </div>
                        ) : reviews.length === 0 ? (
                            <div className="text-center py-8 text-gray-500">
                                Niciun review încă. Fii primul care lasă un review!
                            </div>
                        ) : (
                            reviews.map((review) => (
                                <div
                                    key={review.id}
                                    className={`p-4 rounded-xl border transition-all ${
                                        review.userId === currentUserId
                                            ? 'bg-brand-50 border-brand-200 shadow-sm'
                                            : 'bg-gray-50 border-gray-200'
                                    }`}
                                >
                                    <div className="flex items-start justify-between mb-2">
                                        <div>
                                            <div className="font-semibold text-gray-900">
                                                {review.userName}
                                                {review.userId === currentUserId && (
                                                    <span className="ml-2 text-xs text-brand-600 font-normal">
                                                        (Tu)
                                                    </span>
                                                )}
                                            </div>
                                            <StarRating rating={review.rating} size="sm" />
                                        </div>
                                        <div className="text-xs text-gray-500">
                                            {new Date(review.createdAtUtc).toLocaleDateString('ro-RO')}
                                            {review.createdAtUtc !== review.updatedAtUtc && (
                                                <span className="ml-1">(editat)</span>
                                            )}
                                        </div>
                                    </div>
                                    {review.comment && (
                                        <p className="text-gray-700 text-sm">{review.comment}</p>
                                    )}
                                </div>
                            ))
                        )}
                    </div>
                </div>
            </div>
        </div>
    )
}

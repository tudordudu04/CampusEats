import React from 'react'

interface StarRatingProps {
    rating: number
    maxRating?: number
    size?: 'sm' | 'md' | 'lg'
    showNumber?: boolean
    editable?: boolean
    onChange?: (rating: number) => void
}

export const StarRating: React.FC<StarRatingProps> = ({
    rating,
    maxRating = 5,
    size = 'md',
    showNumber = false,
    editable = false,
    onChange
}) => {
    const [hoverRating, setHoverRating] = React.useState<number | null>(null)

    const sizeClasses = {
        sm: 'w-4 h-4',
        md: 'w-5 h-5',
        lg: 'w-6 h-6'
    }

    const handleClick = (value: number) => {
        if (editable && onChange) {
            onChange(value)
        }
    }

    const renderStar = (index: number) => {
        const starValue = index + 1
        const displayRating = hoverRating !== null ? hoverRating : rating
        const fillPercentage = Math.max(0, Math.min(1, displayRating - index)) * 100

        return (
            <div
                key={index}
                className={`relative inline-block ${editable ? 'cursor-pointer' : ''}`}
                onMouseEnter={() => editable && setHoverRating(starValue)}
                onMouseLeave={() => editable && setHoverRating(null)}
                onClick={() => handleClick(starValue)}
            >
                {/* Empty star (background) */}
                <svg
                    className={`${sizeClasses[size]} text-gray-300`}
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                >
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>

                {/* Filled star (overlay) */}
                <div
                    className="absolute inset-0 overflow-hidden"
                    style={{ width: `${fillPercentage}%` }}
                >
                    <svg
                        className={`${sizeClasses[size]} text-yellow-400`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                        xmlns="http://www.w3.org/2000/svg"
                    >
                        <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                    </svg>
                </div>
            </div>
        )
    }

    return (
        <div className="flex items-center gap-1">
            <div className="flex">
                {Array.from({ length: maxRating }, (_, i) => renderStar(i))}
            </div>
            {showNumber && (
                <span className="ml-1 text-sm text-gray-600">
                    {rating.toFixed(1)}
                </span>
            )}
        </div>
    )
}

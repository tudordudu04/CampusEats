import { useEffect, useMemo, useState } from 'react'
import { AuthApi, type UserDto } from '../services/api'

type RoleFilter = {
    STUDENT: boolean
    WORKER: boolean
    MANAGER: boolean
}

type SortOrder = 'asc' | 'desc'

export default function UserManagement() {
    const [users, setUsers] = useState<UserDto[]>([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const [roleFilter, setRoleFilter] = useState<RoleFilter>({
        STUDENT: true,
        WORKER: true,
        MANAGER: true,
    })

    const [sortOrder, setSortOrder] = useState<SortOrder>('asc')

    const [confirmId, setConfirmId] = useState<string | null>(null)
    const [deleteLoading, setDeleteLoading] = useState(false)
    const [deleteError, setDeleteError] = useState<string | null>(null)

    useEffect(() => {
        void loadUsers()
    }, [])

    async function loadUsers() {
        try {
            setLoading(true)
            setError(null)
            const data = await AuthApi.getAllUsers()
            setUsers(data)
        } catch (err: any) {
            setError(err.message || 'Failed to load users')
        } finally {
            setLoading(false)
        }
    }

    function toggleRole(role: keyof RoleFilter) {
        setRoleFilter(prev => ({ ...prev, [role]: !prev[role] }))
    }

    function toggleSortOrder() {
        setSortOrder(prev => (prev === 'asc' ? 'desc' : 'asc'))
    }

    const visibleUsers = useMemo(() => {
        const allowedRoles = Object.entries(roleFilter)
            .filter(([, enabled]) => enabled)
            .map(([role]) => role)

        let filtered = users.filter(u => allowedRoles.includes(u.role))

        filtered = filtered.sort((a, b) => {
            const nameA = a.name.toLocaleLowerCase()
            const nameB = b.name.toLocaleLowerCase()
            if (nameA < nameB) return sortOrder === 'asc' ? -1 : 1
            if (nameA > nameB) return sortOrder === 'asc' ? 1 : -1
            return 0
        })

        return filtered
    }, [users, roleFilter, sortOrder])

    async function confirmDelete() {
        if (!confirmId) return
        setDeleteLoading(true)
        setDeleteError(null)
        try {
            await AuthApi.deleteUser(confirmId)
            setUsers(prev => prev.filter(u => u.id !== confirmId))
            setConfirmId(null)
        } catch (err: any) {
            setDeleteError(err.message || 'Failed to delete user')
        } finally {
            setDeleteLoading(false)
        }
    }

    const roleLabel = (role: string) => {
        switch (role) {
            case 'STUDENT': return 'Student'
            case 'WORKER': return 'Worker'
            case 'MANAGER': return 'Manager'
            default: return role
        }
    }

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between gap-4">
                <div>
                    <h2 className="text-xl font-semibold mb-1">Manage users</h2>
                    <p className="text-sm text-gray-500">
                        Filter by role, sort by name, and delete accounts.
                    </p>
                </div>

                <button
                    onClick={() => void loadUsers()}
                    disabled={loading}
                    className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 bg-white"
                >
                    {loading ? 'Refreshing…' : 'Refresh'}
                </button>
            </div>

            {/* Filters */}
            <div className="flex flex-wrap items-center gap-4">
                <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-gray-700">Filter roles:</span>
                    <label className="inline-flex items-center gap-1 text-sm">
                        <input
                            type="checkbox"
                            checked={roleFilter.STUDENT}
                            onChange={() => toggleRole('STUDENT')}
                        />
                        <span>Student</span>
                    </label>
                    <label className="inline-flex items-center gap-1 text-sm">
                        <input
                            type="checkbox"
                            checked={roleFilter.WORKER}
                            onChange={() => toggleRole('WORKER')}
                        />
                        <span>Worker</span>
                    </label>
                    <label className="inline-flex items-center gap-1 text-sm">
                        <input
                            type="checkbox"
                            checked={roleFilter.MANAGER}
                            onChange={() => toggleRole('MANAGER')}
                        />
                        <span>Manager</span>
                    </label>
                </div>

                <button
                    onClick={toggleSortOrder}
                    className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 bg-white"
                >
                    Sort name: {sortOrder === 'asc' ? 'A→Z' : 'Z→A'}
                </button>
            </div>

            {error && (
                <div className="text-sm text-red-600">
                    {error}
                </div>
            )}

            {/* Users list */}
            <div className="border border-gray-200 rounded-xl overflow-hidden">
                <table className="min-w-full text-sm">
                    <thead className="bg-gray-50">
                    <tr>
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">Name</th>
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">Email</th>
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">Role</th>
                        <th className="px-4 py-2 text-right font-semibold text-gray-700">Actions</th>
                    </tr>
                    </thead>
                    <tbody>
                    {visibleUsers.length === 0 && (
                        <tr>
                            <td colSpan={4} className="px-4 py-4 text-center text-gray-500">
                                No users found.
                            </td>
                        </tr>
                    )}

                    {visibleUsers.map(user => (
                        <tr key={user.id} className="border-t border-gray-100">
                            <td className="px-4 py-2">{user.name}</td>
                            <td className="px-4 py-2 text-gray-600">{user.email}</td>
                            <td className="px-4 py-2">
                                    <span className="inline-flex px-2 py-0.5 rounded-full text-xs bg-gray-100 text-gray-700">
                                        {roleLabel(user.role)}
                                    </span>
                            </td>
                            <td className="px-4 py-2 text-right">
                                <button
                                    onClick={() => {
                                        setDeleteError(null)
                                        setConfirmId(user.id)
                                    }}
                                    className="px-3 py-1.5 text-xs rounded-lg bg-red-600 text-white hover:bg-red-700"
                                >
                                    Delete
                                </button>
                            </td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            </div>

            {/* Delete confirm popup */}
            {confirmId && (
                <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
                    <div className="bg-white rounded-xl shadow-lg p-6 w-full max-w-sm">
                        <h3 className="text-lg font-semibold mb-2">Delete user</h3>
                        <p className="text-sm text-gray-600 mb-4">
                            Are you sure you want to permanently delete this user account?
                            This action cannot be undone.
                        </p>

                        {deleteError && (
                            <div className="mb-3 text-sm text-red-600">
                                {deleteError}
                            </div>
                        )}

                        <div className="flex justify-end gap-3">
                            <button
                                onClick={() => !deleteLoading && setConfirmId(null)}
                                className="px-4 py-2 text-sm rounded-lg border border-gray-300 bg-white"
                                disabled={deleteLoading}
                            >
                                Cancel
                            </button>
                            <button
                                onClick={() => void confirmDelete()}
                                className="px-4 py-2 text-sm rounded-lg bg-red-600 text-white disabled:opacity-70"
                                disabled={deleteLoading}
                            >
                                {deleteLoading ? 'Deleting…' : 'Delete'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}

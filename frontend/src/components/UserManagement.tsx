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
                    <h2 className="text-xl font-semibold mb-1 dark:text-slate-100">Manage users</h2>
                    <p className="text-sm text-gray-500 dark:text-slate-400">
                        Filter by role, sort by name, and delete accounts.
                    </p>
                </div>

                <button
                    onClick={() => void loadUsers()}
                    disabled={loading}
                    className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100 hover:bg-gray-50 dark:hover:bg-slate-600"
                >
                    {loading ? 'Refreshing…' : 'Refresh'}
                </button>
            </div>

            {/* Filters */}
            <div className="flex flex-wrap items-center gap-4">
                <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-gray-700 dark:text-slate-300">Filter roles:</span>
                    <label className="inline-flex items-center gap-1 text-sm dark:text-slate-300">
                        <input
                            type="checkbox"
                            checked={roleFilter.STUDENT}
                            onChange={() => toggleRole('STUDENT')}
                        />
                        <span>Student</span>
                    </label>
                    <label className="inline-flex items-center gap-1 text-sm dark:text-slate-300">
                        <input
                            type="checkbox"
                            checked={roleFilter.WORKER}
                            onChange={() => toggleRole('WORKER')}
                        />
                        <span>Worker</span>
                    </label>
                    <label className="inline-flex items-center gap-1 text-sm dark:text-slate-300">
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
                    className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100 hover:bg-gray-50 dark:hover:bg-slate-600"
                >
                    Sort name: {sortOrder === 'asc' ? 'A→Z' : 'Z→A'}
                </button>
            </div>

            {error && (
                <div className="text-sm text-red-600 dark:text-red-400">
                    {error}
                </div>
            )}

            {/* Users list */}
            <div className="border border-gray-200 dark:border-slate-700 rounded-xl">
                <div className="overflow-x-auto">
                    <table className="min-w-[480px] w-full text-sm">
                        <thead className="bg-gray-50 dark:bg-slate-900">
                        <tr>
                            <th className="px-4 py-2 text-left font-semibold text-gray-700 dark:text-slate-300">Name</th>
                            <th className="px-4 py-2 text-left font-semibold text-gray-700 dark:text-slate-300">Email</th>
                            <th className="px-4 py-2 text-left font-semibold text-gray-700 dark:text-slate-300">Role</th>
                            <th className="px-4 py-2 text-right font-semibold text-gray-700 dark:text-slate-300">Actions</th>
                        </tr>
                        </thead>
                        <tbody>
                        {visibleUsers.length === 0 && (
                            <tr>
                                <td colSpan={4} className="px-4 py-4 text-center text-gray-500 dark:text-slate-400">
                                    No users found.
                                </td>
                            </tr>
                        )}

                        {visibleUsers.map(user => (
                            <tr key={user.id} className="border-t border-gray-100 dark:border-slate-700">
                                <td className="px-4 py-2 dark:text-slate-200">{user.name}</td>
                                <td className="px-4 py-2 text-gray-600 dark:text-slate-400">{user.email}</td>
                                <td className="px-4 py-2">
                                    <span className="inline-flex px-2 py-0.5 rounded-full text-xs bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-slate-300">
                                        {roleLabel(user.role)}
                                    </span>
                                </td>
                                <td className="px-4 py-2 text-right">
                                    <button
                                        onClick={() => {
                                            setDeleteError(null)
                                            setConfirmId(user.id)
                                        }}
                                        className="px-3 py-1.5 text-xs rounded-lg bg-red-600 dark:bg-red-700 text-white hover:bg-red-700 dark:hover:bg-red-600"
                                    >
                                        Delete
                                    </button>
                                </td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Delete confirm popup */}
            {confirmId && (
                <div className="fixed inset-0 bg-black/30 dark:bg-black/60 flex items-center justify-center z-50">
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-lg p-6 w-full max-w-sm">
                        <h3 className="text-lg font-semibold mb-2 dark:text-slate-100">Delete user</h3>
                        <p className="text-sm text-gray-600 dark:text-slate-400 mb-4">
                            Are you sure you want to permanently delete this user account?
                            This action cannot be undone.
                        </p>

                        {deleteError && (
                            <div className="mb-3 text-sm text-red-600 dark:text-red-400">
                                {deleteError}
                            </div>
                        )}

                        <div className="flex justify-end gap-3">
                            <button
                                onClick={() => !deleteLoading && setConfirmId(null)}
                                className="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100 hover:bg-gray-50 dark:hover:bg-slate-600"
                                disabled={deleteLoading}
                            >
                                Cancel
                            </button>
                            <button
                                onClick={() => void confirmDelete()}
                                className="px-4 py-2 text-sm rounded-lg bg-red-600 dark:bg-red-700 text-white hover:bg-red-700 dark:hover:bg-red-600 disabled:opacity-70"
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
import { useUsers } from "../hooks/useUsers";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("es-AR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

export function UsersListPage() {
  const { users, loading, error, reload } = useUsers();

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-x1 font-semibold text-gray-900">Users</h1>
        <button
          onClick={reload}
          className="rounded-lg border border-gray-300 bg-white px-3 py-1.5 text-sm
                    font-medium text-gray-700 hover:bg-gray-50 transition-colors"
        >
          Refresh
        </button>
      </div>

      {error && !loading && (
        <div
          role="alert"
          className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3"
        >
          {error}
        </div>
      )}

      {!loading && !error && users.length === 0 && (
        <div className="text-sm text-gray-500 py-8 text-center border border-dashed border-gray-300 rounded-lg">
          No users found.
        </div>
      )}

      {!loading && !error && users.length > 0 && (
        <div className="overflow-x-auto border border-gray-200 rounded-lg bg-white">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200 bg-gray-50 text-left text-gray-600">
                <th className="px-4 py-2.5 font-medium">Full Name</th>
                <th className="px-4 py-2.5 font-medium">Email</th>
                <th className="px-4 py-2.5 font-medium">Role</th>
                <th className="px-4 py-2.5 font-medium">Status</th>
                <th className="px-4 py-2.5 font-medium">Created At</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr
                  key={u.id}
                  className="border-b border-gray-100 last:border-0 hover:bg-gray-50"
                >
                  <td className="px-4 py-2.5 text-gray-900">
                    <div className="flex items-center gap-2">
                      {u.fullname}
                      {!u.emailVerified && (
                        <span className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded px-1.5 py-0.5">
                          Unverified
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-2.5 text-gray-600">{u.email}</td>
                  <td className="px-4 py-2.5 text-gray-600">{u.role}</td>
                  <td className="px-4 py-2.5">
                    <span
                      className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${
                        u.active
                          ? "bg-green-50 text-green-700 border border-green-200"
                          : "bg-gray-100 text-gray-500 border border-gray-200"
                      }`}
                    >
                      {u.active ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-gray-500">
                    {formatDate(u.createdAt)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

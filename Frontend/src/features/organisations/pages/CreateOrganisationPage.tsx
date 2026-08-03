import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { Roles } from "../../users/roles";
import { useCreateOrganisation } from "../hooks/useCreateOrganisations";
import { type Organisation } from "../../../api/endpoints/organisations";

export function CreateOrganisationPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { submit, submitting, error } = useCreateOrganisation();

  const [name, setName] = useState("");
  const [cuit, setCuit] = useState("");
  const [adminFullname, setAdminFullname] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [created, setCreated] = useState<Organisation | null>(null);

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">
        You do not have permission to create organisations.
      </div>
    );
  }

  if (created) {
    return (
      <div className="max-w-md">
        <div className="bg-green-50 border border-green-200 rounded-lg p-6">
          <h1 className="text-lg font-semibold text-gray-900 mb-2">
            Organisation Created.
          </h1>
          <p className="text-sm text-gray-700 mb-1">
            <span className="font-medium">{created.name}</span> has been
            created.
          </p>
          <p className="text-sm text-gray-600 mb-4">
            An invitation has been sent to{" "}
            <span className="font-medium">{adminEmail}</span> for the
            administrator to set up their password.
          </p>
          <div className="flex gap-3">
            <button
              onClick={() => {
                setCreated(null);
                setName("");
                setCuit("");
                setAdminFullname("");
                setAdminEmail("");
              }}
              className="rounded-lg bg-blue-600 text-white text-sm font-medium px-4 py-2.5
                         hover:bg-blue-700 transition-colors"
            >
              Create Another
            </button>
            <button
              onClick={() => navigate("/")}
              className="rounded-lg border border-gray-300 bg-white text-sm font-medium px-4 py-2.5
                         text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Go to Home
            </button>
          </div>
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const result = await submit({
      name,
      cuit: cuit.trim() === "" ? null : cuit.trim(),
      adminFullname,
      adminEmail,
    });
    if (result) setCreated(result);
  };

  return (
    <div className="max-w-md">
      <h1 className="text-xl font-semibold text-gray-900 mb-1">
        Create Organisation
      </h1>
      <p className="text-sm text-gray-600 mb-5">
        The organisation will be created along with its administrator, who will
        receive an invitation by email.
      </p>

      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <div>
          <label
            htmlFor="name"
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            Organisation Name
          </label>
          <input
            id="name"
            type="text"
            required
            maxLength={100}
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={submitting}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm
                       focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                       disabled:bg-gray-100"
          />
        </div>

        <div>
          <label
            htmlFor="cuit"
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            CUIT <span className="text-gray-400 font-normal">(opcional)</span>
          </label>
          <input
            id="cuit"
            type="text"
            maxLength={20}
            value={cuit}
            onChange={(e) => setCuit(e.target.value)}
            disabled={submitting}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm
                       focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                       disabled:bg-gray-100"
          />
        </div>

        <div className="pt-2 border-t border-gray-100">
          <p className="text-sm font-medium text-gray-700 mb-3 mt-3">
            Administrador
          </p>
        </div>

        <div>
          <label
            htmlFor="adminFullName"
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            Full Name
          </label>
          <input
            id="adminFullName"
            type="text"
            required
            maxLength={150}
            value={adminFullname}
            onChange={(e) => setAdminFullname(e.target.value)}
            disabled={submitting}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm
                       focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                       disabled:bg-gray-100"
          />
        </div>

        <div>
          <label
            htmlFor="adminEmail"
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            Email
          </label>
          <input
            id="adminEmail"
            type="email"
            required
            maxLength={150}
            value={adminEmail}
            onChange={(e) => setAdminEmail(e.target.value)}
            disabled={submitting}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm
                       focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                       disabled:bg-gray-100"
          />
        </div>

        {error && (
          <div
            role="alert"
            className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2"
          >
            {error}
          </div>
        )}

        <div className="flex items-center gap-3 pt-2">
          <button
            type="submit"
            disabled={submitting}
            className="rounded-lg bg-blue-600 text-white text-sm font-medium px-4 py-2.5
                       hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500
                       focus:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed
                       transition-colors"
          >
            {submitting ? "Creando…" : "Crear organización"}
          </button>
          <button
            type="button"
            onClick={() => navigate("/")}
            disabled={submitting}
            className="rounded-lg border border-gray-300 bg-white text-sm font-medium px-4 py-2.5
                       text-gray-700 hover:bg-gray-50 disabled:opacity-60 transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

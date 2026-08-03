import { NavLink } from "react-router";
import { navSections } from "./navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { cn } from "@/lib/utils";

export function Sidebar() {
  const { user } = useAuth();

  const visible = navSections.filter(
    (s) => !s.requiredRole || s.requiredRole === user?.role,
  );

  return (
    <aside className="w-16 shrink-0 bg-sidebar border-r border-sidebar-border flex flex-col items-center py-4 gap-1">
      {visible.map((section) => {
        const Icon = section.icon;
        return (
          <NavLink
            key={section.to}
            to={section.to}
            title={section.label}
            className={({ isActive }) =>
              cn(
                "w-11 h-11 flex items-center justify-center rounded-lg transition-colors",
                "text-sidebar-foreground/60 hover:text-sidebar-foreground hover:bg-sidebar-accent",
                isActive && "bg-sidebar-accent text-sidebar-foreground",
              )
            }
          >
            <Icon className="w-5 h-5" />
          </NavLink>
        );
      })}
    </aside>
  );
}

import { Outlet } from "react-router";
import { Sidebar } from "./Sidebar";
import { useTopBar } from "./TopBarContext";
import { useAuth } from "@/features/auth/AuthContext";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuGroup,
} from "@/components/ui/dropdown-menu";
import { useNavigate } from "react-router";

function initials(name: string) {
  return name
    .split(" ")
    .map((p) => p[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
}

export function AppShell() {
  const { user, logout } = useAuth();
  const { title, actions } = useTopBar();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout().catch(() => {});
    navigate("/login", { replace: true });
  };

  return (
    <div className="h-screen flex bg-background">
      <Sidebar />

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-14 shrink-0 border-b bg-card flex items-center justify-between px-4">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 rounded bg-primary" />
            <span className="font-semibold text-foreground">Platform</span>
          </div>

          {user && (
            <DropdownMenu>
              <DropdownMenuTrigger className="flex items-center gap-2 rounded-lg px-2 py-1 hover:bg-accent transition-colors outline-none">
                <div className="text-right leading-tight hidden sm:block">
                  <div className="text-sm font-medium text-foreground">
                    {user.email}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {user.role}
                  </div>
                </div>
                <Avatar className="h-8 w-8">
                  <AvatarFallback>{initials(user.email)}</AvatarFallback>
                </Avatar>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuGroup>
                  <DropdownMenuLabel>My Account</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleLogout}>
                    Logout
                  </DropdownMenuItem>
                </DropdownMenuGroup>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </header>

        <div className="h-12 shrink-0 border-b bg-card/50 flex items-center justify-between px-6">
          <h2 className="text-sm font-medium text-foreground">{title}</h2>
          <div className="flex items-center gap-2">{actions}</div>
        </div>

        <main className="flex-1 overflow-auto">
          <div className="max-w-6xl mx-auto px-6 py-6">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}

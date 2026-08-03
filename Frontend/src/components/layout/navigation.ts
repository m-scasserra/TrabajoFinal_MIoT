import { Building2, Cpu, Bell, Users, type LucideIcon } from "lucide-react";
import { Roles } from "@/features/users/roles";

export interface NavSection {
  to: string;
  label: string;
  icon: LucideIcon;
  requiredRole?: string;
}

export const navSections: NavSection[] = [
  {
    to: "/organisations",
    label: "Organisations",
    icon: Building2,
    requiredRole: Roles.SuperAdmin,
  },
  { to: "/devices", label: "Devices", icon: Cpu },
  { to: "/alarms", label: "Alarms", icon: Bell },
  { to: "/users", label: "Users", icon: Users },
];

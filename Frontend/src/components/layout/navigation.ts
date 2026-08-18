import {
  Building2,
  Cpu,
  Radio,
  Bell,
  Users,
  FileCog,
  PackagePlus,
  type LucideIcon,
} from "lucide-react";
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
  { to: "/device-profiles", label: "Device Profiles", icon: FileCog },
  { to: "/nodes", label: "Devices", icon: Cpu },
  { to: "/alarms", label: "Alarms", icon: Bell },
  { to: "/users", label: "Users", icon: Users },
  { to: "/gateways", label: "Gateways", icon: Radio },
  {
    to: "/provisioning/nodes",
    label: "Provision Device",
    icon: PackagePlus,
    requiredRole: Roles.SuperAdmin,
  },
];

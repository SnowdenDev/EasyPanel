"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

// Hand-drawn to match the approved mockup exactly (not lucide) — these four are the
// dashboard's visual identity, called out by name during design review as the reason a
// stock icon set read as generic. Everything else (buttons, dialogs, chevrons) uses
// lucide-react, which is fine — it's the small utility icons that stayed invisible.
const icons = {
  overview: (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <path d="M7.5 15l2.7-4.5 2.6 2.8L16.5 9" />
    </svg>
  ),
  nodes: (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="5" rx="1.4" />
      <rect x="3" y="15" width="18" height="5" rx="1.4" />
      <line x1="7" y1="6.5" x2="7" y2="6.5" />
      <line x1="7" y1="17.5" x2="7" y2="17.5" />
    </svg>
  ),
  instances: (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" rx="1.3" />
      <rect x="14" y="3" width="7" height="7" rx="1.3" />
      <rect x="3" y="14" width="7" height="7" rx="1.3" />
      <rect x="14" y="14" width="7" height="7" rx="1.3" />
    </svg>
  ),
  staff: (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="9" cy="8" r="3" />
      <path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6" />
      <circle cx="17.3" cy="7.3" r="2.2" />
      <path d="M15.8 12.3c2.5.5 4.3 2.6 4.3 5.2" />
    </svg>
  ),
  auditLog: (
    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <line x1="8" y1="8" x2="16" y2="8" />
      <line x1="8" y1="12" x2="13.5" y2="12" />
      <line x1="8" y1="16" x2="12" y2="16" />
    </svg>
  ),
};

const navItems = [
  { href: "/overview", label: "Overview", icon: icons.overview },
  { href: "/nodes", label: "Nodes", icon: icons.nodes },
  { href: "/instances", label: "Instances", icon: icons.instances },
  { href: "/staff", label: "Staff", icon: icons.staff },
  { href: "/audit-log", label: "Audit Log", icon: icons.auditLog },
];

const GITHUB_URL = "https://github.com/SnowdenDev";

export function Sidebar() {
  const pathname = usePathname();

  return (
    <div className="flex flex-col w-[232px] h-full bg-sidebar border-r border-sidebar-border shrink-0">
      <div className="flex items-center gap-2.5 px-5 pt-[22px] pb-[26px]">
        <Image src="/logo.png" alt="" width={28} height={28} className="shrink-0" />
        <span className="text-[14.5px] font-semibold tracking-tight text-foreground">EasyPanel</span>
      </div>

      <nav className="flex flex-col gap-1.5 px-3">
        {navItems.map((item) => {
          const active = item.href === "/overview" ? pathname === item.href : pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2.5 min-h-10 px-2.5 rounded-md text-[13.2px] font-medium transition-colors",
                active
                  ? "bg-sidebar-accent text-sidebar-accent-foreground"
                  : "text-muted-foreground hover:bg-sidebar-accent/60",
              )}
            >
              <span className={active ? "text-primary" : "text-muted-foreground"}>{item.icon}</span>
              <span className="flex-1">{item.label}</span>
              {active ? <span className="w-1 h-1 rounded-sm bg-primary shrink-0" /> : null}
            </Link>
          );
        })}
      </nav>

      <a
        href={GITHUB_URL}
        target="_blank"
        rel="noopener noreferrer"
        className="mt-auto min-h-10 px-5 py-2.5 border-t border-sidebar-border flex items-center gap-2 text-muted-foreground hover:text-foreground transition-colors"
      >
        <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" className="shrink-0">
          <path d="M12 .5C5.65.5.5 5.65.5 12c0 5.08 3.29 9.39 7.86 10.91.58.1.79-.25.79-.56 0-.27-.01-1.17-.02-2.12-3.2.7-3.88-1.36-3.88-1.36-.52-1.33-1.28-1.68-1.28-1.68-1.04-.71.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.02 1.75 2.68 1.25 3.34.95.1-.74.4-1.25.72-1.54-2.55-.29-5.24-1.28-5.24-5.69 0-1.26.45-2.28 1.19-3.09-.12-.29-.52-1.46.11-3.04 0 0 .97-.31 3.18 1.18a11.02 11.02 0 0 1 5.8 0c2.21-1.49 3.18-1.18 3.18-1.18.63 1.58.23 2.75.11 3.04.74.81 1.19 1.83 1.19 3.09 0 4.42-2.69 5.4-5.25 5.68.41.36.78 1.07.78 2.15 0 1.55-.01 2.8-.01 3.18 0 .31.21.67.8.56A10.51 10.51 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5Z" />
        </svg>
        <span className="text-[12.5px] font-medium">Developed by SnowdenDev</span>
      </a>
    </div>
  );
}

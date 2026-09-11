import { redirect } from "next/navigation";
import { ApiRequestError, backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Sidebar } from "@/components/dashboard/sidebar";
import type { NodeSummary } from "@/lib/types";

export default async function DashboardLayout({ children }: LayoutProps<"/">) {
  const session = await getServerSession();
  if (!session) {
    redirect("/login");
  }

  // A cheap call whose real job is confirming the session's token is still accepted by the
  // backend, not just unexpired locally (getServerSession only checks the JWT's own `exp`
  // claim) — every page under this layout benefits from the redirect-on-401 below instead
  // of each handling that individually.
  try {
    await backendFetch<NodeSummary[]>("/api/nodes");
  } catch (error) {
    if (!(error instanceof ApiRequestError && error.status === 401)) {
      throw error;
    }
    redirect("/login");
  }

  return (
    <div className="flex w-full h-screen bg-background">
      <Sidebar />
      <div className="flex flex-col flex-1 min-w-0 h-full">{children}</div>
    </div>
  );
}

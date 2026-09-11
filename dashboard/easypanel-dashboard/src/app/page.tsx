import { redirect } from "next/navigation";
import { getServerSession } from "@/lib/session";

export default async function RootPage() {
  const session = await getServerSession();
  redirect(session ? "/instances" : "/login");
}

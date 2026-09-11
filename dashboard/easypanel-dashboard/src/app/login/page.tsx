import Image from "next/image";
import { redirect } from "next/navigation";
import { getServerSession } from "@/lib/session";
import { LoginForm } from "./login-form";

export default async function LoginPage() {
  const session = await getServerSession();
  if (session) {
    redirect("/instances");
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-[360px] flex flex-col gap-8">
        <div className="flex items-center gap-2.5">
          <Image src="/logo.png" alt="" width={32} height={32} className="shrink-0" priority />
          <span className="text-[15px] font-semibold tracking-tight">EasyPanel</span>
        </div>
        <LoginForm />
      </div>
    </div>
  );
}

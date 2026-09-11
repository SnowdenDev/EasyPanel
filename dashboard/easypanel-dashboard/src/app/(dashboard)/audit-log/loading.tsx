import { Skeleton } from "@/components/ui/skeleton";

export default function AuditLogLoading() {
  return (
    <div className="flex-1 overflow-auto">
      <div className="flex flex-col gap-1 px-8 pt-7 pb-5">
        <Skeleton className="h-5 w-28" />
        <Skeleton className="h-4 w-56" />
      </div>

      <div className="flex flex-col gap-5 px-8 pb-10">
        <Skeleton className="h-[400px] rounded-lg" />
      </div>
    </div>
  );
}

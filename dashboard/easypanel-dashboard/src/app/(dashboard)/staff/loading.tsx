import { Skeleton } from "@/components/ui/skeleton";

export default function StaffLoading() {
  return (
    <div className="flex-1 overflow-auto">
      <div className="flex flex-col gap-1 px-8 pt-7 pb-5">
        <Skeleton className="h-5 w-20" />
        <Skeleton className="h-4 w-80" />
      </div>

      <div className="flex flex-col gap-6 px-8 pb-10">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-[104px] rounded-lg" />
          ))}
        </div>
        <Skeleton className="h-[280px] rounded-lg" />
      </div>
    </div>
  );
}

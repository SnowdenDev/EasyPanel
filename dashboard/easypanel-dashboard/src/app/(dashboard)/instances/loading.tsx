import { Skeleton } from "@/components/ui/skeleton";

export default function InstancesLoading() {
  return (
    <div className="flex-1 overflow-auto">
      <div className="flex flex-col gap-1 px-8 pt-7 pb-5">
        <Skeleton className="h-5 w-32" />
        <Skeleton className="h-4 w-64" />
      </div>

      <div className="flex flex-col gap-5 px-8 pb-10">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-[104px] rounded-lg" />
          ))}
        </div>
        <Skeleton className="h-[320px] rounded-lg" />
      </div>
    </div>
  );
}

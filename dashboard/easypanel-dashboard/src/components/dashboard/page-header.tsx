/**
 * Shared header for every dashboard page: title on the left, actions on the right.
 * Keeps spacing/typography consistent instead of each page hand-rolling its own header row.
 */
export function PageHeader({
  title,
  description,
  actions,
}: {
  title: React.ReactNode;
  description?: React.ReactNode;
  actions?: React.ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-4 px-8 pt-7 pb-5">
      <div className="flex flex-col gap-1">
        <h1 className="text-[19px] font-semibold tracking-tight text-foreground">{title}</h1>
        {description ? <p className="text-[13px] text-muted-foreground max-w-xl">{description}</p> : null}
      </div>
      {actions ? <div className="flex items-center gap-2.5 shrink-0">{actions}</div> : null}
    </div>
  );
}

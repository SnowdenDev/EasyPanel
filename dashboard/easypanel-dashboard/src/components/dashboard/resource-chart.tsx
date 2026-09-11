"use client";

import { Area, AreaChart, CartesianGrid, XAxis } from "recharts";
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";
import type { ResourceHistoryPoint } from "@/lib/mock-data";

const chartConfig: ChartConfig = {
  cpu: { label: "CPU", color: "var(--primary)" },
  memory: { label: "Memory", color: "var(--success)" },
};

export function ResourceChart({ data }: { data: ResourceHistoryPoint[] }) {
  return (
    <ChartContainer config={chartConfig} className="aspect-auto h-[220px] w-full">
      <AreaChart data={data} margin={{ left: 0, right: 8, top: 8, bottom: 0 }}>
        <defs>
          <linearGradient id="fillCpu" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="var(--color-cpu)" stopOpacity={0.35} />
            <stop offset="95%" stopColor="var(--color-cpu)" stopOpacity={0.02} />
          </linearGradient>
          <linearGradient id="fillMemory" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="var(--color-memory)" stopOpacity={0.3} />
            <stop offset="95%" stopColor="var(--color-memory)" stopOpacity={0.02} />
          </linearGradient>
        </defs>
        <CartesianGrid vertical={false} strokeDasharray="3 3" />
        <XAxis
          dataKey="time"
          tickLine={false}
          axisLine={false}
          tickMargin={8}
          minTickGap={48}
          fontSize={11}
        />
        <ChartTooltip content={<ChartTooltipContent indicator="dot" />} />
        <Area
          dataKey="memory"
          type="monotone"
          fill="url(#fillMemory)"
          stroke="var(--color-memory)"
          strokeWidth={1.75}
        />
        <Area
          dataKey="cpu"
          type="monotone"
          fill="url(#fillCpu)"
          stroke="var(--color-cpu)"
          strokeWidth={1.75}
        />
      </AreaChart>
    </ChartContainer>
  );
}

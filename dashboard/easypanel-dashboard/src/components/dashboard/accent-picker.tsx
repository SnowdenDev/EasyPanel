"use client";

import { useEffect, useState } from "react";
import { Check, Palette } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import { ACCENT_STORAGE_KEY, accents, type AccentId, DEFAULT_ACCENT } from "@/lib/accent";

export function AccentPicker() {
  const [accent, setAccent] = useState<AccentId>(DEFAULT_ACCENT);

  useEffect(() => {
    const stored = window.localStorage.getItem(ACCENT_STORAGE_KEY);
    if (stored && accents.some((option) => option.id === stored)) {
      setAccent(stored as AccentId);
    }
  }, []);

  function selectAccent(id: AccentId) {
    setAccent(id);
    document.documentElement.setAttribute("data-accent", id);
    window.localStorage.setItem(ACCENT_STORAGE_KEY, id);
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        title="Accent color"
        className="flex items-center justify-center w-7 h-7 rounded-md text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
      >
        <Palette size={15} />
        <span className="sr-only">Accent color</span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel>Accent color</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <div className="grid grid-cols-5 gap-2 px-2 py-2">
          {accents.map((option) => (
            <button
              key={option.id}
              type="button"
              title={option.label}
              onClick={() => selectAccent(option.id)}
              className={cn(
                "relative flex items-center justify-center w-7 h-7 rounded-full ring-2 ring-offset-2 ring-offset-popover transition-all",
                accent === option.id ? "ring-foreground/70" : "ring-transparent hover:ring-border",
              )}
            >
              <span
                className="w-full h-full rounded-full"
                style={{ backgroundColor: option.swatch }}
              />
              {accent === option.id ? (
                <Check
                  size={12}
                  className="absolute inset-0 m-auto text-white drop-shadow-[0_0_1px_rgba(0,0,0,0.6)]"
                />
              ) : null}
            </button>
          ))}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

// Accent presets applied via the `data-accent` attribute on <html>. Each id must have a
// matching pair of CSS variable overrides in globals.css (one under `[data-accent="id"]`
// for light mode, one under `.dark[data-accent="id"]` for dark mode). "blue" is the
// default — it matches the falcon logo's cyan-to-blue gradient.
export const accents = [
  { id: "blue", label: "Blue (logo)", swatch: "#3B82F6" },
  { id: "violet", label: "Violet", swatch: "#8B5CF6" },
  { id: "emerald", label: "Emerald", swatch: "#10B981" },
  { id: "amber", label: "Amber", swatch: "#F59E0B" },
  { id: "rose", label: "Rose", swatch: "#F43F5E" },
] as const;

export type AccentId = (typeof accents)[number]["id"];

export const DEFAULT_ACCENT: AccentId = "blue";

export const ACCENT_STORAGE_KEY = "easypanel-accent";

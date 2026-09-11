import type { Metadata } from "next";
import { Inter, Geist_Mono } from "next/font/google";
import { Toaster } from "@/components/ui/sonner";
import { ThemeProvider } from "@/components/dashboard/theme-provider";
import { ACCENT_STORAGE_KEY, DEFAULT_ACCENT } from "@/lib/accent";
import "./globals.css";

// Runs before paint (blocking, in <head>) so the persisted accent choice applies on the
// very first frame — without this the page would flash the default accent, then jump to
// the user's saved one once accent-picker.tsx's effect runs on the client.
const setAccentBeforePaint = `
(function () {
  try {
    var accent = localStorage.getItem("${ACCENT_STORAGE_KEY}") || "${DEFAULT_ACCENT}";
    document.documentElement.setAttribute("data-accent", accent);
  } catch (e) {
    document.documentElement.setAttribute("data-accent", "${DEFAULT_ACCENT}");
  }
})();
`;

// Named "--font-sans" directly — matching globals.css's `@theme inline` token exactly
// (it was previously `--font-sans: var(--font-sans)`, a self-referential leftover from
// the shadcn preset that never actually resolved to Geist's variable name, so the sans
// font wasn't really applying regardless of which family this was set to).
const inter = Inter({
  variable: "--font-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "EasyPanel",
  description: "EasyPanel — dedicated server management",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      // suppressHydrationWarning: next-themes and the inline accent script both mutate
      // attributes on this element before React hydrates (class="dark", data-accent="…"),
      // which would otherwise trigger a hydration mismatch warning for attributes that are
      // intentionally client-only.
      suppressHydrationWarning
      className={`${inter.variable} ${geistMono.variable} h-full antialiased`}
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: setAccentBeforePaint }} />
      </head>
      <body className="min-h-full flex flex-col bg-background text-foreground">
        <ThemeProvider attribute="class" defaultTheme="dark" enableSystem disableTransitionOnChange>
          {children}
          <Toaster position="bottom-right" />
        </ThemeProvider>
      </body>
    </html>
  );
}

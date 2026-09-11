import type { Metadata } from "next";
import { Inter, Geist_Mono } from "next/font/google";
import { Toaster } from "@/components/ui/sonner";
import "./globals.css";

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
      className={`${inter.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-background text-foreground">
        {children}
        <Toaster theme="dark" position="bottom-right" />
      </body>
    </html>
  );
}

import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Zibzie Health Core",
  description: "پنل پرونده سلامت زی‌بزی",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="fa"
      dir="rtl"
      className="h-full antialiased"
    >
      <body className="flex min-h-full flex-col">{children}</body>
    </html>
  );
}

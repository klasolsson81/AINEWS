import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'NewsRoom AI -- AI-Genererad Nyhetssandning',
  description: 'En fullstandigt automatiserad, on-demand AI-nyhetssandning pa svenska.',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="sv">
      <body className={`${inter.className} antialiased`}>{children}</body>
    </html>
  );
}

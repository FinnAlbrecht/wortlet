import { useEffect } from "react";

export function PageHeader({ title, subtitle }: { title: string; subtitle?: string }) {
  useEffect(() => {
    document.title = title === "Vokabeltrainer" ? "Wortlet – Vokabeltrainer" : `${title} – Wortlet`;
  }, [title]);
  return (
    <header>
      <h1 id="h1">{title}</h1>
      {subtitle && <p id="sub">{subtitle}</p>}
    </header>
  );
}

import type { ReactNode } from "react";

type SectionProps = {
  title: string;
  columns?: "two" | "three" | "four";
  children: ReactNode;
};

export function Section({ title, columns = "four", children }: SectionProps) {
  return (
    <section className="entry-section">
      <h2>{title}</h2>
      <div className={`field-grid field-grid-${columns}`}>{children}</div>
    </section>
  );
}

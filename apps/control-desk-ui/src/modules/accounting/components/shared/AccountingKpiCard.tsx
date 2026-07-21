import type { LucideIcon } from "lucide-react";

type AccountingKpiCardProps = {
  icon: LucideIcon;
  label: string;
  value: string | number;
  detail: string;
  tone?: "neutral" | "good" | "attention";
};

export function AccountingKpiCard({
  icon: Icon,
  label,
  value,
  detail,
  tone = "neutral"
}: AccountingKpiCardProps) {
  return (
    <article className={`accounting-kpi-card ${tone}`}>
      <Icon size={18} />
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
    </article>
  );
}

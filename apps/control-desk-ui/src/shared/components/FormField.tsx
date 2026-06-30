import type { ReactNode } from "react";

type FormFieldProps = {
  label: string;
  children: ReactNode;
};

export function FormField({ label, children }: FormFieldProps) {
  return (
    <label className="form-field">
      <span>{label}</span>
      {children}
    </label>
  );
}

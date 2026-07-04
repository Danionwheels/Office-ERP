export function addDays(value: Date, days: number): Date {
  const next = new Date(value);
  next.setDate(next.getDate() + days);

  return next;
}

export function parseDateInput(value: string): Date {
  const [year, month, day] = value.split("-").map((part) => Number(part));

  return new Date(year, month - 1, day);
}

export function toDateInputValue(value: Date): string {
  const year = value.getFullYear();
  const month = (value.getMonth() + 1).toString().padStart(2, "0");
  const day = value.getDate().toString().padStart(2, "0");

  return `${year}-${month}-${day}`;
}

export function defaultManualJournalReference(value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");

  return `JE-${datePart}-${timePart}`.slice(0, 40);
}

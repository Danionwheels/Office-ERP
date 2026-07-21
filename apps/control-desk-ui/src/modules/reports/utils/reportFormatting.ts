export function formatReportMoney(amount: number, currencyCode: string): string {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: currencyCode,
      maximumFractionDigits: 2
    }).format(amount);
  } catch {
    return `${formatReportNumber(amount)} ${currencyCode}`.trim();
  }
}

export function formatReportNumber(value: number): string {
  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 2
  }).format(value);
}

export function formatReportInteger(value: number): string {
  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 0
  }).format(value);
}

export function formatReportDate(value: string): string {
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);

  if (year === undefined || month === undefined || day === undefined) {
    return value;
  }

  const parsed = new Date(year, month - 1, day);

  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric"
  }).format(parsed);
}

export function formatReportPeriod(start: string, end: string): string {
  return start === end
    ? formatReportDate(start)
    : `${formatReportDate(start)} – ${formatReportDate(end)}`;
}

export function formatCompactAmount(value: number, currencyCode: string): string {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: currencyCode,
      notation: "compact",
      maximumFractionDigits: 1
    }).format(value);
  } catch {
    return new Intl.NumberFormat(undefined, {
      notation: "compact",
      maximumFractionDigits: 1
    }).format(value);
  }
}

export function normalizeCurrencyCode(value: string): string {
  return value.replace(/[^a-z]/gi, "").slice(0, 3).toUpperCase();
}

export function formatReportError(caughtError: unknown): string {
  return caughtError instanceof Error ? caughtError.message : "The report could not be loaded.";
}

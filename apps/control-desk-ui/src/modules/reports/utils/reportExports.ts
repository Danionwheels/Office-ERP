export type CsvColumn<TRow> = {
  header: string;
  value: (row: TRow) => string | number | null | undefined;
};

export function downloadReportCsv<TRow>(
  fileName: string,
  columns: CsvColumn<TRow>[],
  rows: TRow[]
): void {
  const lines = [
    columns.map((column) => escapeCsvValue(column.header)).join(","),
    ...rows.map((row) =>
      columns.map((column) => escapeCsvValue(column.value(row))).join(",")
    )
  ];
  const blob = new Blob([`\uFEFF${lines.join("\r\n")}`], {
    type: "text/csv;charset=utf-8"
  });
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = objectUrl;
  link.download = fileName;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
}

export function printReport(reportTitle: string): void {
  const previousTitle = document.title;
  const restoreTitle = () => {
    document.title = previousTitle;
    window.removeEventListener("afterprint", restoreTitle);
  };

  document.title = `SafarSuite Control Desk - ${reportTitle}`;
  window.addEventListener("afterprint", restoreTitle);
  window.print();
  window.setTimeout(restoreTitle, 1_000);
}

export function createReportFileName(reportName: string, dateLabel: string): string {
  const safeReportName = reportName.toLowerCase().replace(/[^a-z0-9]+/g, "-");
  return `safarsuite-${safeReportName}-${dateLabel}.csv`;
}

function escapeCsvValue(value: string | number | null | undefined): string {
  const normalizedValue = value === null || value === undefined ? "" : String(value);
  const protectedValue =
    typeof value === "string" && /^[\s\u0000-\u001f\u007f]*[=+\-@]/.test(value)
      ? `'${value}`
      : normalizedValue;

  return /[",\r\n]/.test(protectedValue)
    ? `"${protectedValue.replace(/"/g, '""')}"`
    : protectedValue;
}

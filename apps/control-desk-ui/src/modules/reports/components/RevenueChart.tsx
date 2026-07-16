import type { RevenueSummaryPeriod } from "../types/reportTypes";
import { formatCompactAmount, formatReportMoney } from "../utils/reportFormatting";

export function RevenueChart({
  periods,
  currencyCode,
  chartType
}: {
  periods: RevenueSummaryPeriod[];
  currencyCode: string;
  chartType: "bar" | "line";
}) {
  const width = 760;
  const height = 270;
  const left = 66;
  const right = 20;
  const top = 22;
  const bottom = 50;
  const chartWidth = width - left - right;
  const chartHeight = height - top - bottom;
  const minimum = Math.min(0, ...periods.map((period) => period.revenue));
  const maximum = Math.max(0, ...periods.map((period) => period.revenue));
  const scaleMaximum = maximum === minimum ? maximum + 1 : maximum;
  const range = scaleMaximum - minimum;
  const slotWidth = chartWidth / Math.max(periods.length, 1);
  const y = (value: number) => top + ((scaleMaximum - value) / range) * chartHeight;
  const zeroY = y(0);
  const points = periods
    .map((period, index) => `${left + slotWidth * (index + 0.5)},${y(period.revenue)}`)
    .join(" ");

  return (
    <figure className="revenue-chart">
      <svg
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-labelledby="revenue-chart-title revenue-chart-description"
      >
        <title id="revenue-chart-title">
          {chartType === "bar" ? "Bar" : "Line"} chart of revenue by period
        </title>
        <desc id="revenue-chart-description">
          Revenue ranges from {formatReportMoney(minimum, currencyCode)} to{" "}
          {formatReportMoney(maximum, currencyCode)}. Exact values follow in the table.
        </desc>
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const value = scaleMaximum - range * ratio;
          const lineY = top + chartHeight * ratio;
          return (
            <g key={ratio}>
              <line className="revenue-chart-grid" x1={left} x2={width - right} y1={lineY} y2={lineY} />
              <text className="revenue-chart-axis-label" x={left - 8} y={lineY + 4} textAnchor="end">
                {formatCompactAmount(value, currencyCode)}
              </text>
            </g>
          );
        })}
        <line className="revenue-chart-axis" x1={left} x2={width - right} y1={zeroY} y2={zeroY} />
        {chartType === "bar" ? (
          periods.map((period, index) => {
            const valueY = y(period.revenue);
            const barY = Math.min(valueY, zeroY);
            const barHeight = Math.max(Math.abs(zeroY - valueY), 1);
            const barWidth = Math.min(slotWidth * 0.62, 48);
            const x = left + slotWidth * (index + 0.5) - barWidth / 2;
            return (
              <rect
                className={period.revenue < 0 ? "revenue-chart-bar negative" : "revenue-chart-bar"}
                key={`${period.periodStart}-${period.periodEnd}`}
                x={x}
                y={barY}
                width={barWidth}
                height={barHeight}
                rx={3}
              >
                <title>{period.label}: {formatReportMoney(period.revenue, currencyCode)}</title>
              </rect>
            );
          })
        ) : (
          <>
            <polyline className="revenue-chart-line" points={points} />
            {periods.map((period, index) => (
              <circle
                className="revenue-chart-point"
                key={`${period.periodStart}-${period.periodEnd}`}
                cx={left + slotWidth * (index + 0.5)}
                cy={y(period.revenue)}
                r={4}
              >
                <title>{period.label}: {formatReportMoney(period.revenue, currencyCode)}</title>
              </circle>
            ))}
          </>
        )}
        {periods.map((period, index) => (
          <text
            className="revenue-chart-period-label"
            key={`label-${period.periodStart}-${period.periodEnd}`}
            x={left + slotWidth * (index + 0.5)}
            y={height - 20}
            textAnchor="middle"
          >
            {period.label.length <= 12 ? period.label : `${period.label.slice(0, 11)}…`}
          </text>
        ))}
      </svg>
    </figure>
  );
}

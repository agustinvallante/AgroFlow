interface MetricCardProps {
  label: string;
  value: string | number;
  unit?: string;
  trend?: string;
  trendDirection?: "up" | "down";
  valueColor?: string;
  extraClassName?: string;
}

export function MetricCard({
  label,
  value,
  unit,
  trend,
  trendDirection,
  valueColor,
  extraClassName,
}: MetricCardProps) {
  return (
    <div className={`metric ${extraClassName ?? ""}`}>
      <div className="label">{label}</div>
      <div className="value" style={valueColor ? { color: valueColor } : undefined}>
        {value}
        {unit && <small>{unit}</small>}
      </div>
      {trend && (
        <div className={`trend ${trendDirection ?? ""}`}>{trend}</div>
      )}
    </div>
  );
}

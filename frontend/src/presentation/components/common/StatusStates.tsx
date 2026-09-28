export function LoadingState({ label = "Cargando datos..." }: { label?: string }) {
  return <div className="loading-state">{label}</div>;
}

export function ErrorState({ message }: { message: string }) {
  return <div className="error-state">{message}</div>;
}

export function EmptyState({ message }: { message: string }) {
  return <div className="empty-state">{message}</div>;
}

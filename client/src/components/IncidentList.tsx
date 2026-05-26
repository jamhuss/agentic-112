import type { Incident, UpdateIncidentRequest } from "../types";
import { IncidentCard } from "./IncidentCard";

interface Props {
  incidents: Incident[];
  onEdit: (incident: Incident) => void;
  onUpdateStatus: (id: string, request: UpdateIncidentRequest) => void;
}

export function IncidentList({ incidents, onEdit, onUpdateStatus }: Props) {
  if (incidents.length === 0) {
    return <p className="empty-state">Inga ärenden ännu.</p>;
  }

  return (
    <div className="incident-grid">
      {incidents.map((incident) => (
        <IncidentCard
          key={incident.id}
          incident={incident}
          onEdit={onEdit}
          onUpdateStatus={onUpdateStatus}
        />
      ))}
    </div>
  );
}

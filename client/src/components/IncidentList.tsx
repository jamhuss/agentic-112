import type { Incident } from "../types";
import { SERVICE_LABELS, PRIORITY_LABELS, STATUS_LABELS } from "../labels";

interface Props {
  incidents: Incident[];
  onEdit: (incident: Incident) => void;
}

export function IncidentList({ incidents, onEdit }: Props) {
  if (incidents.length === 0) {
    return <p className="empty-state">Inga ärenden ännu.</p>;
  }

  return (
    <table className="incident-table">
      <thead>
        <tr>
          <th>Beskrivning</th>
          <th>Tjänster</th>
          <th>Prioritet</th>
          <th>Status</th>
          <th>Skapad av</th>
          <th>Datum</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {incidents.map((incident) => (
          <tr key={incident.id}>
            <td>{incident.description}</td>
            <td>{incident.services.map((s) => SERVICE_LABELS[s] ?? s).join(", ")}</td>
            <td>
              <span className={`badge priority-${incident.priority}`}>
                {PRIORITY_LABELS[incident.priority] ?? incident.priority}
              </span>
            </td>
            <td>
              <span className={`badge status-${incident.status}`}>
                {STATUS_LABELS[incident.status] ?? incident.status}
              </span>
            </td>
            <td>{incident.createdBy}</td>
            <td>{new Date(incident.createdAt).toLocaleString("sv-SE")}</td>
            <td>
              <button className="btn-edit" onClick={() => onEdit(incident)}>
                Redigera
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

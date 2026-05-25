import type { Incident } from "../types";
import { SERVICE_LABELS, PRIORITY_LABELS } from "../labels";

interface Props {
  incidents: Incident[];
}

export function IncidentList({ incidents }: Props) {
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
          <th>Skapad av</th>
          <th>Datum</th>
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
            <td>{incident.createdBy}</td>
            <td>{new Date(incident.createdAt).toLocaleString("sv-SE")}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

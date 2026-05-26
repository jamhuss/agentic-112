import type { Incident, UpdateIncidentRequest } from "../types";
import { SERVICE_LABELS, PRIORITY_LABELS, STATUS_LABELS } from "../labels";
import { PipelineSteps } from "./PipelineSteps";

interface Props {
  incident: Incident;
  onEdit: (incident: Incident) => void;
  onUpdateStatus: (id: string, request: UpdateIncidentRequest) => void;
}

export function IncidentCard({ incident, onEdit, onUpdateStatus }: Props) {
  return (
    <div className={`incident-card status-border-${incident.status}`}>
      <div className="card-header">
        <h3 className="card-description">{incident.description}</h3>
        <span className={`badge status-${incident.status}`}>
          {STATUS_LABELS[incident.status] ?? incident.status}
        </span>
      </div>

      <div className="card-meta">
        <span>
          <strong>Tjänster:</strong>{" "}
          {incident.services.map((s) => SERVICE_LABELS[s] ?? s).join(", ")}
        </span>
        <span>
          <strong>Prioritet:</strong>{" "}
          <span className={`badge priority-${incident.priority}`}>
            {PRIORITY_LABELS[incident.priority] ?? incident.priority}
          </span>
        </span>
        <span>
          <strong>Skapad av:</strong> {incident.createdBy}
        </span>
        <span>
          <strong>Datum:</strong>{" "}
          {new Date(incident.createdAt).toLocaleString("sv-SE")}
        </span>
        {incident.confidence != null && (
          <span>
            <strong>Konfidens:</strong> {Math.round(incident.confidence * 100)}%
          </span>
        )}
      </div>

      <PipelineSteps steps={incident.steps ?? []} createdBy={incident.createdBy} />

      <div className="card-actions">
        {incident.status === "flagged" && (
          <>
            <button
              className="btn-approve"
              onClick={() => onUpdateStatus(incident.id, { status: "ongoing" })}
            >
              ✓ Godkänn
            </button>
            <button
              className="btn-reject"
              onClick={() => onUpdateStatus(incident.id, { status: "rejected" })}
            >
              ✗ Avvisa
            </button>
          </>
        )}
        <button className="btn-edit" onClick={() => onEdit(incident)}>
          Redigera
        </button>
      </div>
    </div>
  );
}

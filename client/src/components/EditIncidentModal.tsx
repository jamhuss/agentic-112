import { useState } from "react";
import type { Incident, UpdateIncidentRequest } from "../types";
import { SERVICE_LABELS, PRIORITY_LABELS, STATUS_LABELS } from "../labels";

const AVAILABLE_SERVICES = Object.keys(SERVICE_LABELS);
const PRIORITIES = Object.keys(PRIORITY_LABELS);
const STATUSES = Object.keys(STATUS_LABELS);

interface Props {
  incident: Incident | null;
  onClose: () => void;
  onSubmit: (id: string, request: UpdateIncidentRequest) => void;
}

export function EditIncidentModal({ incident, onClose, onSubmit }: Props) {
  const [description, setDescription] = useState(incident?.description ?? "");
  const [services, setServices] = useState<string[]>(incident?.services ?? []);
  const [priority, setPriority] = useState(incident?.priority ?? "medium");
  const [status, setStatus] = useState(incident?.status ?? "ongoing");

  if (!incident) return null;

  function handleServiceToggle(service: string) {
    setServices((prev) =>
      prev.includes(service)
        ? prev.filter((s) => s !== service)
        : [...prev, service]
    );
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!description.trim() || services.length === 0) return;
    onSubmit(incident!.id, {
      description: description.trim(),
      services,
      priority,
      status,
    });
    onClose();
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>Redigera ärende</h2>
        <form onSubmit={handleSubmit}>
          <label>
            Beskrivning
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              required
            />
          </label>

          <fieldset>
            <legend>Tjänster</legend>
            <div className="checkbox-group">
              {AVAILABLE_SERVICES.map((service) => (
                <label key={service} className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={services.includes(service)}
                    onChange={() => handleServiceToggle(service)}
                  />
                  {SERVICE_LABELS[service]}
                </label>
              ))}
            </div>
          </fieldset>

          <label>
            Prioritet
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value)}
            >
              {PRIORITIES.map((p) => (
                <option key={p} value={p}>
                  {PRIORITY_LABELS[p]}
                </option>
              ))}
            </select>
          </label>

          <label>
            Status
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {STATUS_LABELS[s]}
                </option>
              ))}
            </select>
          </label>

          <div className="modal-actions">
            <button type="button" className="btn-secondary" onClick={onClose}>
              Avbryt
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={!description.trim() || services.length === 0}
            >
              Spara
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

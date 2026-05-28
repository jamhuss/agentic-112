import { useState, useEffect } from "react";
import type { Incident, CreateManualRequest, UpdateIncidentRequest, ValidateIncidentRequest } from "../types";
import { SERVICE_LABELS, PRIORITY_LABELS, STATUS_LABELS } from "../labels";

const AVAILABLE_SERVICES = Object.keys(SERVICE_LABELS);
const PRIORITIES = Object.keys(PRIORITY_LABELS);
const STATUSES = Object.keys(STATUS_LABELS);

type ModalMode = "create" | "agentic" | "edit";

interface Props {
    open: boolean;
    mode: ModalMode;
    incident?: Incident | null;
    onClose: () => void;
    onCreate?: (request: CreateManualRequest) => void;
    onUpdate?: (id: string, request: UpdateIncidentRequest) => void;
    onValidate?: (id: string, request: ValidateIncidentRequest) => void;
    submitting?: boolean;
    validating?: boolean;
}

const TITLES: Record<ModalMode, string> = {
    create: "Skapa ärende manuellt",
    agentic: "Skapa agentic ärende",
    edit: "Redigera ärende",
};

export function IncidentModal({ open, mode, incident, onClose, onCreate, onUpdate, onValidate, submitting = false, validating = false }: Props) {
    const [description, setDescription] = useState("");
    const [services, setServices] = useState<string[]>([]);
    const [priority, setPriority] = useState("low");
    const [status, setStatus] = useState("ongoing");

    useEffect(() => {
        if (mode === "edit" && incident) {
            setDescription(incident.description);
            setServices(incident.services);
            setPriority(incident.priority);
            setStatus(incident.status);
        } else {
            setDescription("");
            setServices([]);
            setPriority("low");
            setStatus("ongoing");
        }
    }, [mode, incident]);

    if (!open) return null;

    const showInputs = mode !== "agentic";

    function handleServiceToggle(service: string) {
        setServices((prev) =>
            prev.includes(service)
                ? prev.filter((s) => s !== service)
                : [...prev, service]
        );
    }

    function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!description.trim()) return;

        if (mode === "edit" && incident && onUpdate) {
            onUpdate(incident.id, { description: description.trim(), services, priority, status });
        } else if (onCreate) {
            if (showInputs && services.length === 0) return;
            onCreate({ description: description.trim(), services, priority });
        }
    }

    function handleValidate() {
        if (!description.trim() || !incident || !onValidate) return;
        onValidate(incident.id, { description: description.trim(), services, priority });
    }

    const canSubmit = description.trim() && (mode === "agentic" || mode === "edit" || services.length > 0);

    return (
        <div className="modal-backdrop" onClick={onClose}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
                <h2>{TITLES[mode]}</h2>
                <form onSubmit={handleSubmit}>
                    <label>
                        Beskrivning
                        <textarea
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="Beskriv händelsen..."
                            rows={mode === "edit" ? 3 : 4}
                            required
                        />
                    </label>

                    {showInputs && (
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
                    )}

                    {showInputs && (
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
                    )}

                    {showInputs && <label>
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
                    </label>}

                    <div className="modal-actions">
                        <button type="button" className="btn-secondary" onClick={onClose} disabled={submitting}>
                            Avbryt
                        </button>
                        {mode === "edit" && (
                            <button
                                type="button"
                                className="btn-secondary"
                                onClick={handleValidate}
                                disabled={!canSubmit || submitting || validating}
                            >
                                {validating ? "Validerar..." : "Validera med AI"}
                            </button>
                        )}
                        <button
                            type="submit"
                            className="btn-primary"
                            disabled={!canSubmit || submitting || validating}
                        >
                            {submitting ? (mode === "edit" ? "Sparar..." : "Analyserar...") : (mode === "edit" ? "Spara" : "Skapa")}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

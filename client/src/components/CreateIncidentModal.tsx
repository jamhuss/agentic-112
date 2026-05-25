import { useState } from "react";
import type { CreateManualRequest } from "../types";
import { SERVICE_LABELS, PRIORITY_LABELS } from "../labels";

const AVAILABLE_SERVICES = Object.keys(SERVICE_LABELS);
const PRIORITIES = Object.keys(PRIORITY_LABELS);

interface Props {
    open: boolean;
    onClose: () => void;
    onSubmit: (request: CreateManualRequest) => void;
    isAgentic?: boolean;
}

export function CreateIncidentModal({ open, onClose, onSubmit, isAgentic = false }: Props) {
    const [description, setDescription] = useState("");
    const [services, setServices] = useState<string[]>([]);
    const [priority, setPriority] = useState<"critical" | "high" | "medium" | "low">("low");

    if (!open) return null;

    function handleServiceToggle(service: string) {
        setServices((prev) =>
            prev.includes(service)
                ? prev.filter((s) => s !== service)
                : [...prev, service]
        );
    }

    function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!description.trim() || (!isAgentic && services.length === 0)) return;
        onSubmit({ description: description.trim(), services, priority });
        setDescription("");
        setServices([]);
        setPriority("low");
        onClose();
    }

    return (
        <div className="modal-backdrop" onClick={onClose}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
                <h2>{isAgentic ? "Skapa agentic ärende" : "Skapa ärende manuellt"}</h2>
                <form onSubmit={handleSubmit}>
                    <label>
                        Beskrivning
                        <textarea
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="Beskriv händelsen..."
                            rows={4}
                            required
                        />
                    </label>
                    {!isAgentic && (
                        <>
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
                                    onChange={(e) => setPriority(e.target.value as "critical" | "high" | "medium" | "low")}
                                >
                                    {PRIORITIES.map((p) => (
                                        <option key={p} value={p}>
                                            {PRIORITY_LABELS[p]}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </>
                    )}



                    <div className="modal-actions">
                        <button type="button" className="btn-secondary" onClick={onClose}>
                            Avbryt
                        </button>
                        <button
                            type="submit"
                            className="btn-primary"
                            disabled={!description.trim() || (!isAgentic && services.length === 0)}
                        >
                            Skapa
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

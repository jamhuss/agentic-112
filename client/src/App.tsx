import { useEffect, useState } from "react";
import { fetchIncidents, createManualIncident, updateIncident, createAgenticIncident, validateIncident } from "./api";
import type { Incident, CreateManualRequest, UpdateIncidentRequest, ValidateIncidentRequest } from "./types";
import { IncidentList } from "./components/IncidentList";
import { CreateIncidentModal } from "./components/CreateIncidentModal";
import { EditIncidentModal } from "./components/EditIncidentModal";
import "./App.css";

function App() {
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingIncident, setEditingIncident] = useState<Incident | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [validating, setValidating] = useState(false);
  const [isAgentic, setIsAgentic] = useState(false);

  async function loadIncidents() {
    try {
      const data = await fetchIncidents();
      setIncidents(data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadIncidents();
  }, []);

  async function handleCreate(request: CreateManualRequest) {
    setSubmitting(true);
    try {
      if (isAgentic) {
        await createAgenticIncident({ description: request.description });
      } else {
        await createManualIncident(request);
      }
      await loadIncidents();
      setModalOpen(false);
      setIsAgentic(false);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleUpdate(id: string, request: UpdateIncidentRequest) {
    setSubmitting(true);
    try {
      await updateIncident(id, request);
      await loadIncidents();
      setEditingIncident(null);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleValidate(id: string, request: ValidateIncidentRequest) {
    setValidating(true);
    try {
      await validateIncident(id, request);
      await loadIncidents();
      setEditingIncident(null);
    } finally {
      setValidating(false);
    }
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>Ärenden</h1>
       <div className="buttons">
         <button className="btn-primary" onClick={() => { setIsAgentic(false); setModalOpen(true); }}>
          + Nytt ärende
        </button>
         <button className="btn-primary btn-agentic" onClick={() => { setIsAgentic(true); setModalOpen(true); }}>
          🤖 Nytt AI-ärende
         </button>
       </div>
      </header>

      <main>
        {loading ? (
          <p>Laddar...</p>
        ) : (
          <IncidentList
            incidents={incidents}
            onEdit={setEditingIncident}
            onUpdateStatus={handleUpdate}
          />
        )}
      </main>

      <CreateIncidentModal
        open={modalOpen}
        onClose={() => { setModalOpen(false); setIsAgentic(false); }}
        onSubmit={handleCreate}
        isAgentic={isAgentic}
        submitting={submitting}
      />

      <EditIncidentModal
        key={editingIncident?.id}
        incident={editingIncident}
        onClose={() => setEditingIncident(null)}
        onSubmit={handleUpdate}
        onValidate={handleValidate}
        submitting={submitting}
        validating={validating}
      />
    </div>
  );
}

export default App;

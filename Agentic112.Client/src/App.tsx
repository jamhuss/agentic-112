import { useEffect, useState } from "react";
import { fetchIncidents, createManualIncident, updateIncident, createAgenticIncident, validateIncident } from "./api";
import type { Incident, CreateManualRequest, UpdateIncidentRequest, ValidateIncidentRequest } from "./types";
import { IncidentList } from "./components/IncidentList";
import { IncidentModal } from "./components/IncidentModal";
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

  async function withLoading(setter: (v: boolean) => void, action: () => Promise<void>) {
    setter(true);
    try {
      await action();
      await loadIncidents();
    } finally {
      setter(false);
    }
  }

  function handleCreate(request: CreateManualRequest) {
    return withLoading(setSubmitting, async () => {
      if (isAgentic) {
        await createAgenticIncident({ description: request.description });
      } else {
        await createManualIncident(request);
      }
      setModalOpen(false);
      setIsAgentic(false);
    });
  }

  function handleUpdate(id: string, request: UpdateIncidentRequest) {
    return withLoading(setSubmitting, async () => {
      await updateIncident(id, request);
      setEditingIncident(null);
    });
  }

  function handleValidate(id: string, request: ValidateIncidentRequest) {
    return withLoading(setValidating, async () => {
      await validateIncident(id, request);
      setEditingIncident(null);
    });
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

      <IncidentModal
        open={modalOpen}
        mode={isAgentic ? "agentic" : "create"}
        onClose={() => { setModalOpen(false); setIsAgentic(false); }}
        onCreate={handleCreate}
        submitting={submitting}
      />

      <IncidentModal
        key={editingIncident?.id}
        open={!!editingIncident}
        mode="edit"
        incident={editingIncident}
        onClose={() => setEditingIncident(null)}
        onUpdate={handleUpdate}
        onValidate={handleValidate}
        submitting={submitting}
        validating={validating}
      />
    </div>
  );
}

export default App;

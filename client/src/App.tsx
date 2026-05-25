import { useEffect, useState } from "react";
import { fetchIncidents, createManualIncident, updateIncident, createAgenticIncident } from "./api";
import type { Incident, CreateManualRequest, UpdateIncidentRequest } from "./types";
import { IncidentList } from "./components/IncidentList";
import { CreateIncidentModal } from "./components/CreateIncidentModal";
import { EditIncidentModal } from "./components/EditIncidentModal";
import "./App.css";

function App() {
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingIncident, setEditingIncident] = useState<Incident | null>(null);
  const [loading, setLoading] = useState(true);
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
    await (isAgentic ? createAgenticIncident(request) : createManualIncident(request));
    await loadIncidents();
  }

  async function handleUpdate(id: string, request: UpdateIncidentRequest) {
    await updateIncident(id, request);
    await loadIncidents();
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>Ärenden</h1>
       <div className="buttons">
         <button className="btn-primary" onClick={() => setModalOpen(true)}>
          + Nytt ärende
        </button>
         <button className="btn-primary" onClick={() => { setIsAgentic(true); setModalOpen(true); } }>
          + Nytt agentic ärende
         </button>
       </div>
      </header>

      <main>
        {loading ? (
          <p>Laddar...</p>
        ) : (
          <IncidentList incidents={incidents} onEdit={setEditingIncident} />
        )}
      </main>

      <CreateIncidentModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSubmit={handleCreate}
        isAgentic={isAgentic}
      />

      <EditIncidentModal
        key={editingIncident?.id}
        incident={editingIncident}
        onClose={() => setEditingIncident(null)}
        onSubmit={handleUpdate}
      />
    </div>
  );
}

export default App;

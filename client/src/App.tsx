import { useEffect, useState } from "react";
import { fetchIncidents, createManualIncident } from "./api";
import type { Incident, CreateManualRequest } from "./types";
import { IncidentList } from "./components/IncidentList";
import { CreateIncidentModal } from "./components/CreateIncidentModal";
import "./App.css";

function App() {
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);

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
    await createManualIncident(request);
    await loadIncidents();
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>Ärenden</h1>
        <button className="btn-primary" onClick={() => setModalOpen(true)}>
          + Nytt ärende
        </button>
      </header>

      <main>
        {loading ? <p>Laddar...</p> : <IncidentList incidents={incidents} />}
      </main>

      <CreateIncidentModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSubmit={handleCreate}
      />
    </div>
  );
}

export default App;

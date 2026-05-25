import type { CreateManualRequest, Incident, UpdateIncidentRequest } from "./types";

const API_BASE = "/api/incidents";

export async function fetchIncidents(): Promise<Incident[]> {
  const res = await fetch(API_BASE);
  if (!res.ok) throw new Error("Failed to fetch incidents");
  return res.json();
}

export async function createManualIncident(
  request: CreateManualRequest
): Promise<Incident> {
  const res = await fetch(`${API_BASE}/manual`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error("Failed to create incident");
  return res.json();
}

export async function createAgenticIncident(
  request: CreateManualRequest
): Promise<Incident> {
  const res = await fetch(`${API_BASE}/ai`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error("Failed to create incident");
  return res.json();
}

export async function updateIncident(
  id: string,
  request: UpdateIncidentRequest
): Promise<Incident> {
  const res = await fetch(`${API_BASE}/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error("Failed to update incident");
  return res.json();
}

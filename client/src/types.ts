export interface Incident {
  id: string;
  description: string;
  services: string[];
  priority: string;
  status: string;
  createdBy: string;
  confidence?: number;
  credibility?: string;
  needsHumanReview?: boolean;
  createdAt: string;
}

export interface CreateManualRequest {
  description: string;
  services: string[];
  priority: string;
}

export interface UpdateIncidentRequest {
  description?: string;
  services?: string[];
  priority?: string;
  status?: string;
}

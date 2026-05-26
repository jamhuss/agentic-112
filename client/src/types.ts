export interface PipelineStep {
  name: string;
  result: string;
  reasoning: string;
  timestamp: string;
}

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
  steps: PipelineStep[];
  createdAt: string;
}

export interface CreateManualRequest {
  description: string;
  services: string[];
  priority: string;
}

export interface CreateAiRequest {
  description: string;
}

export interface UpdateIncidentRequest {
  description?: string;
  services?: string[];
  priority?: string;
  status?: string;
}

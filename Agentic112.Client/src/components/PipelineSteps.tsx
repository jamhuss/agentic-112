import type { PipelineStep } from "../types";

const STEP_LABELS: Record<string, string> = {
  classification: "Klassificering",
  classification_validation: "Validering",
  credibility_check: "Trovärdighet",
};

function getStepIcon(step: PipelineStep): string {
  if (step.result === "ERROR") return "❌";

  if (step.name === "credibility_check") {
    if (step.result.includes("credibility: high")) return "✅";
    if (step.result.includes("credibility: medium")) return "⚠️";
    if (step.result.includes("credibility: low")) return "🚫";
  }

  if (step.name === "classification_validation") {
    if (step.result.includes("servicesMatch: True")) return "✅";
    return "⚠️";
  }

  return "✅";
}

interface Props {
  steps: PipelineStep[];
  createdBy: string;
}

export function PipelineSteps({ steps, createdBy }: Props) {
  if (steps.length === 0 && createdBy === "User") {
    return (
      <div className="pipeline-steps">
        <div className="pipeline-step">
          <span className="step-icon">📝</span>
          <span className="step-label">Manuellt skapat</span>
        </div>
      </div>
    );
  }

  return (
    <div className="pipeline-steps">
      {steps.map((step, i) => {
        const isError = step.result === "ERROR";
        const icon = getStepIcon(step);
        return (
          <div key={i} className={`pipeline-step ${isError ? "step-error" : ""}`}>
            <span className="step-icon">{icon}</span>
            <div className="step-content">
              <span className="step-label">
                {STEP_LABELS[step.name] ?? step.name}
              </span>
              <span className="step-reasoning">{step.reasoning}</span>
            </div>
          </div>
        );
      })}
    </div>
  );
}

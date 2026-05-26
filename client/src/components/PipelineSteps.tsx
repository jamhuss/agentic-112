import type { PipelineStep } from "../types";

const STEP_LABELS: Record<string, string> = {
  classification: "Klassificering",
  credibility_check: "Trovärdighet",
};

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
        return (
          <div key={i} className={`pipeline-step ${isError ? "step-error" : ""}`}>
            <span className="step-icon">{isError ? "❌" : "✅"}</span>
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

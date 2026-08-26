namespace Agentic112.Domain.Constants
{
    public static class IncidentConstants
    {
        // Services
        public const string ServiceAmbulance = "ambulance";
        public const string ServicePolice = "police";
        public const string ServiceFireDepartment = "fire_department";
        public const string ServiceAssistance = "assistance";

        public static readonly List<string> Services =
        [
            ServiceAmbulance,
            ServicePolice,
            ServiceFireDepartment,
            ServiceAssistance
        ];

        // Priorities
        public const string PriorityCritical = "critical";
        public const string PriorityHigh = "high";
        public const string PriorityMedium = "medium";
        public const string PriorityLow = "low";

        public static readonly List<string> Priorities =
        [
            PriorityCritical,
            PriorityHigh,
            PriorityMedium,
            PriorityLow
        ];

        // Credibility levels
        public const string CredibilityHigh = "high";
        public const string CredibilityMedium = "medium";
        public const string CredibilityLow = "low";

        public static readonly List<string> CredibilityLevels =
        [
            CredibilityHigh,
            CredibilityMedium,
            CredibilityLow
        ];

        // Pipeline step names (must match the frontend STEP_LABELS keys)
        public const string StepClassification = "classification";
        public const string StepClassificationValidation = "classification_validation";
        public const string StepCredibilityCheck = "credibility_check";
        public const string StepValidation = "validation";

        public static readonly List<string> PipelineSteps =
        [
            StepClassification,
            StepClassificationValidation,
            StepCredibilityCheck,
            StepValidation
        ];

        // JSON schema names sent to the AI response format
        public const string SchemaCredibilityAssessment = "credibility_assessment";

        // Pipeline step statuses
        public const string StepStatusPending = "pending";
        public const string StepStatusCompleted = "completed";
        public const string StepStatusFailed = "failed";

        public static readonly List<string> PipelineStepStatuses =
        [
            StepStatusPending,
            StepStatusCompleted,
            StepStatusFailed
        ];

        // Created-by types
        public const string CreatedByUser = "User";
        public const string CreatedByAI = "AI";

        public static readonly List<string> CreatedByTypes =
        [
            CreatedByUser,
            CreatedByAI,
        ];

        // Statuses
        public const string StatusPendingReview = "pending_review";
        public const string StatusOngoing = "ongoing";
        public const string StatusFlagged = "flagged";
        public const string StatusRejected = "rejected";
        public const string StatusClosed = "closed";

        public static readonly List<string> Statuses =
        [
            StatusPendingReview,
            StatusOngoing,
            StatusFlagged,
            StatusRejected,
            StatusClosed
        ];
    }
}

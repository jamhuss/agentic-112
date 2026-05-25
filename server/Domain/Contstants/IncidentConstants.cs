namespace server.Domain.Contstants
{
    public static class IncidentConstants
    {
        public static readonly List<string> Services =
        [
            "ambulance",
            "police",
            "fire_department",
            "assistance"
        ];

        public static readonly List<string> Priorities =
        [
            "critical",
            "high",
            "medium",
            "low"
        ];

        public static readonly List<string> CredibilityLevels =
        [
            "high",
            "medium",
            "low"
        ];
    }
}

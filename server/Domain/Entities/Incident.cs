namespace Domain.Entities;

public class Incident
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public List<string> Services { get; set; } = new();
    public string Priority { get; set; } = "";
    public string CreatedBy { get; set; } = "User";
    public double? Confidence { get; set; }
    public string? Credibility { get; set; }
    public bool? NeedsHumanReview { get; set; }
    public DateTime CreatedAt { get; set; }
}
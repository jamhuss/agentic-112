using Agentic112.Domain.Models;

namespace Agentic122.Application.Interfaces;

public interface ICredibilityGateway
{
    Task<CredibilityAssessment> AssessAsync(string description, List<string> services, string priority, string createdBy);
}

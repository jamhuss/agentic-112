using Domain.Models;

namespace Application.Interfaces;

public interface ICredibilityGateway
{
    Task<CredibilityAssessment> AssessAsync(string description, List<string> services, string priority, string createdBy);
}

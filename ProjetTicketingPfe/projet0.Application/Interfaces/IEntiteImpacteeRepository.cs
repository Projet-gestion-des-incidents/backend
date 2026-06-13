using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Interfaces
{
    public interface IEntiteImpacteeRepository : IGenericRepository<EntiteImpactee>
    {
        Task<List<EntiteImpactee>> GetByIdsAsync(List<Guid> ids);
        Task<List<EntiteImpactee>> GetByIncidentIdAsync(Guid incidentId);
        Task<List<EntiteImpactee>> GetByTypeAsync(TypeEntiteImpactee type);
    }

}

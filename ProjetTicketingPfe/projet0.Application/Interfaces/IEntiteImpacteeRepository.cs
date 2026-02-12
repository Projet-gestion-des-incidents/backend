using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface IEntiteImpacteeRepository : IGenericRepository<EntiteImpactee>
    {
        Task<List<EntiteImpactee>> GetByIdsAsync(List<Guid> ids);
        Task<List<EntiteImpactee>> GetByIncidentIdAsync(Guid incidentId);
        Task<List<EntiteImpactee>> GetByTypeAsync(TypeEntiteImpactee type);
    }

}

using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface IIncidentTPERepository : IGenericRepository<IncidentTPE>
    {
        Task<List<IncidentTPE>> GetByIncidentIdAsync(Guid incidentId);
        Task<List<IncidentTPE>> GetByTPEIdAsync(Guid tpeId);
        Task<IncidentTPE> GetByIncidentAndTPEAsync(Guid incidentId, Guid tpeId);
        Task<bool> ExistsAsync(Guid incidentId, Guid tpeId);
        Task<bool> DeleteLiaisonAsync(Guid incidentId, Guid tpeId);
        
    }
}

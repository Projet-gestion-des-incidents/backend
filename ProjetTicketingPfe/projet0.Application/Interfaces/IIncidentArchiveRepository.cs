using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface IIncidentArchiveRepository : IGenericRepository<IncidentArchive>
    {
        Task<bool> ExistsAsync(Guid incidentId, Guid userId);
        Task<List<Guid>> GetArchivedIncidentIdsByUserAsync(Guid userId);
        Task<List<IncidentArchive>> GetArchivesByUserAsync(Guid userId);  // ✅ AJOUTER

        Task<IncidentArchive?> GetByIncidentAndUserAsync(Guid incidentId, Guid userId);
    }
}

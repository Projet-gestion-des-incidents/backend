using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface IIncidentArchiveRepository : IGenericRepository<IncidentArchive>
    {
        Task<bool> ExistsAsync(Guid incidentId, Guid userId);
        Task<List<Guid>> GetArchivedIncidentIdsByUserAsync(Guid userId);
        Task<List<IncidentArchive>> GetArchivesByUserAsync(Guid userId);  
        Task<List<Guid>> GetAllArchivedIncidentIdsAsync();

        Task<IncidentArchive?> GetByIncidentAndUserAsync(Guid incidentId, Guid userId);
    }
}

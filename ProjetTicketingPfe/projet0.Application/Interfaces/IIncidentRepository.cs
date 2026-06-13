using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Interfaces
{
    public interface IIncidentRepository : IGenericRepository<Incident>
    {
        Task<Incident> GetByCodeAsync(string code);
        IQueryable<Incident> QueryWithDetails(Guid? createdById = null);
        Task<List<Incident>> GetIncidentsByStatutAsync(StatutIncident statut);
        Task<List<Incident>> GetIncidentsBySeveriteAsync(SeveriteIncident severite);
        Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null);
        Task SaveChangesAsync();
        void RemoveEntiteImpactee(EntiteImpactee entite);
        Task AddEntiteImpacteeAsync(EntiteImpactee entite);
        Task<string> GenerateCodeIncidentAsync();
        Task<int> GetNextIncidentNumberAsync(int year);
        Task<Incident> GetIncidentWithDetailsAsync(Guid id);
        Task<List<Incident>> GetAllWithDetailsAsync();
        Task<List<Incident>> GetIncidentsByCreatedByAsync(Guid createdById);
       
    }
}

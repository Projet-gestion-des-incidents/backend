using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface IIncidentRepository : IGenericRepository<Incident>
    {
        Task<Incident> GetByCodeAsync(string code);
        IQueryable<Incident> QueryWithDetails(Guid? createdById = null);
        Task<List<Incident>> GetIncidentsByStatutAsync(StatutIncident statut);
        Task<List<Incident>> GetIncidentsBySeveriteAsync(SeveriteIncident severite);
        Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null);
        Task<string> GenerateCodeIncidentAsync();
        Task<int> GetNextIncidentNumberAsync(int year);
        Task<Incident> GetIncidentWithDetailsAsync(Guid id);
        Task<List<Incident>> GetAllWithDetailsAsync();
        Task<List<Incident>> GetIncidentsByCreatedByAsync(Guid createdById);
    }
}
